// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
using osu.Game.Tournament.Models;
using osu.Game.Tournament.Screens.Gameplay.Components.RoundInformation;
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
        private static readonly Color4 boarder_color = Color4Extensions.FromHex("#808080");

        private const float cover_width = 50f;

        public RoundInformationPreview()
        {
            AutoSizeAxes = Axes.X;
            Height = 110;
            Masking = true;
            CornerRadius = 10;

            InternalChildren = new Drawable[]
            {
                new BackdropBlurContainer
                {
                    BorderColour = boarder_color,
                    BorderThickness = 2f,
                    RelativeSizeAxes = Axes.Both,
                    BlurSigma = new Vector2(10f),
                    CornerRadius = 10,
                    Masking = true,
                    Child = new Box
                    {
                        Name = "backgroud",
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black,
                        Alpha = 0.25f,
                    },
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
                                    Direction = FillDirection.Horizontal,
                                    Spacing = new Vector2(8f, 0)
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
                            Colour = Color4Extensions.FromHex("#E5E5E5"),
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

        protected override void Update()
        {
            base.Update();

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

            //var protectedDetail = new MapDetailContent("保图");
            var banMapDetail = new MapDetailContent("禁图");
            var pickDetail = new MapDetailContent("选图");
            var banMapDetail1 = new MapDetailContent("禁图1");
            var pickDetail1 = new MapDetailContent("选图1");

            //BeatmapChoice?[] protectedChoice = ladderInfo.CurrentMatch.Value.PicksBans.Where(b => b.Type == ChoiceType.Protected).ToArray();
            //protectedChoice = protectedChoice.Concat(Enumerable.Repeat<BeatmapChoice?>(null, Math.Max(2 - protectedChoice.Length, 0))).ToArray();

            BeatmapChoice?[] banChoices = ladderInfo.CurrentMatch.Value.PicksBans.Where(b => b.Type == ChoiceType.Ban).ToArray();

            BeatmapChoice?[] pickChoices = ladderInfo.CurrentMatch.Value.PicksBans.Where(b => b.Type == ChoiceType.Pick).ToArray();

            banChoices = banChoices.Concat(Enumerable.Repeat((BeatmapChoice?)null, (ladderInfo.CurrentMatch.Value.Round.Value?.BanCount.Value ?? 2) * 2 - banChoices.Length)).ToArray();

            int pickMapCount = ladderInfo.CurrentMatch.Value.Round.Value.BestOf.Value - 1 // 去掉TB

            var pickChoice = pickChoices.Take(pickMapCount)
                                        // 往后面填充null
                                        .Concat(Enumerable.Repeat((BeatmapChoice?)null, pickMapCount - pickChoices.Take(pickMapCount).Count()));

            //mapContentContainer.Add(protectedDetail);
            //mapContentContainer.Add(createDivideLine());
            mapContentContainer.Add(banMapDetail);
            mapContentContainer.Add(createDivideLine());
            mapContentContainer.Add(pickDetail);
            mapContentContainer.Add(createDivideLine());
            mapContentContainer.Add(banMapDetail1);
            mapContentContainer.Add(createDivideLine());
            mapContentContainer.Add(pickDetail1);
            mapContentContainer.Add(createDivideLine());

            var TBMap = ladderInfo.CurrentMatch.Value?.Round.Value?.Beatmaps.FirstOrDefault(map => map.Mods == "TB");

            if (TBMap != null)
            {
                mapContentContainer.Add(new TBMapBox());
            }

            int mapCount = ladderInfo.CurrentMatch.Value.Round.Value.Beatmaps.Count;
            int remainMapCount = mapCount - ladderInfo.CurrentMatch.Value.PicksBans.Count;

            mapCountText.Text = $"图池内谱面数量：{mapCount}  |  图池内剩余谱面：{remainMapCount}";

            Scheduler.Add(() =>
            {
                //protectedDetail.UpdateBeatmap(protectedChoice);
                banMapDetail.UpdateBeatmap(banChoices.Take(2));
                pickDetail.UpdateBeatmap(pickChoice.Take(2));
                banMapDetail1.UpdateBeatmap(banChoices.Skip(2));
                pickDetail1.UpdateBeatmap(pickChoice.Skip(2));

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
            Colour = Color4Extensions.FromHex("#E5E5E5"),
            Height = 38.5f,
            Width = 1f,
            Margin = new MarginPadding
            {
                Top = 37f
            }
        };

        private partial class MapDetailContent : CompositeDrawable
        {
            private FillFlowContainer mapContent = null!;

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

                InternalChild = new FillFlowContainer
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        //createHeaderSection(headerName),
                        mapContent = new FillFlowContainer
                        {
                            Spacing = new Vector2(10),
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

                mapContent.Clear();

                mapContent.ChildrenEnumerable = maps.Select(map =>
                {
                    MapBox mapBox;

                    switch (map?.Type)
                    {
                        case ChoiceType.Protected:
                            mapBox = new ProtectMapBox(map);
                            break;

                        case ChoiceType.Ban:
                            mapBox = new BanMapBox(map);
                            break;

                        case ChoiceType.Pick:
                            mapBox = new PickMapBox(map);
                            break;

                        default:
                        case null:
                            mapBox = new UnselectMapBox();
                            break;
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
    }
}
