// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Runtime.Versioning;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.IPC.MemoryIPC
{
    // 在未来应该完全脱离FileBased
    // 但是先这样吧
    [SupportedOSPlatform("windows")]
    public partial class MemoryBasedIPC : FileBasedIPC, IProvideAdditionalData
    {
        public override bool ReadScoreFromFile => false;

        public SlotPlayerStatus[] SlotPlayers { get; } = Enumerable.Range(0, 8).Select(i => new SlotPlayerStatus()).ToArray();
        BindableList<TourneyChatItem> IProvideAdditionalData.TourneyChat => throw new NotImplementedException(); //= new BindableList<TourneyChatItem>();

        [Resolved]
        private LadderInfo ladder { get; set; } = null!;

        private readonly BindableInt playersPerTeam = new BindableInt
        {
            MinValue = 1,
            MaxValue = 4,
        };

        private readonly StableMemoryReader[] readers;

        public MemoryBasedIPC()
        {
            readers = Enumerable.Range(0, 8).Select(i => new StableMemoryReader()).ToArray();
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            playersPerTeam.BindTo(ladder.PlayersPerTeam);
        }

        protected override void Update()
        {
            base.Update();

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
                }
            }

            int[] team1OnlineId = ladder.CurrentMatch.Value?.Team1.Value?.Players.Select(p => p.OnlineID).ToArray() ??
                                  Array.Empty<int>();
            int[] team2OnlineId = ladder.CurrentMatch.Value?.Team2.Value?.Players.Select(p => p.OnlineID).ToArray() ??
                                  Array.Empty<int>();

            Score1.Value = SlotPlayers.Where(s => team1OnlineId.Any(t => t == s.OnlineID.Value)).Sum(calculateModMultiplier);
            Score2.Value = SlotPlayers.Where(s => team2OnlineId.Any(t => t == s.OnlineID.Value)).Sum(calculateModMultiplier);

            long calculateModMultiplier(SlotPlayerStatus s)
            {
                return (long)(s.Score.Value * (ladder.ModMultiplierSettings.FirstOrDefault(m => (m.Mods.Value & s.Mods.Value) > LegacyMods.None)?.Multiplier.Value ?? 1.0));
            }
        }
    }
}
