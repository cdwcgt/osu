// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Tournament.Models;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Gameplay.Components.MatchHeader
{
    public partial class TeamScore : CompositeDrawable
    {
        private const float side_width = 6;
        private readonly TeamColour colour;
        private readonly int pointToWin;
        private readonly Bindable<int?> currentTeamScore = new Bindable<int?>();
        private readonly TournamentSpriteText counterText;
        private readonly Container background;

        private bool showScore;

        public bool ShowScore
        {
            get => showScore;
            set
            {
                if (showScore == value)
                    return;

                showScore = value;

                if (IsLoaded)
                    updateDisplay();
            }
        }

        public TeamScore(Bindable<int?> score, TeamColour colour, int pointToWin)
        {
            this.colour = colour;
            this.pointToWin = pointToWin;

            Width = 50 + 2 * side_width;
            Height = 24;

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Y,
                    Width = side_width,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Colour = TournamentGame.GetTeamColour(colour)
                },
                new Box
                {
                    RelativeSizeAxes = Axes.Y,
                    Width = side_width,
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Colour = TournamentGame.GetTeamColour(colour)
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = background = new Container
                    {
                        Padding = new MarginPadding { Horizontal = side_width },
                        Colour = Color4Extensions.FromHex("#383838"),
                        RelativeSizeAxes = Axes.Both,
                        Masking = true,
                        Child = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.White,
                        },
                    },
                },
                counterText = new TournamentSpriteText
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Colour = Color4.White,
                    Font = OsuFont.Torus.With(size: 25),
                }
            };

            currentTeamScore.BindTo(score);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            currentTeamScore.BindValueChanged(_ => updateDisplay(), true);
        }

        private void updateDisplay()
        {
            if (!ShowScore || currentTeamScore.Value == null)
            {
                resetDisplay();
                return;
            }

            counterText.Text = currentTeamScore.Value.Value.ToString();

            bool isWinning = currentTeamScore.Value >= pointToWin;
            background.FadeColour(Color4Extensions.FromHex(isWinning ? "#FFE8AD" : "#383838"), 100);
            counterText.FadeColour(Color4Extensions.FromHex(isWinning ? "#2E2E2E" : "#FFFFFF"), 100);

            if (isWinning)
            {
                background.EdgeEffect = new EdgeEffectParameters
                {
                    Type = EdgeEffectType.Glow,
                    Colour = Color4Extensions.FromHex("#FFE8AD").Opacity(0.25f),
                    Radius = 20,
                    Roundness = 10,
                };
            }
            else
            {
                background.EdgeEffect = new EdgeEffectParameters();
            }
        }

        private void resetDisplay()
        {
            background.FadeColour(Color4Extensions.FromHex("#383838"), 100);
            counterText.FadeColour(Color4.White, 100);
            counterText.Text = "-";
        }
    }
}
