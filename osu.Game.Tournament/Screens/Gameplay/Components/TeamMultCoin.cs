// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Models;
using osuTK;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class TeamMultCoin : CompositeDrawable
    {
        private readonly Bindable<double?> coin = new Bindable<double?>();
        private readonly TeamColour colour;
        private RollingMultCoinContainer counter = null!;
        private readonly Anchor anchor;
        private readonly bool flip;
        private Container animationContainer = null!;
        private RollingSignNumberContainer diffContainer = null!;

        public TeamMultCoin(Bindable<double?> coin, TeamColour colour)
        {
            this.coin.BindTo(coin);
            this.colour = colour;
            flip = colour == TeamColour.Blue;
            anchor = flip ? Anchor.BottomRight : Anchor.BottomLeft;
        }

        [BackgroundDependencyLoader]
        private void load(TextureStore store)
        {
            Height = 23f;
            Width = 150f;

            InternalChildren = new Drawable[]
            {
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = anchor,
                    Origin = anchor,
                    Children = new Drawable[]
                    {
                        animationContainer = new Container
                        {
                            Alpha = 0,
                            Width = 45f,
                            Height = 20f,
                            Anchor = flip ? Anchor.CentreLeft : Anchor.CentreRight,
                            Origin = flip ? Anchor.CentreLeft : Anchor.CentreRight,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    Colour = TournamentGame.GetTeamColour(colour),
                                    RelativeSizeAxes = Axes.Both,
                                },
                                diffContainer = new RollingMultDiffNumberContainer
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre
                                }
                            }
                        },
                        new Box
                        {
                            Colour = TournamentGame.GetTeamColour(colour),
                            RelativeSizeAxes = Axes.Both,
                        },
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Anchor = anchor,
                            Origin = anchor,
                            Padding = new MarginPadding
                            {
                                Horizontal = 2f
                            },
                            Children = new Drawable[]
                            {
                                new Container
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Margin = new MarginPadding { Vertical = 3.5f, Horizontal = 6.4f },
                                    Anchor = anchor,
                                    Origin = anchor,
                                    Child = new Sprite
                                    {
                                        Texture = store.Get("multcoin"),
                                        Scale = new Vector2(0.04f),
                                        Anchor = anchor,
                                        Origin = anchor,
                                    },
                                },
                                counter = new RollingMultCoinContainer
                                {
                                    Anchor = anchor,
                                    Origin = anchor,
                                },
                                new TournamentSpriteText
                                {
                                    Margin = new MarginPadding { Bottom = 2f },
                                    Anchor = anchor,
                                    Origin = anchor,
                                    Font = OsuFont.Torus.With(size: 15),
                                    Text = "MultCoin"
                                }
                            }
                        }
                    }
                },
            };

            coin.BindValueChanged(d =>
            {
                Scheduler.AddOnce(() => triggerAnimation(d.OldValue ?? 0, d.NewValue ?? 0));
            }, true);
        }

        private void triggerAnimation(double oldAmount, double newAmount)
        {
            FinishTransforms(true);
            double diff = newAmount - oldAmount;
            diffContainer.DisplayedCount = diff;
            diffContainer.Current.Value = diff;
            counter.DisplayedCount = oldAmount;
            counter.Current.Value = oldAmount;
            animationContainer.FadeIn(500);
            animationContainer.MoveToX(flip ? -48 : 48, 500, Easing.OutElastic);

            using (BeginDelayedSequence(2000))
            {
                counter.Current.Value = newAmount;
                diffContainer.Current.Value = 0;
            }

            using (BeginDelayedSequence(4000))
            {
                animationContainer.MoveToX(0, 500, Easing.OutElastic);
                animationContainer.FadeOut(500);
            }
        }

        private partial class RollingMultDiffNumberContainer : RollingSignNumberContainer
        {
            protected override double RollingDuration => 1000;
        }

        private partial class RollingMultCoinContainer : RollingCounter<double>
        {
            protected override double RollingDuration => 1000;

            protected override IHasText CreateText()
            {
                return new MultCoinTextContainer();
            }

            protected override LocalisableString FormatCount(double count) => count.ToString("N2");
        }

        private partial class MultCoinTextContainer : FillFlowContainer, IHasText
        {
            public LocalisableString Text
            {
                get => wholePart.Text;
                set
                {
                    string[] split = value.ToString().Split(".");

                    wholePart.Text = split[0] + ".";
                    fractionPart.Text = split[1];
                }
            }

            private readonly SpriteText wholePart;
            private readonly SpriteText fractionPart;

            public MultCoinTextContainer()
            {
                AutoSizeAxes = Axes.X;
                RelativeSizeAxes = Axes.Y;

                Children = new Drawable[]
                {
                    wholePart = new OsuSpriteText
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        Font = OsuFont.Torus.With(size: 25),
                    },
                    fractionPart = new OsuSpriteText
                    {
                        Margin = new MarginPadding { Bottom = 1f },
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        Font = OsuFont.Torus.With(size: 20),
                    }
                };
            }
        }
    }
}
