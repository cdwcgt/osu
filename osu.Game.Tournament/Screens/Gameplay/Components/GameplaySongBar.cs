// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Extensions;
using osu.Game.Graphics.UserInterface;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Models;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class GameplaySongBar : SongBar
    {
        private FillFlowContainer leftData = null!;
        private Container beatmapPanel = null!;
        private FillFlowContainer rightData = null!;
        private readonly Bindable<TournamentMatch?> currentMatch = new Bindable<TournamentMatch?>();
        public new const float HEIGHT = 50;

        private readonly Bindable<ColourInfo> arrowColor = new Bindable<ColourInfo>(Color4.White);
        private SpriteIcon leftArrow = null!;
        private SpriteIcon rightArrow = null!;
        private bool expanded;
        private Box background = null!;
        private LoadingSpinner loading = null!;

        public readonly BindableBool WaitForResult = new BindableBool();

        [Resolved]
        private LadderInfo ladder { get; set; } = null!;

        public override bool Expanded
        {
            get => expanded;
            set
            {
                expanded = value;
                updatePosition();
            }
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            currentMatch.BindValueChanged(matchChanged);
            currentMatch.BindTo(ladder.CurrentMatch);

            Anchor = Anchor.BottomCentre;
            Origin = Anchor.BottomCentre;

            RelativeSizeAxes = Axes.None;
            AutoSizeAxes = Axes.X;
            Height = HEIGHT + 7f;

            Padding = new MarginPadding { Bottom = 7f };

            InternalChildren = new Drawable[]
            {
                new FillFlowContainer
                {
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    RelativeSizeAxes = Axes.Y,
                    AutoSizeAxes = Axes.X,
                    Direction = FillDirection.Horizontal,
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            Name = "Left arrow",
                            AutoSizeAxes = Axes.Both,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Child = leftArrow = new SpriteIcon
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                Size = new Vector2(30),
                                Icon = FontAwesome.Solid.ChevronRight,
                                Shadow = true
                            },
                        },
                        new FillFlowContainer
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            AutoSizeAxes = Axes.X,
                            RelativeSizeAxes = Axes.Y,
                            Direction = FillDirection.Horizontal,
                            Masking = true,
                            EdgeEffect = new EdgeEffectParameters
                            {
                                Colour = new Color4(0f, 0f, 0f, 0.25f),
                                Type = EdgeEffectType.Shadow,
                                Radius = 8,
                                Offset = new Vector2(1, 1),
                                Hollow = true
                            },
                            Children = new Drawable[]
                            {
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Y,
                                    Width = 240,
                                    Name = "Left data",
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = Colour4.Black,
                                            Alpha = 0.55f,
                                        },
                                        leftData = new FillFlowContainer
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Direction = FillDirection.Vertical,
                                        }
                                    },
                                },
                                beatmapPanel = new Container
                                {
                                    RelativeSizeAxes = Axes.Y,
                                    AutoSizeAxes = Axes.X,
                                    Child = new TournamentBeatmapPanel(beatmap)
                                    {
                                        Width = 500,
                                        CenterText = true
                                    },
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Y,
                                    Width = 240,
                                    Name = "Right data",
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = Colour4.Black,
                                            Alpha = 0.55f,
                                        },
                                        rightData = new FillFlowContainer
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Direction = FillDirection.Vertical,
                                        }
                                    },
                                },
                            }
                        },
                        new Container
                        {
                            Name = "Right arrow",
                            AutoSizeAxes = Axes.Both,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Child = rightArrow = new SpriteIcon
                            {
                                Size = new Vector2(30),
                                Icon = FontAwesome.Solid.ChevronLeft,
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Shadow = true
                            },
                        }
                    }
                },
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0f,
                    Colour = Color4.Black
                },
                loading = new LoadingSpinner
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                },
            };

            arrowColor.BindValueChanged(c =>
            {
                leftArrow.FadeColour(c.NewValue, 300);
                rightArrow.FadeColour(c.NewValue, 300);
            });

            WaitForResult.BindValueChanged(s =>
            {
                if (s.NewValue)
                {
                    background.FadeIn(300);
                    loading.Show();
                }
                else
                {
                    background.FadeOut(300);
                    loading.Hide();
                }
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            leftArrow.MoveToX(-25, 1500, Easing.Out).Then().MoveToX(0, 1500, Easing.In).Loop();
            rightArrow.MoveToX(25, 1500, Easing.Out).Then().MoveToX(0, 1500, Easing.In).Loop();
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
            BeatmapChoice? isChoice = currentMatch.Value?.PicksBans.FirstOrDefault(p => p.BeatmapID == beatmap?.OnlineID && p.Type == ChoiceType.Pick);

            if (isChoice == null)
            {
                arrowColor.Value = Color4.White;
                return;
            }

            arrowColor.Value = getTeamColour(isChoice.Team);

            static ColourInfo getTeamColour(TeamColour teamColour) => teamColour == TeamColour.Red ? Color4Extensions.FromHex("#D43030") : Color4Extensions.FromHex("#2A82E4");
        }

        private void updatePosition()
        {
            this.MoveTo(expanded ? new Vector2(0, -25) : Vector2.Zero, 300, Easing.Out);
        }

        private string? getBeatmapModPosition()
        {
            var roundBeatmap = ladder.CurrentMatch.Value?.Round.Value?.Beatmaps.FirstOrDefault(roundMap => roundMap.ID == beatmap!.OnlineID);

            if (roundBeatmap == null)
                return null;

            var modArray = ladder.CurrentMatch.Value!.Round.Value.Beatmaps.Where(b => b.Mods == roundBeatmap.Mods).ToArray();

            int id = Array.FindIndex(modArray, b => b.ID == roundBeatmap?.ID) + 1;

            return $"{roundBeatmap.Mods}{id}";
        }

        protected override void PostUpdate()
        {
            updateState();

            GetBeatmapInformation(out double bpm, out double length, out string srExtra, out var stats);

            (string, string)[] srAndModStats =
            {
                ("星级", $"{beatmap!.StarRating:0.00}{srExtra}")
            };

            string? modPosition = getBeatmapModPosition();

            if (modPosition != null)
            {
                srAndModStats = srAndModStats.Append(("谱面位置", modPosition)).ToArray();
            }

            leftData.Children = new Drawable[]
            {
                new DiffPiece(("BPM", $"{bpm:0.#}"))
                {
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                },
                new DiffPiece(("谱面长度", length.ToFormattedDuration().ToString()))
                {
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                },
            };

            rightData.Children = new Drawable[]
            {
                new DiffPiece(stats)
                {
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                },
                new DiffPiece(srAndModStats)
                {
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                }
            };

            beatmapPanel.Child = new TournamentBeatmapPanel(beatmap)
            {
                Width = 500,
                CenterText = true,
            };
        }
    }
}
