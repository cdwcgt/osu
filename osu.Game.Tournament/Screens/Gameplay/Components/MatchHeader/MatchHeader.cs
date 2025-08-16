// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Screens.Menu;
using osu.Game.Tournament.Models;

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

            MatchRoundDisplay matchRoundDisplay = new MatchRoundDisplay();

            Children = new Drawable[]
            {
                roundStage = new RoundStage
                {
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.TopCentre,
                },
                new FlowContainerWithOrigin
                {
                    Anchor = Anchor.Centre,
                    RelativeSizeAxes = Axes.Y,
                    AutoSizeAxes = Axes.X,
                    Direction = FillDirection.Horizontal,
                    CentreTarget = matchRoundDisplay,
                    Children = new Drawable[]
                    {
                        teamDisplay1 = new TeamScoreDisplay(TeamColour.Red),
                        matchRoundDisplay,
                        teamDisplay2 = new TeamScoreDisplay(TeamColour.Blue)
                    }
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            updateDisplay();
        }

        private void updateDisplay()
        {
            roundStage.WarmUp.Value = !showScores;
        }
    }
}
