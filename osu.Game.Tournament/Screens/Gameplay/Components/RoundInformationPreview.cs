// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Models;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class RoundInformationPreview : CompositeDrawable
    {
        [Resolved]
        private LadderInfo ladderInfo { get; set; } = null!;

        private readonly FillFlowContainer mapContentContainer;
        private readonly TournamentSpriteText mapCountText;
        private static readonly Color4 boarder_color = new Color4(56, 56, 56, 255);

        private const float cover_width = 50f;

        public RoundInformationPreview()
        {
            AutoSizeAxes = Axes.X;
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
                    Padding = new MarginPadding
                    {
                        Left = 30f,
                    },
                    Children = new Drawable[]
                    {
                        new BufferedContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                mapContentContainer = new FillFlowContainer
                                {
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                    AutoSizeAxes = Axes.Both,
                                    Direction = FillDirection.Horizontal
                                },
                                new Container
                                {
                                    Name = "cover",
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    RelativeSizeAxes = Axes.Both,
                                    Blending = new BlendingParameters
                                    {
                                        // Don't change the destination colour.
                                        RGBEquation = BlendingEquation.Add,
                                        Source = BlendingType.Zero,
                                        Destination = BlendingType.One,
                                        // Subtract the cover's alpha from the destination (points with alpha 1 should make the destination completely transparent).
                                        AlphaEquation = BlendingEquation.Add,
                                        SourceAlpha = BlendingType.Zero,
                                        DestinationAlpha = BlendingType.OneMinusSrcAlpha
                                    },
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                            RelativeSizeAxes = Axes.Y,
                                            Width = cover_width,
                                            Colour = ColourInfo.GradientHorizontal(
                                                Color4.White.Opacity(1f),
                                                Color4.White.Opacity(0f)
                                            )
                                        },
                                        new Box
                                        {
                                            Anchor = Anchor.CentreRight,
                                            Origin = Anchor.CentreRight,
                                            RelativeSizeAxes = Axes.Y,
                                            Width = cover_width,
                                            Colour = ColourInfo.GradientHorizontal(
                                                Color4.White.Opacity(0f),
                                                Color4.White.Opacity(1f)
                                            )
                                        },
                                    }
                                }
                            }
                        },
                        mapCountText = new TournamentSpriteText
                        {
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.BottomCentre,
                            Font = OsuFont.Torus.With(size: 12),
                            Colour = new Color4(79, 78, 78, 255),
                            Margin = new MarginPadding(5)
                        }
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            ladderInfo.CurrentMatch.BindValueChanged(matchChanged, true);
        }

        private bool mapContentReturnPosition;
        private bool mapContentNeedRoll;
        private double pauseTime;

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (!mapContentNeedRoll)
                return;

            if (pauseTime > 0)
            {
                pauseTime -= Clock.ElapsedFrameTime;
                return;
            }

            if (!mapContentReturnPosition && mapContentContainer.DrawWidth + mapContentContainer.X < 1000 - cover_width)
            {
                mapContentReturnPosition = true;
                pauseTime = 3000;
            }

            if (mapContentReturnPosition && mapContentContainer.X >= cover_width)
            {
                mapContentReturnPosition = false;
                pauseTime = 3000;
            }

            mapContentContainer.X = mapContentReturnPosition ? mapContentContainer.X + (float)(50 * Time.Elapsed / 1000) : mapContentContainer.X + (float)(-50 * Time.Elapsed / 1000);
        }

        private void matchChanged(ValueChangedEvent<TournamentMatch?> match)
        {
            if (match.OldValue != null)
                match.OldValue.PicksBans.CollectionChanged -= picksBansOnCollectionChanged;
            if (match.NewValue != null)
                match.NewValue.PicksBans.CollectionChanged += picksBansOnCollectionChanged;

            Scheduler.AddOnce(updateState);
        }

        private void picksBansOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            => Scheduler.AddOnce(updateState);

        private void updateState()
        {
            mapContentContainer.Clear();
            mapCountText.Text = string.Empty;

            if (ladderInfo.CurrentMatch.Value?.Round.Value == null)
                return;

            var banMapDetail = new MapDetailContent("禁图");
            BeatmapChoice?[] banChoices = ladderInfo.CurrentMatch.Value.PicksBans.Where(b => b.Type == ChoiceType.Ban).ToArray();
            var remainChoices = ladderInfo.CurrentMatch.Value.PicksBans.Except(banChoices);
            banChoices = banChoices.Concat(Enumerable.Repeat((BeatmapChoice?)null, (ladderInfo.CurrentMatch.Value.Round.Value?.BanCount.Value ?? 2) * 2 - banChoices.Length)).ToArray();

            var pickDetail = new MapDetailContent("选图");

            int pickMapCount = ladderInfo.CurrentMatch.Value.Round.Value.BestOf.Value - 1;

            var pickChoice = remainChoices.Take(pickMapCount)
                                          // 往后面填充null
                                          .Concat(Enumerable.Repeat((BeatmapChoice?)null, pickMapCount - remainChoices.Take(pickMapCount).Count()));

            mapContentContainer.Add(banMapDetail);
            mapContentContainer.Add(createDivideLine());
            mapContentContainer.Add(pickDetail);
            mapContentContainer.Add(createDivideLine());
            mapContentContainer.Add(createDivideLine());
            var TBMap = ladderInfo.CurrentMatch.Value?.Round.Value?.Beatmaps.FirstOrDefault(map => map.Mods == "TB");

            if (TBMap != null)
            {
                bool isTBSelected = remainChoices.Any(p => p?.BeatmapID == TBMap.ID);
                mapContentContainer.Add(createTBMapBox(isTBSelected));
            }

            int mapCount = ladderInfo.CurrentMatch.Value.Round.Value.Beatmaps.Count;
            int remainMapCount = mapCount - ladderInfo.CurrentMatch.Value.PicksBans.Count;

            mapCountText.Text = $"图池内谱面数量：{mapCount}  |  图池内剩余谱面：{remainMapCount}";

            Scheduler.Add(() =>
            {
                banMapDetail.UpdateBeatmap(banChoices);
                pickDetail.UpdateBeatmap(pickChoice);

                Scheduler.Add(() =>
                {
                    if (mapContentContainer.DrawWidth < 1000)
                    {
                        mapContentNeedRoll = false;
                        mapContentContainer.Anchor = Anchor.TopCentre;
                        mapContentContainer.Origin = Anchor.TopCentre;
                        mapContentContainer.X = 0;
                        return;
                    }

                    mapContentNeedRoll = true;

                    mapContentContainer.Anchor = Anchor.TopLeft;
                    mapContentContainer.Origin = Anchor.TopLeft;
                });
            });
        }

        private Box createDivideLine() => new Box
        {
            Colour = new Color4(79, 79, 79, 255),
            Height = 38.5f,
            Width = 1f,
            Margin = new MarginPadding
            {
                Top = 37f
            }
        };

        private MapBox createTBMapBox(bool isSelected)
        {
            MapBox mapbox = new MapBox();

            mapbox.Margin = new MarginPadding
            {
                Vertical = 36f,
                Horizontal = 16f
            };
            mapbox.CenterLine.Colour = isSelected ? new Color4(197, 60, 100, 255) : Color4.Gray;
            mapbox.TopMapContainer.Add(new TournamentSpriteText
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Text = "决胜局",
                Colour = new Color4(82, 79, 79, 255)
            });

            var TBMap = ladderInfo.CurrentMatch.Value?.Round.Value?.Beatmaps.FirstOrDefault(map => map.Mods == "TB");

            if (TBMap == null)
                return mapbox;

            mapbox.BottomMapContainer.Add(new TournamentModIcon("TB")
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                FillMode = FillMode.Fill
            });

            return mapbox;
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
                        Font = OsuFont.Torus.With(size: 20),
                        Shadow = true,
                    }
                }
            };
        }

        private partial class MapDetailContent : CompositeDrawable
        {
            private FillFlowContainer banMapContent = null!;

            [Resolved]
            private LadderInfo ladderInfo { get; set; } = null!;

            private readonly string headerName;

            public MapDetailContent(string headerName)
            {
                this.headerName = headerName;
            }

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
                        createHeaderSection(headerName),
                        banMapContent = new FillFlowContainer
                        {
                            Spacing = new Vector2(20),
                            Direction = FillDirection.Horizontal,
                            AutoSizeAxes = Axes.Both,
                        }
                    }
                };
            }

            public void UpdateBeatmap(IEnumerable<BeatmapChoice?> maps)
            {
                TournamentRound? round = ladderInfo.CurrentMatch.Value?.Round.Value;

                if (round == null)
                    return;

                banMapContent.Clear();

                banMapContent.ChildrenEnumerable = maps.Select(map =>
                {
                    var mapBox = new MapBox();

                    if (map == null)
                        return mapBox;

                    var roundBeatmap = round.Beatmaps.FirstOrDefault(roundMap => roundMap.ID == map.BeatmapID);
                    if (roundBeatmap == null)
                        return mapBox;

                    mapBox.CenterLine.Colour = map.Team == TeamColour.Red
                        ? new Color4(212, 48, 48, 255)
                        : new Color4(42, 130, 228, 255);

                    Color4 backgroundColor = map.Type == ChoiceType.Ban ? Color4.Gray : roundBeatmap.BackgroundColor;
                    Color4 textColor = map.Type == ChoiceType.Ban ? new Color4(229, 229, 229, 255) : roundBeatmap.TextColor;

                    var modArray = round.Beatmaps.Where(b => b.Mods == roundBeatmap.Mods).ToArray();

                    int id = Array.FindIndex(modArray, b => b.ID == roundBeatmap.ID) + 1;

                    var mapBoxContent = createMapBoxContent($"{roundBeatmap.Mods}{id}", backgroundColor, textColor);

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

            private static Drawable createHeaderSection(string text)
            {
                return new GridContainer
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 20f,
                    ColumnDimensions = new Dimension[]
                    {
                        new Dimension(GridSizeMode.Distributed),
                        new Dimension(GridSizeMode.AutoSize),
                        new Dimension(GridSizeMode.Distributed)
                    },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            // 左边的线条
                            new Box
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 1,
                                Colour = new Color4(166, 166, 166, 255),
                            },
                            // 文字
                            new Container
                            {
                                AutoSizeAxes = Axes.X,
                                RelativeSizeAxes = Axes.Y,
                                Anchor = Anchor.TopLeft,
                                Origin = Anchor.CentreLeft,
                                Margin = new MarginPadding { Horizontal = 10f },
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
                                Height = 1,
                                Colour = new Color4(166, 166, 166, 255),
                            }
                        }
                    }
                };
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

            public Container TopMapContainer { get; }

            public Container BottomMapContainer { get; }

            public Box CenterLine { get; }
        }
    }
}
