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
using osu.Game.Tournament.Models;
using osuTK;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class TeamMultCoin : CompositeDrawable
    {
        private readonly Bindable<double?> coin = new Bindable<double?>();
        private readonly TeamColour colour;
        private RollingTextContainer counter = null!;
        private readonly Anchor anchor;

        public TeamMultCoin(Bindable<double?> coin, TeamColour colour)
        {
            this.coin.BindTo(coin);
            this.colour = colour;
            bool flip = colour == TeamColour.Blue;
            anchor = flip ? Anchor.BottomRight : Anchor.BottomLeft;
        }

        [BackgroundDependencyLoader]
        private void load(TextureStore store)
        {
            Height = 23f;
            Width = 150f;

            InternalChild = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = anchor,
                Origin = anchor,
                Children = new Drawable[]
                {
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
                            counter = new RollingTextContainer
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
            };

            coin.BindValueChanged(d =>
            {
                if (d.NewValue == null)
                {
                    counter.Current.Value = 0;
                    return;
                }

                counter.Current.Value = d.NewValue.Value;
            }, true);
        }

        public partial class RollingTextContainer : RollingCounter<double>
        {
            protected override double RollingDuration => 500;

            protected override IHasText CreateText()
            {
                return new TextContainer();
            }

            protected override LocalisableString FormatCount(double count) => count.ToString("N1");
        }

        protected partial class TextContainer : FillFlowContainer, IHasText
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

            public TextContainer()
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
