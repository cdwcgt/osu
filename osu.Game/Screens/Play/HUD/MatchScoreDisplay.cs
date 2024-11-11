// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osuTK;

namespace osu.Game.Screens.Play.HUD
{
    public partial class MatchScoreDisplay : CompositeDrawable
    {
        private const float bar_height = 18;
        private const float font_size = 50;

        public BindableLong Team1Score = new BindableLong();
        public BindableLong Team2Score = new BindableLong();

        protected MatchScoreCounter Score1Text = null!;
        protected MatchScoreCounter Score2Text = null!;

        private Drawable score1Bar = null!;
        private Drawable score2Bar = null!;

        protected MatchScoreDiffCounter ScoreDiffText = null!;

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            InternalChildren = new[]
            {
                new Box
                {
                    Name = "top bar red (static)",
                    RelativeSizeAxes = Axes.X,
                    Height = bar_height / 4,
                    Width = 0.5f,
                    Colour = colours.TeamColourRed,
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopRight
                },
                new Box
                {
                    Name = "top bar blue (static)",
                    RelativeSizeAxes = Axes.X,
                    Height = bar_height / 4,
                    Width = 0.5f,
                    Colour = colours.TeamColourBlue,
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopLeft
                },
                score1Bar = new Box
                {
                    Name = "top bar red",
                    RelativeSizeAxes = Axes.X,
                    Height = bar_height,
                    Width = 0,
                    Colour = colours.TeamColourRed,
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopRight
                },
                score2Bar = new Box
                {
                    Name = "top bar blue",
                    RelativeSizeAxes = Axes.X,
                    Height = bar_height,
                    Width = 0,
                    Colour = colours.TeamColourBlue,
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopLeft
                },
                new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Height = font_size + bar_height,
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Children = new Drawable[]
                    {
                        Score1Text = new MatchScoreCounter
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre
                        },
                        Score2Text = new MatchScoreCounter
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre
                        },
                    }
                },
                ScoreDiffText = new MatchScoreDiffCounter
                {
                    Anchor = Anchor.TopCentre,
                    Margin = new MarginPadding
                    {
                        Top = bar_height / 4,
                        Horizontal = 8
                    },
                    Alpha = 0
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Team1Score.BindValueChanged(_ => updateScores());
            Team2Score.BindValueChanged(_ => updateScores(), true);
        }

        private void updateScores()
        {
            Score1Text.Current.Value = Team1Score.Value;
            Score2Text.Current.Value = Team2Score.Value;

            int comparison = Team1Score.Value.CompareTo(Team2Score.Value);

            if (comparison > 0)
            {
                Score1Text.Winning = true;
                Score2Text.Winning = false;
            }
            else if (comparison < 0)
            {
                Score1Text.Winning = false;
                Score2Text.Winning = true;
            }
            else
            {
                Score1Text.Winning = false;
                Score2Text.Winning = false;
            }

            var winningBar = Team1Score.Value > Team2Score.Value ? score1Bar : score2Bar;
            var losingBar = Team1Score.Value <= Team2Score.Value ? score1Bar : score2Bar;

            long diff = Math.Max(Team1Score.Value, Team2Score.Value) - Math.Min(Team1Score.Value, Team2Score.Value);

            losingBar.ResizeWidthTo(0, 400, Easing.OutQuint);
            winningBar.ResizeWidthTo(Math.Min(0.4f, MathF.Pow(diff / 1500000f, 0.5f) / 2), 400, Easing.OutQuint);

            ScoreDiffText.Alpha = diff != 0 ? 1 : 0;
            ScoreDiffText.Current.Value = -diff;
            ScoreDiffText.Origin = Team1Score.Value > Team2Score.Value ? Anchor.TopLeft : Anchor.TopRight;
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();
            Score1Text.X = -Math.Max(5 + Score1Text.DrawWidth / 2, score1Bar.DrawWidth);
            Score2Text.X = Math.Max(5 + Score2Text.DrawWidth / 2, score2Bar.DrawWidth);
        }

        protected partial class MatchScoreCounter : CommaSeparatedScoreCounter
        {
            private OsuSpriteText displayedSpriteText = null!;

            public Box Background { get; } = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Alpha = 0f,
                Depth = 1,
                Colour = Color4Extensions.FromHex("FFB405"),
            };

            public MatchScoreCounter()
            {
                Margin = new MarginPadding { Top = bar_height - 3, Horizontal = 10 };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                AddInternal(Background);
            }

            public bool Winning
            {
                set => updateFont(value);
            }

            protected override OsuSpriteText CreateSpriteText() => base.CreateSpriteText().With(s =>
            {
                displayedSpriteText = s;
                displayedSpriteText.Spacing = new Vector2(-6);
                updateFont(false);
            });

            private void updateFont(bool winning)
                => displayedSpriteText.Font = winning
                    ? OsuFont.Torus.With(weight: FontWeight.Bold, size: font_size, fixedWidth: true)
                    : OsuFont.Torus.With(weight: FontWeight.Regular, size: font_size * 0.8f, fixedWidth: true);
        }

        protected partial class MatchScoreDiffCounter : CommaSeparatedScoreCounter
        {
            public Box Background { get; }
            public SpriteIcon WarningIcon { get; }

            protected override Container<Drawable> Content { get; } = new Container
            {
                AutoSizeAxes = Axes.Both
            };

            public MatchScoreDiffCounter()
            {
                AutoSizeAxes = Axes.Both;
                InternalChild = new Container
                {
                    AutoSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        Background = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Alpha = 0,
                            Colour = Color4Extensions.FromHex("FFB405"),
                        },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Padding = new MarginPadding { Horizontal = 2f },
                            Children = new Drawable[]
                            {
                                Content,
                                WarningIcon = new SpriteIcon
                                {
                                    Size = new Vector2(16),
                                    Icon = FontAwesome.Solid.ExclamationTriangle,
                                    Colour = Color4Extensions.FromHex("383838"),
                                }
                            }
                        }
                    }
                };
            }

            protected override OsuSpriteText CreateSpriteText() => base.CreateSpriteText().With(s =>
            {
                s.Spacing = new Vector2(-2);
                s.Font = OsuFont.Torus.With(weight: FontWeight.Regular, size: bar_height, fixedWidth: true);
            });
        }
    }
}
