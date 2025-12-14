// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Logging;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Chat;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.IPC.MemoryIPC
{
    [SupportedOSPlatform("windows")]
    public partial class MemoryBasedIPC : MatchIPCInfo, IProvideAdditionalData
    {
        private int lastBeatmapId;
        private GetBeatmapRequest? beatmapLookupRequest;

        public IBindable<bool> Available => available;

        private readonly BindableBool available = new BindableBool();

        public SlotPlayerStatus[] SlotPlayers { get; } = Enumerable.Range(0, 8).Select(i => new SlotPlayerStatus()).ToArray();
        public Bindable<Channel> TourneyChatChannel { get; } = new Bindable<Channel>();

        [Resolved]
        protected LadderInfo Ladder { get; private set; } = null!;

        public BindableInt Team1Combo { get; } = new BindableInt();
        public BindableInt Team2Combo { get; } = new BindableInt();

        [Resolved]
        protected IAPIProvider API { get; private set; } = null!;

        public bool FetchDataFromMemory { get; set; }

        private readonly BindableInt playersPerTeam = new BindableInt
        {
            MinValue = 1,
            MaxValue = 4,
        };

        private StableMemoryReader[] readers;
        private TourneyManagerMemoryReader tourneyManagerMemoryReader;

        public MemoryBasedIPC()
        {
            readers = Enumerable.Range(0, 8).Select(i => new StableMemoryReader()).ToArray();
            tourneyManagerMemoryReader = new TourneyManagerMemoryReader();

            ChatChannel.BindValueChanged(c =>
            {
                TourneyChatChannel.Value = new Channel
                {
                    Name = "mp",
                    Id = c.NewValue,
                    Type = ChannelType.Private
                };
            }, true);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            playersPerTeam.BindTo(Ladder.PlayersPerTeam);
        }

        private const int update_hz = 5;
        private double lastUpdateTime;

        public void Reset()
        {
            foreach (var reader in readers)
            {
                reader.Dispose();
            }

            readers = Enumerable.Range(0, 8).Select(i => new StableMemoryReader()).ToArray();

            tourneyManagerMemoryReader.Dispose();
            tourneyManagerMemoryReader = new TourneyManagerMemoryReader();
        }

        private void updateTourneyData()
        {
            var reader = tourneyManagerMemoryReader;

            try
            {
                State.Value = reader.GetTourneyState();
                LegacyMods mods = Mods.Value = reader.GetMods();

                int beatmapId = reader.GetBeatmapId();

                if (beatmapId > 0 && lastBeatmapId != beatmapId)
                {
                    beatmapLookupRequest?.Cancel();

                    lastBeatmapId = beatmapId;

                    var existing = Ladder.CurrentMatch.Value?.Round.Value?.Beatmaps.FirstOrDefault(b => b.ID == beatmapId);

                    if (existing != null)
                    {
                        Beatmap.Value = existing.Beatmap;
                        var ruleset = Ladder.Ruleset.Value?.CreateInstance();
                        string modStr = existing.Mods;

                        var mod = ruleset!.CreateModFromAcronym(modStr);

                        Mods.Value = mod != null ? ruleset.ConvertToLegacyMods(new[] { mod }) : mods;
                    }
                    else
                    {
                        beatmapLookupRequest = new GetBeatmapRequest(new APIBeatmap { OnlineID = beatmapId });
                        beatmapLookupRequest.Success += b =>
                        {
                            if (lastBeatmapId == beatmapId)
                                Beatmap.Value = new TournamentBeatmap(b);
                        };
                        beatmapLookupRequest.Failure += _ =>
                        {
                            if (lastBeatmapId == beatmapId)
                                Beatmap.Value = null;
                        };
                        API.Queue(beatmapLookupRequest);
                        Mods.Value = mods;
                    }
                }

                ChatChannel.Value = (int)reader.GetChannelId();
                updateMessageList(reader.GetTourneyChat() ?? new List<Message>());
            }
            catch (InvalidOperationException)
            {
                if (reader.Status == AttachStatus.UnAttached)
                {
                    Logger.Log("Attempt fetch data when Unattached. Tourney Manager");
                    return;
                }

                throw;
            }
        }

        private void updateMessageList(List<Message> tourneyChatItems)
        {
            var channel = TourneyChatChannel.Value;

            var toRemove = channel.Messages.Except(tourneyChatItems).ToArray();
            foreach (var item in toRemove)
                channel.Messages.Remove(item);

            if (toRemove.Length > 0)
            {
                Logger.Log($"memory: deleted {toRemove.Length} message items");
            }

            var toAdd = tourneyChatItems.Except(channel.Messages).ToArray();
            channel.AddNewMessages(toAdd);

            if (toAdd.Length > 0)
            {
                Logger.Log($"memory: add {toAdd.Length} message items");
            }
        }

        protected override void Update()
        {
            base.Update();

            lastUpdateTime += Time.Elapsed;

            if (lastUpdateTime < 1000.0 / update_hz)
                return;

            lastUpdateTime = 0;

            switch (tourneyManagerMemoryReader.Status)
            {
                case AttachStatus.UnAttached:
                    tourneyManagerMemoryReader.AttachToProcessByTitleNameAsync(" Tournament Manager");
                    available.Value = false;
                    break;

                case AttachStatus.Initializing:
                    available.Value = false;
                    break;

                case AttachStatus.Attached:
                    updateTourneyData();
                    available.Value = true;
                    break;
            }

            for (int i = 0; i < playersPerTeam.Value * 2; i++)
            {
                var reader = readers[i];
                var player = SlotPlayers[i];

                switch (reader.Status)
                {
                    case AttachStatus.UnAttached:
                        reader.AttachToProcessByTitleNameAsync($"{TournamentGame.TOURNAMENT_CLIENT_NAME}{i}");
                        continue;

                    case AttachStatus.Initializing:
                        continue;

                    case AttachStatus.Attached:
                    {
                        if (!FetchDataFromMemory)
                            return;

                        try
                        {
                            var user = reader.GetTournamentUser();
                            if (user == null)
                                continue;

                            player.OnlineID.Value = user.OnlineID;

                            var gameplayData = reader.GetGameplayData();
                            if (gameplayData == null)
                                continue;

                            player.Accuracy.Value = gameplayData.Accuracy / 100;
                            player.Combo.Value = gameplayData.Combo;
                            player.MaxCombo.Value = gameplayData.MaxCombo;
                            player.Hit50.Value = gameplayData.Hit50;
                            player.Hit100.Value = gameplayData.Hit100;
                            player.Hit300.Value = gameplayData.Hit300;
                            player.HitGeki.Value = gameplayData.HitGeki;
                            player.HitKatu.Value = gameplayData.HitKatu;
                            player.HitMiss.Value = gameplayData.HitMiss;
                            player.Mods.Value = gameplayData.Mods;
                            player.Score.Value = gameplayData.Score;
                            continue;
                        }
                        catch (InvalidOperationException)
                        {
                            if (reader.Status == AttachStatus.UnAttached)
                            {
                                Logger.Log($"Attempt fetch data when Unattached. {TournamentGame.TOURNAMENT_CLIENT_NAME}{i}");
                                continue;
                            }

                            throw;
                        }
                    }
                }
            }

            UpdateScore();
        }

        protected void UpdateScore()
        {
            Score1.Value = GetTeamScore(TeamColour.Red).Sum(CalculateModMultiplier);
            Score2.Value = GetTeamScore(TeamColour.Blue).Sum(CalculateModMultiplier);

            Team1Combo.Value = getCombo(TeamColour.Red);
            Team2Combo.Value = getCombo(TeamColour.Blue);
        }

        protected long CalculateModMultiplier(PlayerScore s)
        {
            return (long)(s.Score * (Ladder.ModMultiplierSettings.FirstOrDefault(m => (m.Mods.Value & s.Mods) > LegacyMods.None)?.Multiplier.Value ?? 1.0));
        }

        protected virtual IEnumerable<PlayerScore> GetTeamScore(TeamColour colour)
        {
            int[] teamIds = GetTeamIds(colour);

            return SlotPlayers.Where(s => teamIds.Any(t => t == s.OnlineID.Value)).Select(s => new PlayerScore
            {
                OnlineId = s.OnlineID.Value,
                Score = s.Score.Value,
                Mods = s.Mods.Value
            });
        }

        protected int[] GetTeamIds(TeamColour colour)
        {
            return Ladder.CurrentMatch.Value?.GetTeamByColor(colour)?.Players.Select(p => p.OnlineID).ToArray() ??
                   Array.Empty<int>();
        }

        private int getCombo(TeamColour colour)
        {
            int[] teamIds = GetTeamIds(colour);

            return SlotPlayers.Where(s => teamIds.Any(t => t == s.OnlineID.Value)).Select(s => s.Combo.Value).Sum();
        }
    }

    public struct PlayerScore
    {
        public int OnlineId;
        public long Score;
        public LegacyMods Mods;
    }
}
