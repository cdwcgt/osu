// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class ScoreModeTeamScore : RollingCounter<int>
    {
        private readonly Bindable<int?> score =  new Bindable<int?>();
        protected override double RollingDuration => 0;

        public ScoreModeTeamScore(Bindable<int?> score, TeamColour colour)
        {
            bool flip = colour == TeamColour.Blue;
            var anchor = flip ? Anchor.TopRight : Anchor.TopLeft;

            Margin = new MarginPadding { Horizontal = 5f };

            AutoSizeAxes = Axes.Both;
            Anchor = anchor;
            Origin = anchor;

            this.score.BindValueChanged(s =>
            {
                Current.Value = s.NewValue ?? 0;
            }, true);
            this.score.BindTo(score);
        }

        protected override OsuSpriteText CreateSpriteText() => new OsuSpriteText
        {
            Font = OsuFont.Torus.With(size: 30f),
        };
    }
}
