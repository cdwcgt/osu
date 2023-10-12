// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Screens;
using osu.Game.Scoring;
using osu.Game.Screens.Menu;
using osu.Game.Screens.OnlinePlay.Multiplayer.Spectate;
using osu.Game.Screens.Play;

namespace osu.Game.Screens.ReplayVs
{
    /// <summary>
    /// Used to load a single <see cref="MultiSpectatorPlayer"/> in a <see cref="MultiSpectatorScreen"/>.
    /// </summary>
    public partial class ReplayVsPlayerLoader : PlayerLoader
    {
        public readonly ScoreInfo Score;

        public ReplayVsPlayerLoader([NotNull] Score score, [NotNull] Func<ReplayVsPlayer> createPlayer)
            : base(createPlayer)
        {
            if (score.Replay == null)
                throw new ArgumentException($"{nameof(score)} must have a non-null {nameof(score.Replay)}.", nameof(score));

            Score = score.ScoreInfo;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            PlayerSettings.Expire();
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            // these will be reverted thanks to PlayerLoader's lease.
            Mods.Value = Score.Mods;
            Ruleset.Value = Score.Ruleset;

            base.OnEntering(e);
        }

        protected override void LogoArriving(OsuLogo logo, bool resuming)
        {
        }

        protected override void LogoExiting(OsuLogo logo)
        {
        }
    }
}
