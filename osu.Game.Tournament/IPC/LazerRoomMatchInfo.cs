// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.ComponentModel;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ExceptionExtensions;
using osu.Framework.Logging;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Rooms;
using osu.Game.Rulesets;
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

        private readonly Bindable<Room?> currentRoom = new Bindable<Room?>();
        private long lastPlaylistItemId;

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

            client.RoomUpdated += onRoomUpdated;
            client.SettingsChanged += onSettingsChanged;
            client.ItemChanged += onItemChanged;
            client.GameplayAborted += onGameplayAborted;
            client.LoadRequested += onLoadRequested;
        }

        private void onRoomUpdated() => Scheduler.AddOnce(() =>
        {
            bool wasRoomJoined = currentRoom.Value == null;
            bool isRoomJoined = client.Room != null;

            if (wasRoomJoined && !isRoomJoined)
            {
                Logger.Log($"{this} exiting due to loss of room or connection");

                currentRoom.Value = null;
            }

            if (client.LocalUser != null && client.LocalUser.State != MultiplayerUserState.Spectating)
            {
                client.ToggleSpectate().FireAndForget();
            }
        });

        private void onGameplayAborted(GameplayAbortReason reason)
        {
            State.Value = TourneyState.Idle;
        }

        private void onLoadRequested()
        {
            State.Value = TourneyState.WaitingForClients;
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

            int beatmapId = item.BeatmapID;

            var existing = Ladder.CurrentMatch.Value?.Round.Value?.Beatmaps.FirstOrDefault(b => b.ID == beatmapId);

            var itemMods = client.LocalUser.Mods.Concat(item.RequiredMods).Select(m => m.ToMod(rulesetInstance)).ToArray();

            if (existing != null)
            {
                Beatmap.Value = existing.Beatmap;
                string modStr = existing.Mods;

                var mod = rulesetInstance.CreateModFromAcronym(modStr);

                Mods.Value = mod != null ? rulesetInstance.ConvertToLegacyMods(new[] { mod }) : rulesetInstance.ConvertToLegacyMods(itemMods);
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
    }
}
