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

        private readonly Bindable<TournamentMatch?> currentMatch = new Bindable<TournamentMatch?>();

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
            currentMatch.BindTo(ladderInfo.CurrentMatch);
            currentMatch.BindValueChanged(matchChanged, true);
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

            if (currentMatch.Value?.Round.Value == null)
                return;

            var currentPicksBans = currentMatch.Value.PicksBans.ToList();

            foreach (var group in currentMatch.Value.Round.Value.BanPickFlowGroups)
            {
                var currentMapDetail = new MapDetailContent(group.Name.Value);
                List<BeatmapChoice?> currentChoices = new List<BeatmapChoice?>();

                for (int i = 0; i < group.TotalStep; i++)
                {
                    var find = currentPicksBans.FirstOrDefault(p => p.Type == group.Steps[i % group.Steps.Count].CurrentAction.Value);

                    if (find != null)
                    {
                        currentChoices.Add(find);
                        currentPicksBans.Remove(find);
                    }
                    else
                        break;
                }

                while (currentChoices.Count < group.TotalStep)
                {
                    currentChoices.Add(null);
                }

                currentMapDetail.UpdateBeatmap(currentChoices);
                mapContentContainer.Add(currentMapDetail);
                mapContentContainer.Add(createDivideLine());
            }

            var TBMap = currentMatch.Value.Round.Value?.Beatmaps.FirstOrDefault(map => map.Mods == "TB");

            if (TBMap != null)
            {
                mapContentContainer.Add(new TBMapBox());
            }

            int mapCount = currentMatch.Value.Round.Value!.Beatmaps.Count;
            int remainMapCount = mapCount - currentMatch.Value.PicksBans.Count(p => p.IsConsumed());

            mapCountText.Text = $"图池内谱面数量：{mapCount}  |  图池内剩余谱面：{remainMapCount}";

            Scheduler.Add(() =>
            {
                FinishTransforms();

                if (mapContentContainer.DrawWidth < 1000)
                {
                    mapContentContainer.Anchor = Anchor.TopCentre;
                    mapContentContainer.Origin = Anchor.TopCentre;
                    mapContentContainer.X = 0;
                    return;
                }

                mapContentContainer.Anchor = Anchor.TopLeft;
                mapContentContainer.Origin = Anchor.TopLeft;

                // 每秒走50px
                double timeToRepeat = (mapContentContainer.DrawWidth - 1000 + cover_width) / 30f * 1000;
                mapContentContainer.X = cover_width;
                mapContentContainer.MoveToX((1000 - cover_width - mapContentContainer.DrawWidth), timeToRepeat, Easing.OutSine).Then(5000).MoveToX(cover_width, timeToRepeat, Easing.OutSine).Then(5000)
                                   .Loop();
            });
        }

        private Box createDivideLine() => new Box
        {
            Colour = Color4Extensions.FromHex("#E5E5E5"),
            Height = 38.5f,
            Width = 2f,
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

            public void UpdateBeatmap(IEnumerable<BeatmapChoice?> maps) => Scheduler.AddOnce(() =>
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
            });

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
