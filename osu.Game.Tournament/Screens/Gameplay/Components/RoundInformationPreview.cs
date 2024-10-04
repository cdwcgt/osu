// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Overlays.Music;
using osu.Game.Tournament.Models;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class RoundInformationPreview : CompositeDrawable
    {
        private readonly FillFlowContainer mapContentContainer;
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
                        mapContentContainer = new FillFlowContainer
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal
                        }
                    }
                }
            };

            var mapDetail = new MapDetailContent();
            mapContentContainer.Add(mapDetail);
            List<BeatmapChoice> choices = new List<BeatmapChoice>();

            for (int i = 0; i < 4; i++)
            {
                choices.Add(new BeatmapChoice
                {
                    Team = TeamColour.Red
                });
            }

            Scheduler.Add(() =>
            {
                mapDetail.UpdateBeatmap(choices);
            });
        }

        private static Drawable createHeaderSection(string text)
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

        private static Drawable createMapBoxContent(string mapName, Color4 backgroundColor, Color4 textColor)
        {
            return new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = backgroundColor,
                    },
                    new TournamentSpriteText
                    {
                        Text = mapName,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Colour = textColor,
                        Font = OsuFont.Torus.With(size: 25),
                        Shadow = true,
                    }
                }
            };
        }

        private partial class MapDetailContent : CompositeDrawable
        {
            private FillFlowContainer mapContent = null!;

            [Resolved]
            private LadderInfo ladderInfo { get; set; } = null!;

            [BackgroundDependencyLoader]
            private void load()
            {
                AutoSizeAxes = Axes.Both;
                Margin = new MarginPadding(16);

                InternalChild = new FillFlowContainer
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        createHeaderSection("禁图"),
                        mapContent = new FillFlowContainer
                        {
                            Spacing = new Vector2(20),
                            Direction = FillDirection.Horizontal,
                            AutoSizeAxes = Axes.Both,
                        }
                    }
                };
            }

            public void UpdateBeatmap(IEnumerable<BeatmapChoice> maps)
            {
                if (ladderInfo.CurrentMatch.Value?.Round.Value == null)
                    return;

                mapContent.Clear();

                mapContent.ChildrenEnumerable = maps.Select(map =>
                {
                    var mapBox = new MapBox();

                    //var roundBeatmap = ladderInfo.CurrentMatch.Value.Round.Value?.Beatmaps.FirstOrDefault(roundMap => roundMap.ID == map.BeatmapID);
                    var roundBeatmap = ladderInfo.CurrentMatch.Value.Round.Value?.Beatmaps.FirstOrDefault();
                    if (roundBeatmap == null)
                        return mapBox;

                    mapBox.CenterLine.Colour = map.Team == TeamColour.Red
                        ? new Color4(212, 48, 48, 255)
                        : new Color4(42, 130, 228, 255);

                    Color4 backgroundColor = map.Type == ChoiceType.Ban ? Color4.Gray : roundBeatmap.BackgroundColor;
                    Color4 textColor = map.Type == ChoiceType.Ban ? new Color4(229, 229, 229, 255) : roundBeatmap.TextColor;
                    var mapBoxContent = createMapBoxContent(roundBeatmap.Mods, backgroundColor, textColor);

                    if (map.Team == TeamColour.Red)
                    {
                        mapBox.BottomMapContainer.Add(mapBoxContent);
                    }
                    else
                    {
                        mapBox.TopMapContainer.Add(mapBoxContent);
                    }

                    return mapBox;
                });
            }
        }

        private partial class MapBox : CompositeDrawable
        {
            public MapBox()
            {
                Height = 50;
                Width = 42;

                InternalChildren = new Drawable[]
                {
                    TopMapContainer = new Container
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Height = 18,
                        RelativeSizeAxes = Axes.X,
                    },
                    CenterLine = new Box
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Height = 3.6f,
                        RelativeSizeAxes = Axes.X,
                        Colour = Color4.Gray,
                    },
                    BottomMapContainer = new Container
                    {
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre,
                        Height = 18,
                        RelativeSizeAxes = Axes.X,
                    },
                };
            }

            public Container TopMapContainer { get; set; }

            public Container BottomMapContainer { get; set; }

            public Box CenterLine { get; set; }
        }
    }
}
