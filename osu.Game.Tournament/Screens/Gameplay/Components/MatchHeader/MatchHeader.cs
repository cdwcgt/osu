// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Tournament.Models;
using osuTK;

namespace osu.Game.Tournament.Screens.Gameplay.Components.MatchHeader
{
    public partial class MatchHeader : Container
    {
        private TeamScoreDisplay teamDisplay1 = null!;
        private TeamScoreDisplay teamDisplay2 = null!;
        private RoundStage roundStage = null!;

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

        private bool showPoint = true;

        public bool ShowRound
        {
            get => showPoint;
            set
            {
                if (value == showPoint)
                    return;

                showPoint = value;

                if (IsLoaded)
                    updateDisplay();
            }
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.X;
            Height = 24;
            Margin = new MarginPadding { Top = 50 };

            Children = new Drawable[]
            {
                new FillFlowContainer
                {
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Children = new Drawable[]
                    {
                        teamDisplay1 = new TeamScoreDisplay(TeamColour.Red)
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                        },
                        new MatchRoundDisplay
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                        },
                        teamDisplay2 = new TeamScoreDisplay(TeamColour.Blue)
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                        },
                    }
                },
                roundStage = new RoundStage()
            };
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
            roundStage.WarmUp.Value = !showScores;
        }
    }
}
