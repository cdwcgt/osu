// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Game.Screens.Play.HUD;
using osu.Game.Tournament.IPC;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class TournamentMatchScoreDisplay : MatchScoreDisplay
    {
        private bool invertTextColor;

        [Resolved]
        private MatchIPCInfo ipc { get; set; } = null!;

        public bool InvertTextColor
        {
            get => invertTextColor;
            set
            {
                invertTextColor = value;
                updateColor();
            }
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Team1Score.BindTo(ipc.Score1);
            Team2Score.BindTo(ipc.Score2);
        }

        private void updateColor()
        {
            Score1Text.Background.FadeIn(50);
            Score2Text.Background.FadeIn(50);
            ScoreDiffText.Background.FadeIn(50);
            ScoreDiffText.SuccessIcon.FadeIn(50);

            Score1Text.DrawableCount.Colour = Score2Text.DrawableCount.Colour = ScoreDiffText.DrawableCount.Colour = Color4Extensions.FromHex("383838");
        }
    }
}
