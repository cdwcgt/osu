// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
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

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.X;
            Height = 24;
            Margin = new MarginPadding { Top = 50 };

            MatchRoundDisplay matchRoundDisplay = new MatchRoundDisplay();

            Children = new Drawable[]
            {
                new TeamCoinDIffDisplay
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.BottomCentre,
                    Margin = new MarginPadding { Bottom = 5f }
                },
                roundStage = new RoundStage
                {
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
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

            currentMatch.BindTo(ladder.CurrentMatch);
            currentMatch.BindValueChanged(matchChanged);
            WarmUp.BindValueChanged(_ => updateDisplay());
            banPicks.BindCollectionChanged((_, _) => updateDisplay());

            updateMatch();
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

        [Resolved]
        private LadderInfo ladder { get; set; } = null!;

        private readonly Bindable<TournamentMatch?> currentMatch = new Bindable<TournamentMatch?>();
        private readonly Bindable<int?> team1Score = new Bindable<int?>();
        private readonly Bindable<int?> team2Score = new Bindable<int?>();

        private readonly BindableList<BeatmapChoice> banPicks = new BindableList<BeatmapChoice>();
        public BindableBool WarmUp { get; } = new BindableBool();

        private void matchChanged(ValueChangedEvent<TournamentMatch?> match)
        {
            team1Score.UnbindBindings();
            team2Score.UnbindBindings();
            banPicks.UnbindBindings();
            WarmUp.UnbindBindings();

            Scheduler.AddOnce(updateMatch);
        }

        private void updateMatch()
        {
            var match = currentMatch.Value;

            if (match == null) return;

            match.StartMatch();

            team1Score.BindTo(match.Team1Score);
            team2Score.BindTo(match.Team2Score);
            banPicks.BindTo(match.PicksBans);

            updateDisplay();
        }
    }
}
