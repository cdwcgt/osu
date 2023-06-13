// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Beatmaps;
using System.Collections.Generic;
using System;
using osu.Framework.Bindables;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Replays.Mover;

namespace osu.Game.Rulesets.Osu.Mods
{
    public class OsuModFlowerDance : Mod, ICreateReplayData, IApplicableFailOverride
    {
        public override string Name => "Flower Dance";

        public override string Acronym => "FD";

        public override ModType Type => ModType.Automation;

        public override LocalisableString Description => "Enjoy the beautiful curve.";
        public override double ScoreMultiplier => 1;

        public bool PerformFail() => false;

        public bool RestartOnFail => false;

        public override bool UserPlayable => false;
        public override bool ValidForMultiplayer => false;
        public override bool ValidForMultiplayerAsFreeMod => false;

        public override bool RequiresConfiguration => true;

        [SettingSource("Jump multiplier")]
        public BindableFloat JumpMultiplier { get; set; } = new BindableFloat
        {
            Value = 0.6f,
            Default = 0.6f,
            MinValue = 0f,
            MaxValue = 2f,
            Precision = 0.01f,
        };

        [SettingSource("Angle offset")]
        public BindableFloat AngleOffset { get; set; } = new BindableFloat
        {
            Value = 0.45f,
            Default = 0.45f,
            MinValue = 0f,
            MaxValue = 2f,
            Precision = 0.01f,
        };

        public override Type[] IncompatibleMods => new[] { typeof(ModCinema), typeof(ModRelax), typeof(ModFailCondition), typeof(ModNoFail), typeof(ModAdaptiveSpeed), typeof(OsuModMagnetised), typeof(OsuModRepel), typeof(OsuModAutopilot), typeof(OsuModSpunOut), typeof(OsuModAlternate), typeof(OsuModSingleTap), typeof(OsuModAutoplay), typeof(OsuModNoScope)};

        public ModReplayData CreateReplayData(IBeatmap beatmap, IReadOnlyList<Mod> mods)
            => new ModReplayData(new FlowerMover(beatmap, mods, JumpMultiplier.Value, AngleOffset.Value).Generate(), new ModCreatedUser { Username = "lazer!dance" });
    }
}
