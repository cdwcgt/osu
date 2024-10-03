// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class RoundInformationPreview : CompositeDrawable
    {
        private static readonly Color4 boarder_color = new Color4(56, 56, 56, 255);

        public RoundInformationPreview()
        {
            Width = 1030;
            Height = 110;
            Masking = true;

            BorderColour = boarder_color;
            BorderThickness = 2f;

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    Name = "backgroud",
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(229, 229, 229, 255)
                },
                new Container
                {
                    Name = "左侧小框",
                    Width = 30f,
                    RelativeSizeAxes = Axes.Y,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = boarder_color
                        },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Y,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Spacing = new Vector2(5),
                            Direction = FillDirection.Vertical,
                            Children = new Drawable[]
                            {
                                new TournamentSpriteText
                                {
                                    Text = "回",
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Font = OsuFont.Torus.With(size: 20)
                                },
                                new TournamentSpriteText
                                {
                                    Text = "合",
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Font = OsuFont.Torus.With(size: 20)
                                }
                            }
                        }
                    }
                },
                new Container
                {
                    Name = "右侧主要内容",
                    Width = 1000f,
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    RelativeSizeAxes = Axes.Y,
                    Children = new Drawable[]
                    {
                        new FillFlowContainer
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Vertical,
                            Children = new Drawable[]
                            {
                                createHeaderSection("禁图"),
                            }
                        }
                    }
                }
            };
        }

        private Drawable createHeaderSection(string text)
        {
            return new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                Height = 20f,
                Direction = FillDirection.Horizontal,
                Children = new Drawable[]
                {
                    // 左边的线条
                    new Box
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 2,
                        Colour = Color4.LightGray,
                        Width = 0.4f
                    },
                    // 文字
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Width = 0.2f,
                        Child = new TournamentSpriteText
                        {
                            Text = text,
                            Font = OsuFont.Torus.With(size: 20),
                            Colour = Color4.Gray,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                        },
                    },
                    // 右边的线条
                    new Box
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 2,
                        Colour = Color4.LightGray,
                        Width = 0.4f
                    }
                }
            };
        }

        private Drawable createMapBox(string mapName, Color4 color)
        {
            return new Container
            {
                AutoSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box
                    {
                        Size = new Vector2(60, 30),
                        Colour = color,
                    },
                    new TournamentSpriteText
                    {
                        Text = mapName,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Colour = Color4.White,
                        Font = OsuFont.Torus.With(size: 18)
                    }
                }
            };
        }
    }
}
