// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Models;
using osuTK;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class MatchHeader : Container
    {
        private TeamScoreDisplay teamDisplay1 = null!;
        private TeamScoreDisplay teamDisplay2 = null!;
        private DrawableTournamentHeaderLogo logo = null!;

        private readonly Bindable<TournamentMatch?> currentMatch = new Bindable<TournamentMatch?>();

        private bool showScores = true;

        public bool ShowScores
        {
            get => showScores;
            set
            {
                if (value == showScores)
                    return;

                showScores = value;

                if (IsLoaded)
                    updateDisplay();
            }
        }

        private bool showLogo = true;

        public bool ShowLogo
        {
            get => showLogo;
            set
            {
                if (value == showLogo)
                    return;

                showLogo = value;

                if (IsLoaded)
                    updateDisplay();
            }
        }

        [BackgroundDependencyLoader]
        private void load(LadderInfo info)
        {
            RelativeSizeAxes = Axes.X;
            Height = 95;
            Children = new Drawable[]
            {
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Padding = new MarginPadding(20),
                    Spacing = new Vector2(5),
                    Children = new Drawable[]
                    {
                        logo = new DrawableTournamentHeaderLogo
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Alpha = showLogo ? 1 : 0
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
                teamDisplay1 = new TeamScoreDisplay(TeamColour.Red)
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                },
                teamDisplay2 = new TeamScoreDisplay(TeamColour.Blue)
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                },
            };

            currentMatch.BindValueChanged(m =>
            {
                if (m.NewValue == null || m.NewValue.StructureType.Value != MatchStructureType.HeadToHead)
                {
                    Hide();
                    return;
                }

                Show();
            });

            currentMatch.BindTo(info.CurrentMatch);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            updateDisplay();
        }

        private void updateDisplay()
        {
            teamDisplay1.ShowScore = showScores;
            teamDisplay2.ShowScore = showScores;

            logo.Alpha = showLogo ? 1 : 0;
        }
    }
}
