// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
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
    public partial class FourTeamMatchHeader : CompositeDrawable
    {
        private readonly BindableList<TournamentMatchSlot> slot = new BindableList<TournamentMatchSlot>();
        private readonly Bindable<TournamentMatch?> currentMatch = new Bindable<TournamentMatch?>();

        public FourTeamMatchHeader(BindableList<TournamentMatchSlot> slot) // test purpose
        {
            this.slot.BindTo(slot);

            RelativeSizeAxes = Axes.X;
            Height = 95;
        }

        [BackgroundDependencyLoader]
        private void load(LadderInfo info)
        {
            currentMatch.BindValueChanged(m =>
            {
                if (m.NewValue == null || m.NewValue.StructureType.Value != MatchStructureType.FourTeams)
                    return;

                slot.BindTo(m.NewValue.TeamSlots);
            });

            slot.BindCollectionChanged((_, _) =>
            {
                updateDisplay();
            }, true);

            currentMatch.BindTo(info.CurrentMatch);
        }

        private void updateDisplay()
        {
            InternalChild = new FillFlowContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(10),
                Children = new Drawable[]
                {
                    new TeamDisplay(slot.FirstOrDefault(s => s.Colour.Value == TeamColour.Red)),
                    new TeamDisplay(slot.FirstOrDefault(s => s.Colour.Value == TeamColour.Blue)),
                    new FillFlowContainer
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding(10),
                        Spacing = new Vector2(5),
                        Children = new Drawable[]
                        {
                            new DrawableTournamentHeaderLogo
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                            },
                            new DrawableTournamentHeaderText
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                            },
                            new MatchRoundDisplay
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Scale = new Vector2(0.4f)
                            },
                        }
                    },
                    new TeamDisplay(slot.FirstOrDefault(s => s.Colour.Value == TeamColour.Yellow)),
                    new TeamDisplay(slot.FirstOrDefault(s => s.Colour.Value == TeamColour.Green)),
                }
            };
        }

        private partial class TeamDisplay : DrawableTournamentTeam
        {
            private readonly TournamentMatchSlot? slot;
            private readonly Bindable<int?> currentTeamScore = new Bindable<int?>();

            private TournamentSpriteText scoreText = null!;

            public TeamDisplay(TournamentMatchSlot? slot)
                : base(slot?.Team.Value)
            {
                Height = 95;
                Width = 300;
                this.slot = slot;
                Anchor = Anchor.TopCentre;
                Origin = Anchor.TopCentre;

                Masking = true;
                CornerRadius = 5;
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour color)
            {
                if (slot == null || Team == null)
                    return;

                ColourInfo backgroundColor = Color4.Black;

                switch (slot!.Colour.Value)
                {
                    case TeamColour.Red:
                        backgroundColor = color.Red3;
                        break;

                    case TeamColour.Blue:
                        backgroundColor = color.BlueDark;
                        break;

                    case TeamColour.Yellow:
                        backgroundColor = color.YellowDark;
                        break;

                    case TeamColour.Green:
                        backgroundColor = color.Green3;
                        break;
                }

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = backgroundColor
                    },
                    new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        ColumnDimensions = new[]
                        {
                            new Dimension(GridSizeMode.AutoSize),
                            new Dimension()
                        },
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                Flag.With(f =>
                                {
                                    f.Margin = new MarginPadding(10);
                                    f.Anchor = Anchor.CentreLeft;
                                    f.Origin = Anchor.CentreLeft;
                                }),
                                new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Anchor = Anchor.CentreRight,
                                    Origin = Anchor.CentreRight,
                                    Direction = FillDirection.Vertical,
                                    Spacing = new Vector2(10),
                                    Children = new Drawable[]
                                    {
                                        new TournamentSpriteTextWithBackground(Team?.FullName.Value ?? "???")
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Text =
                                            {
                                                Anchor = Anchor.Centre,
                                                Origin = Anchor.Centre,
                                                Font = OsuFont.Torus.With(weight: FontWeight.SemiBold, size: 20),
                                                Padding = new MarginPadding { Left = 10, Right = 10 },
                                            },
                                        },
                                        scoreText = new TournamentSpriteText
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Text = "10"
                                        }
                                    }
                                }
                            }
                        }
                    }
                };

                currentTeamScore.BindValueChanged(s =>
                {
                    scoreText.Text = s.NewValue.ToString() ?? string.Empty;
                });
                slot.Score.BindTo(currentTeamScore);
            }
        }
    }
}
