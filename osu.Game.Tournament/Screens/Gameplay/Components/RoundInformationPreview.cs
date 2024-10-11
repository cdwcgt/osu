// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
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

            var firstHalfPickDetail = new MapDetailContent("上半场");
            var secondHalfPickDetail = new MapDetailContent("下半场");

            int halfMapCount = (ladderInfo.CurrentMatch.Value.Round.Value?.BestOf.Value - 1) / 2 ?? 99;

            var firstHalfPickChoice = remainChoices.Take(halfMapCount)
                                                   // 往后面填充null
                                                   .Concat(Enumerable.Repeat((BeatmapChoice?)null, halfMapCount - remainChoices.Take(halfMapCount).Count()));

            var secondHalfPickChoice = remainChoices.Skip(halfMapCount).Take(halfMapCount)
                                                    .Concat(Enumerable.Repeat((BeatmapChoice?)null, halfMapCount - remainChoices.Skip(halfMapCount).Take(halfMapCount).Count()));;

            mapContentContainer.Add(banMapDetail);
            mapContentContainer.Add(createDivideLine());
            mapContentContainer.Add(firstHalfPickDetail);
            mapContentContainer.Add(createDivideLine());
            mapContentContainer.Add(secondHalfPickDetail);
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
                firstHalfPickDetail.UpdateBeatmap(firstHalfPickChoice);
                secondHalfPickDetail.UpdateBeatmap(secondHalfPickChoice);
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
                Colour = Color4.Black
            });

            var TBMap = ladderInfo.CurrentMatch.Value?.Round.Value?.Beatmaps.FirstOrDefault(map => map.Mods == "TB");

            if (TBMap == null)
                return mapbox;

            mapbox.BottomMapContainer.Add(createMapBoxContent("TB", TBMap.BackgroundColor, TBMap.TextColor));

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
                if (ladderInfo.CurrentMatch.Value?.Round.Value == null)
                    return;

                banMapContent.Clear();

                banMapContent.ChildrenEnumerable = maps.Select(map =>
                {
                    var mapBox = new MapBox();

                    if (map == null)
                        return mapBox;

                    var roundBeatmap = ladderInfo.CurrentMatch.Value.Round.Value.Beatmaps.FirstOrDefault(roundMap => roundMap.ID == map.BeatmapID);
                    if (roundBeatmap == null)
                        return mapBox;

                    mapBox.CenterLine.Colour = map.Team == TeamColour.Red
                        ? new Color4(212, 48, 48, 255)
                        : new Color4(42, 130, 228, 255);

                    Color4 backgroundColor = map.Type == ChoiceType.Ban ? Color4.Gray : roundBeatmap.BackgroundColor;
                    Color4 textColor = map.Type == ChoiceType.Ban ? new Color4(229, 229, 229, 255) : roundBeatmap.TextColor;

                    var modArray = ladderInfo.CurrentMatch.Value.Round.Value.Beatmaps.Where(b => b.Mods == roundBeatmap.Mods).ToArray();

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
