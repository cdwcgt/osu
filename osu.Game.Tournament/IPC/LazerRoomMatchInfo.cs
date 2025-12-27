// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Extensions.ExceptionExtensions;
using osu.Framework.Logging;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Rooms;
using osu.Game.Rulesets;
using osu.Game.Screens.OnlinePlay;
using osu.Game.Screens.OnlinePlay.Multiplayer;
using osu.Game.Screens.Select.Leaderboards;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.IPC
{
    public partial class LazerRoomMatchInfo : MatchIPCInfo
    {
        [Resolved]
        private MultiplayerClient client { get; set; } = null!;

        [Resolved]
        private RulesetStore rulesets { get; set; } = null!;

        [Resolved]
        protected LadderInfo Ladder { get; private set; } = null!;

        [Resolved]
        private BeatmapManager beatmapManager { get; set; } = null!;

        [Resolved]
        private BeatmapModelDownloader beatmapDownloader { get; set; } = null!;

        [Resolved]
        private BeatmapLookupCache beatmapLookupCache { get; set; } = null!;

        [Resolved]
        private Bindable<WorkingBeatmap> workingBeatmap { get; set; } = null!;

        public MultiSpectatorLeaderboardProvider LeaderboardProvider
        {
            set => leaderboardProvider = value;
        }

        private BindableList<MultiplayerRoomUser> redTeamUser = new BindableList<MultiplayerRoomUser>();
        private BindableList<MultiplayerRoomUser> blueTeamUser = new BindableList<MultiplayerRoomUser>();
        private BindableList<MultiplayerRoomUser> roomUser = new BindableList<MultiplayerRoomUser>();

        public IBindableList<MultiplayerRoomUser> RedTeamUser => redTeamUser;
        public IBindableList<MultiplayerRoomUser> BlueTeamUser => blueTeamUser;
        public IBindableList<MultiplayerRoomUser> RoomUser => roomUser;

        private MultiSpectatorLeaderboardProvider? leaderboardProvider;

        private readonly OnlinePlayBeatmapAvailabilityTracker beatmapAvailabilityTracker = new MultiplayerBeatmapAvailabilityTracker();

        private readonly Bindable<Room?> currentRoom = new Bindable<Room?>();

        public IBindable<Room?> CurrentRoom => currentRoom;

        private long lastPlaylistItemId;

        public LazerRoomMatchInfo()
        {
            AddInternal(beatmapAvailabilityTracker);
        }

        public void Join(Room room, string? password, Action<Room>? onSuccess = null, Action<string, Exception?>? onFailure = null) => Schedule(() =>
        {
            if (!client.IsConnected.Value)
            {
                return;
            }

            client.JoinRoom(room, password).ContinueWith(result =>
            {
                if (result.IsCompletedSuccessfully)
                {
                    currentRoom.Value = room;
                    onSuccess?.Invoke(room);
                    updateUsers();
                }
                else
                {
                    Exception? exception = result.Exception?.AsSingular();

                    onFailure ??= (m, e) => Logger.Error(e, m);

                    if (exception?.GetHubExceptionMessage() is string message)
                        onFailure?.Invoke(message, exception);
                    else
                        onFailure?.Invoke($"Failed to join multiplayer room. {exception?.Message}", exception);
                }
            });
        });

        public void Left()
        {
            if (currentRoom.Value == null) return;

            client.LeaveRoom().FireAndForget();
            currentRoom.Value = null;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            currentRoom.BindValueChanged(onRoomUpdated);
            beatmapAvailabilityTracker.Availability.BindValueChanged(onBeatmapAvailabilityChanged, true);

            client.RoomUpdated += onRoomUpdated;
            client.SettingsChanged += onSettingsChanged;
            client.ItemChanged += onItemChanged;
            client.GameplayAborted += onGameplayAborted;
            client.LoadRequested += onLoadRequested;
            client.UserJoined += _ => updateUsers();
            client.UserLeft += _ => updateUsers();
            client.UserKicked += _ => updateUsers();
        }

        private void onRoomUpdated() => Scheduler.AddOnce(() =>
        {
            if (currentRoom.Value != null && client.Room == null)
            {
                Logger.Log("exiting room");

                currentRoom.Value = null;
            }

            if (client.LocalUser != null && client.LocalUser.State != MultiplayerUserState.Spectating)
            {
                client.ToggleSpectate().FireAndForget();
            }

            Logger.Log($"Room status {client.Room?.State} {client.Room?.MatchState} {currentRoom.Value?.Status}");

            if (State.Value == TourneyState.Playing && client.Room?.State != MultiplayerRoomState.Playing)
            {
                State.Value = TourneyState.Ranking;
            }
        });

        private void onGameplayAborted(GameplayAbortReason reason)
        {
            State.Value = TourneyState.Idle;
        }

        private void onLoadRequested()
        {
            leaderboardProvider = null;

            Scheduler.AddOnce(() =>
            {
                updateGameplayState();

                if (!workingBeatmap.IsDefault)
                    State.Value = TourneyState.WaitingForClients;
            });
        }

        private void onSettingsChanged(MultiplayerRoomSettings settings)
        {
            if (settings.PlaylistItemId != lastPlaylistItemId)
            {
                onActivePlaylistItemChanged();
                lastPlaylistItemId = settings.PlaylistItemId;
            }
        }

        private void onItemChanged(MultiplayerPlaylistItem item)
        {
            if (item.ID == client.Room?.Settings.PlaylistItemId)
                onActivePlaylistItemChanged();
        }

        private void onActivePlaylistItemChanged()
        {
            if (client.Room == null)
                return;

            Scheduler.AddOnce(updateGameplayState);
        }

        private void updateGameplayState()
        {
            if (client.Room == null || client.LocalUser == null)
                return;

            MultiplayerPlaylistItem item = client.Room.CurrentPlaylistItem;
            int gameplayBeatmapId = client.LocalUser.BeatmapId ?? item.BeatmapID;
            int gameplayRulesetId = client.LocalUser.RulesetId ?? item.RulesetID;

            RulesetInfo ruleset = rulesets.GetRuleset(gameplayRulesetId)!;
            Ruleset rulesetInstance = ruleset.CreateInstance();

            var localBeatmap = beatmapManager.QueryBeatmap($@"{nameof(BeatmapInfo.OnlineID)} == $0 AND {nameof(BeatmapInfo.MD5Hash)} == {nameof(BeatmapInfo.OnlineMD5Hash)}", gameplayBeatmapId);
            workingBeatmap.Value = beatmapManager.GetWorkingBeatmap(localBeatmap);

            var existing = Ladder.CurrentMatch.Value?.Round.Value?.Beatmaps.FirstOrDefault(b => b.ID == gameplayBeatmapId);

            var itemMods = client.LocalUser.Mods.Concat(item.RequiredMods).Select(m => m.ToMod(rulesetInstance)).ToArray();

            if (existing != null)
            {
                Beatmap.Value = existing.Beatmap;
                string modStr = existing.Mods;

                var mod = rulesetInstance.CreateModFromAcronym(modStr);

                Mods.Value = mod != null ? rulesetInstance.ConvertToLegacyMods(new[] { mod }) : rulesetInstance.ConvertToLegacyMods(itemMods);
            }
        }

        private CancellationTokenSource? downloadCheckCancellation;
        private int lastAutoDownloadBeatmap;

        private void onBeatmapAvailabilityChanged(ValueChangedEvent<BeatmapAvailability> e)
        {
            if (client.Room == null || client.LocalUser == null)
                return;

            client.ChangeBeatmapAvailability(e.NewValue).FireAndForget();

            if (e.NewValue == BeatmapAvailability.LocallyAvailable())
            {
                updateGameplayState();

                // Optimistically enter spectator if the match is in progress while spectating.
                if (client.LocalUser.State == MultiplayerUserState.Spectating && (client.Room.State == MultiplayerRoomState.WaitingForLoad || client.Room.State == MultiplayerRoomState.Playing))
                    onLoadRequested();
            }

            if (e.NewValue == BeatmapAvailability.NotDownloaded())
            {
                MultiplayerPlaylistItem item = client.Room.CurrentPlaylistItem;

                if (item.BeatmapID == lastAutoDownloadBeatmap)
                    return;

                lastAutoDownloadBeatmap = item.BeatmapID;

                downloadCheckCancellation?.Cancel();

                beatmapLookupCache
                    .GetBeatmapAsync(item.BeatmapID, (downloadCheckCancellation = new CancellationTokenSource()).Token)
                    .ContinueWith(resolved => Schedule(() =>
                    {
                        var beatmapSet = resolved.GetResultSafely()?.BeatmapSet;

                        if (beatmapSet == null)
                            return;

                        if (beatmapManager.IsAvailableLocally(new BeatmapSetInfo { OnlineID = beatmapSet.OnlineID }))
                            return;

                        beatmapDownloader.Download(beatmapSet);
                    }));
            }
        }

        private void onRoomUpdated(ValueChangedEvent<Room?> room)
        {
            if (room.OldValue != null)
            {
                room.OldValue.PropertyChanged -= onRoomPropertyChanged;
            }

            if (room.NewValue != null)
            {
                room.NewValue.PropertyChanged += onRoomPropertyChanged;
            }

            updateChannel();
        }

        private void onRoomPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Room.ChannelId))
                updateChannel();
        }

        private void updateChannel()
        {
            if (currentRoom.Value?.RoomID == null || currentRoom.Value.ChannelId == 0)
                return;

            ChatChannel.Value = currentRoom.Value.ChannelId;
        }

        private double waitingForIdle;
        private const int time_to_idle_from_ranking = 10 * 1000;

        private void updateUsers() => Scheduler.AddOnce(() =>
        {
            if (client.Room == null || client.LocalUser == null)
                return;

            var currentUser = client.Room.Users;

            roomUser.AddRange(currentUser.Except(roomUser));
            var toRemove = roomUser.Except(currentUser).ToArray();

            foreach (var user in toRemove)
                roomUser.Remove(user);

            updateTeamUser(TeamColour.Red);
            updateTeamUser(TeamColour.Blue);
        });

        private void updateTeamUser(TeamColour colour)
        {
            var teamUser = roomUser.Where(p => GetTeamIds(colour).Any(id => p.UserID == id)).ToArray();

            var localTeamUser = colour == TeamColour.Red ? redTeamUser : blueTeamUser;

            localTeamUser.AddRange(teamUser.Except(localTeamUser));

            var toRemove = localTeamUser.Except(teamUser).ToArray();
            foreach (var user in toRemove)
                localTeamUser.Remove(user);
        }

        protected override void Update()
        {
            base.Update();

            if (State.Value == TourneyState.Playing)
            {
                updateScore();
            }

            if (State.Value == TourneyState.Ranking)
            {
                waitingForIdle += Time.Elapsed;

                if (waitingForIdle >= time_to_idle_from_ranking)
                    State.Value = TourneyState.Idle;
            }
            else
            {
                waitingForIdle = 0;
            }
        }

        private void updateScore()
        {
            if (leaderboardProvider == null)
                return;

            GameplayLeaderboardScore[] team1Score = GetTeamScore(TeamColour.Red).ToArray();
            GameplayLeaderboardScore[] team2Score = GetTeamScore(TeamColour.Blue).ToArray();

            Score1.Value = team1Score.Sum(CalculateModMultiplier);
            Score2.Value = team2Score.Sum(CalculateModMultiplier);

            Team1Combo.Value = team1Score.Sum(s => s.Combo.Value);
            Team2Combo.Value = team2Score.Sum(s => s.Combo.Value);
        }

        protected virtual IEnumerable<GameplayLeaderboardScore> GetTeamScore(TeamColour colour)
        {
            int[] teamIds = GetTeamIds(colour);

            return leaderboardProvider!.Scores.Where(u => teamIds.Any(t => t == u.User.OnlineID));
        }

        protected int[] GetTeamIds(TeamColour colour)
        {
            return Ladder.CurrentMatch.Value?.GetTeamByColor(colour)?.Players.Select(p => p.OnlineID).ToArray() ??
                   Array.Empty<int>();
        }

        protected long CalculateModMultiplier(GameplayLeaderboardScore s)
        {
            return s.TotalScore.Value;
        }
    }
}
