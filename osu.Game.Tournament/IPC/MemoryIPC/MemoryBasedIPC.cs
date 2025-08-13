// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Runtime.Versioning;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.EnumExtensions;
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

            var team1ById = (ladder.CurrentMatch.Value?.Team1.Value?.Players ?? Enumerable.Empty<TournamentUser>())
                            .DistinctBy(u => u.OnlineID)
                            .ToDictionary(u => u.OnlineID);

            var team2ById = (ladder.CurrentMatch.Value?.Team2.Value?.Players ?? Enumerable.Empty<TournamentUser>())
                            .DistinctBy(u => u.OnlineID)
                            .ToDictionary(u => u.OnlineID);

            Score1.Value = SlotPlayers.Sum(s => team1ById.TryGetValue(s.OnlineID.Value, out var u)
                ? calculateFinalScore(s, u)
                : 0);
            Score2.Value = SlotPlayers.Sum(s => team2ById.TryGetValue(s.OnlineID.Value, out var u)
                ? calculateFinalScore(s, u)
                : 0);

            long calculateFinalScore(SlotPlayerStatus s, TournamentUser u) =>
                Math.Min(getMaxScore(s.Mods.Value),
                    (long)(s.Score.Value * getModMultiplier(s.Mods.Value) * u.PlayerMultiplier));

            double getModMultiplier(LegacyMods mods) => ladder.ModMultiplierSettings.Where(m => (m.Mods.Value & mods) > LegacyMods.None).Aggregate(1.0, (i, v) => i * v.Multiplier.Value);

            long getMaxScore(LegacyMods mods) => (long)(1_000_000 * GetOriginalModMultiplier(mods));
        }

        public static double GetOriginalModMultiplier(LegacyMods mods)
        {
            double multiplier = 1;

            if (mods.HasFlagFast(LegacyMods.Hidden))
                multiplier *= 1.06;

            if (mods.HasFlagFast(LegacyMods.HardRock))
                multiplier *= 1.1;

            if (mods.HasFlagFast(LegacyMods.DoubleTime))
                multiplier *= 1.2;

            if (mods.HasFlagFast(LegacyMods.Flashlight))
                multiplier *= 1.12;

            if (mods.HasFlagFast(LegacyMods.SpunOut))
                multiplier *= 0.9;

            if (mods.HasFlagFast(LegacyMods.Easy))
                multiplier *= 0.5;

            return multiplier;
        }
    }
}
