// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Tournament.Models;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Gameplay.Components.MatchHeader
{
    public partial class TeamScore : CompositeDrawable
    {
        private readonly Bindable<int?> currentTeamScore = new Bindable<int?>();
        private readonly TournamentSpriteText counterText;
        private readonly Box background;

        public TeamScore(Bindable<int?> score, TeamColour colour)
        {
            bool flip = colour == TeamColour.Blue;

            Width = 50;
            Height = 24;

            InternalChildren = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4Extensions.FromHex("#383838")
                },
                counterText = new TournamentSpriteText()
            };

            currentTeamScore.BindValueChanged(scoreChanged);
            currentTeamScore.BindTo(score);
        }

        private void scoreChanged(ValueChangedEvent<int?> score) => counterText.Text = score.NewValue?.ToString() ?? "";
    }
}
