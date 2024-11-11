// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Game.Screens.Play.HUD;
using osu.Game.Tournament.Components;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class TournamentMatchScoreDisplay : MatchScoreDisplay
    {
        private bool invertTextColor;
        private readonly Colour4 black = Colour4.FromHex("1f1f1f");

        [Resolved]
        private RoundInfo roundInfo { get; set; } = null!;

        public readonly BindableBool WaitForResult = new BindableBool();

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
            Team1Score.BindTo(roundInfo.Score1);
            Team2Score.BindTo(roundInfo.Score2);

            WaitForResult.BindValueChanged(_ => updateColor());
        }

        private void updateColor()
        {
            if (!WaitForResult.Value)
            {
                Score1Text.Background.FadeOut(50);
                Score2Text.Background.FadeOut(50);
                ScoreDiffText.Background.FadeOut(50);
                ScoreDiffText.WarningIcon.FadeOut(50);
                var color = invertTextColor ? black : Colour4.White;
                Score1Text.DrawableCount.Colour = Score2Text.DrawableCount.Colour = ScoreDiffText.DrawableCount.Colour = color;
                return;
            }

            Score1Text.Background.FadeIn(50);
            Score2Text.Background.FadeIn(50);
            ScoreDiffText.Background.FadeIn(50);
            ScoreDiffText.WarningIcon.FadeIn(50);

            Score1Text.DrawableCount.Colour = Score2Text.DrawableCount.Colour = ScoreDiffText.DrawableCount.Colour = Color4Extensions.FromHex("333333");
        }
    }
}
