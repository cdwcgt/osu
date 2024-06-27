// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Extensions;
using osu.Game.Rulesets;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Models;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Gameplay
{
    public partial class GameplaySongBar : SongBar
    {
        private FillFlowContainer leftData = null!;
        private Container beatmapPanel = null!;
        private FillFlowContainer rightData = null!;
        private readonly Bindable<TournamentMatch?> currentMatch = new Bindable<TournamentMatch?>();
        public new const float HEIGHT = 50;

        private Bindable<ColourInfo> ArrowColor = new Bindable<ColourInfo>(Color4.White);

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load(LadderInfo ladder)
        {
            currentMatch.BindValueChanged(matchChanged);
            currentMatch.BindTo(ladder.CurrentMatch);

            Anchor = Anchor.BottomCentre;
            Origin = Anchor.BottomCentre;

            RelativeSizeAxes = Axes.None;
            AutoSizeAxes = Axes.X;
            Height = HEIGHT;

            SpriteIcon leftArrow;
            SpriteIcon rightArrow;

            InternalChild = new FillFlowContainer
            {
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
                AutoSizeAxes = Axes.X,
                RelativeSizeAxes = Axes.Y,
                Direction = FillDirection.Horizontal,
                Children = new Drawable[]
                {
                    leftArrow = new SpriteIcon
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Size = new Vector2(30),
                        Icon = FontAwesome.Solid.ChevronRight,
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.Y,
                        Width = 210,
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
                            IsTextCenter = true
                        },
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.Y,
                        Width = 210,
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
                    rightArrow = new SpriteIcon
                    {
                        Size = new Vector2(30),
                        Icon = FontAwesome.Solid.ChevronLeft,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                    },
                }
            };

            ArrowColor.BindValueChanged(c =>
            {
                leftArrow.FadeColour(c.NewValue, 300);
                rightArrow.FadeColour(c.NewValue, 300);
            });
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
                ArrowColor.Value = Color4.White;
                return;
            }

            ArrowColor.Value = TournamentGame.GetTeamColour(isChoice.Team);
        }

        protected override void PostUpdate()
        {
            updateState();

            GetBeatmapInformation(out double bpm, out double length, out string srExtra, out var stats);

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
                new DiffPiece(("星级", $"{beatmap.StarRating:0.00}{srExtra}"))
                {
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                }
            };

            beatmapPanel.Child = new TournamentBeatmapPanel(beatmap)
            {
                Width = 500,
                IsTextCenter = true,
            };
        }
    }
}
