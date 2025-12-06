// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Replays;
using osu.Game.Screens.Play;

namespace osu.Game.Rulesets.Mods
{
    public abstract class ModAutoplay : Mod, ICreateReplayData, IApplicableToPlayer
    {
        public override string Name => "Autoplay";
        public override string Acronym => "AT";
        public override IconUsage? Icon => OsuIcon.ModAutoplay;
        public override ModType Type => ModType.Automation;
        public override LocalisableString Description => "Watch a perfect automated play through the song.";
        public override double ScoreMultiplier => 1;

        public sealed override bool UserPlayable => false;
        public sealed override bool ValidForMultiplayer => false;
        public sealed override bool ValidForMultiplayerAsFreeMod => false;

        public override Type[] IncompatibleMods => new[] { typeof(ModCinema), typeof(ModRelax), typeof(ModAdaptiveSpeed), typeof(ModTouchDevice) };

        public override bool HasImplementation => GetType().GenericTypeArguments.Length == 0;

        [SettingSource("Save score")]
        public Bindable<bool> SaveScore { get; } = new BindableBool();

        public virtual ModReplayData CreateReplayData(IBeatmap beatmap, IReadOnlyList<Mod> mods) => new ModReplayData(new Replay(), new ModCreatedUser { Username = @"autoplay" });

        public virtual void ApplyToPlayer(Player player)
        {
            if (player is ReplayPlayer replayPlayer)
            {
                replayPlayer.SaveScore = SaveScore.Value;
            }
        }
    }
}
