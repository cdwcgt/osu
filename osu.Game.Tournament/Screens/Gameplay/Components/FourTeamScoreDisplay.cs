// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Tournament.IPC;
using osu.Game.Tournament.Models;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class FourTeamScoreDisplay : CompositeDrawable
    {
        private readonly FillFlowContainer<TeamScoreDisplay> flow;

        public FourTeamScoreDisplay()
        {
            InternalChildren = new Drawable[]
            {
                new Box
                {
                    Colour = Color4.Black,
                    Alpha = 0.6f,
                    RelativeSizeAxes = Axes.Both
                },
                flow = new FillFlowContainer<TeamScoreDisplay>
                {
                    LayoutDuration = 450,
                    LayoutEasing = Easing.OutQuint,
                    RelativeSizeAxes = Axes.Both,
                    Children = new[]
                    {
                        new TeamScoreDisplay(TeamColour.Red),
                        new TeamScoreDisplay(TeamColour.Blue),
                        new TeamScoreDisplay(TeamColour.Yellow),
                        new TeamScoreDisplay(TeamColour.Green)
                    }
                }
            };

            foreach (var score in flow.ToArray())
            {
                score.Score.BindValueChanged(_ => sort());
            }

            sort();
        }

        private void sort()
        {
            foreach (var score in flow.ToArray())
                flow.SetLayoutPosition(score, -score.Score.Value);
        }

        private partial class TeamScoreDisplay : CompositeDrawable
        {
            private readonly TeamColour colour;

            public IBindable<long> Score => teamScore;
            private readonly Bindable<long> teamScore = new Bindable<long>();
            private readonly Bindable<TournamentMatch?> currentMatch = new Bindable<TournamentMatch?>();

            private Container scoreBox = null!;
            private ScoreCounter scoreCounter = null!;
            private TournamentSpriteText teamNameText = null!;

            public TeamScoreDisplay(TeamColour colour)
            {
                RelativeSizeAxes = Axes.X;
                Height = 36;
                this.colour = colour;
            }

            [BackgroundDependencyLoader]
            private void load(MatchIPCInfo ipc, LadderInfo info)
            {
                switch (colour)
                {
                    case TeamColour.Red:
                        teamScore.BindTo(ipc.Score1);
                        break;

                    case TeamColour.Blue:
                        teamScore.BindTo(ipc.Score2);
                        break;

                    case TeamColour.Yellow:
                        teamScore.BindTo(ipc.Score3);
                        break;

                    case TeamColour.Green:
                        teamScore.BindTo(ipc.Score4);
                        break;
                }

                InternalChildren = new Drawable[]
                {
                    new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        ColumnDimensions = new[]
                        {
                            new Dimension(GridSizeMode.Absolute, 100),
                            new Dimension(GridSizeMode.Distributed),
                        },
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                new Container
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Child = teamNameText = new TournamentSpriteText
                                    {
                                        Text = $"Team {colour}"
                                    }
                                },
                                new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Direction = FillDirection.Horizontal,
                                    Padding = new MarginPadding { Vertical = 5f },
                                    Children = new Drawable[]
                                    {
                                        scoreBox = new Container
                                        {
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                            RelativeSizeAxes = Axes.Both,
                                            Padding = new MarginPadding { Vertical = 5f },
                                            Width = 0,
                                            Colour = TournamentGame.GetTeamColour(colour),
                                            Child = new Container
                                            {
                                                Masking = true,
                                                CornerRadius = 5,
                                                RelativeSizeAxes = Axes.Both,
                                                Child = new Box
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                }
                                            }
                                        },
                                        scoreCounter = new ScoreCounter
                                        {
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                        }
                                    }
                                }
                            }
                        }
                    }
                };

                currentMatch.BindValueChanged(_ => updateDisplay());
                currentMatch.BindTo(info.CurrentMatch);

                teamScore.BindValueChanged(s => updateScore());
            }

            private void updateDisplay()
            {
                if (currentMatch.Value == null || currentMatch.Value.StructureType.Value != MatchStructureType.FourTeams)
                    return;

                var team = currentMatch.Value.TeamSlots.SingleOrDefault(t => t.Colour.Value == colour);
                teamNameText.Text = team?.Team.Value?.FlagName.Value ?? $"Team {colour}";
            }

            private void updateScore()
            {
                scoreBox.ResizeWidthTo(((float)Score.Value / 1_000_000) * 0.65f, 50f);
                scoreCounter.Current.Value = Score.Value;
            }

            private partial class ScoreCounter : CommaSeparatedScoreCounter
            {
                protected override OsuSpriteText CreateSpriteText() => base.CreateSpriteText().With(s =>
                {
                    s.Font = OsuFont.Torus.With(weight: FontWeight.Regular, size: 25f, fixedWidth: true);
                });
            }
        }
    }
}
