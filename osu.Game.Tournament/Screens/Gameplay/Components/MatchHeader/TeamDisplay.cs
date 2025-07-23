// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Models;
using osuTK;

namespace osu.Game.Tournament.Screens.Gameplay.Components.MatchHeader
{
    public partial class TeamDisplay : DrawableTournamentTeam
    {
        private readonly TeamMultCoin score;

        private readonly Bindable<string> teamName = new Bindable<string>("???");

        private bool showScore;
        private readonly TeamCoinDIffDisplay coinDiff;

        [Resolved]
        private LadderInfo ladderInfo { get; set; } = null!;

        public bool ShowScore
        {
            get => showScore;
            set
            {
                showScore = value;

                if (IsLoaded)
                    updateDisplay();
            }
        }

        public TeamDisplay(TournamentTeam? team, TeamColour colour, Bindable<double?> currentTeamCoin, int pointsToWin)
            : base(team)
        {
            AutoSizeAxes = Axes.Both;

            Anchor = Anchor.CentreLeft;
            Origin = Anchor.CentreLeft;

            var anchor = colour == TeamColour.Red ? Anchor.CentreLeft : Anchor.CentreRight;

            Flag.RelativeSizeAxes = Axes.None;
            Flag.Scale = new Vector2(0.8f);
            Flag.Origin = anchor;
            Flag.Anchor = anchor;

            InternalChild = new Container
            {
                AutoSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(10, 0),
                        Children = new Drawable[]
                        {
                            new FillFlowContainer
                            {
                                Direction = FillDirection.Vertical,
                                Origin = anchor,
                                Anchor = anchor,
                                AutoSizeAxes = Axes.Both,
                                Spacing = new Vector2(0, 3),
                                Children = new Drawable[]
                                {
                                    Flag.With(f =>
                                    {
                                        f.Anchor = Anchor.TopCentre;
                                        f.Origin = Anchor.TopCentre;
                                    }),
                                    coinDiff = new TeamCoinDIffDisplay(colour)
                                }
                            },
                            new FillFlowContainer
                            {
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                                Origin = anchor,
                                Anchor = anchor,
                                Children = new Drawable[]
                                {
                                }
                            },
                        }
                    },
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            updateDisplay();

            FinishTransforms(true);

            if (Team != null)
                teamName.BindTo(Team.FullName);
        }

        private void updateDisplay()
        {
            //score.FadeTo(ShowScore ? 1 : 0, 200);
            coinDiff.FadeTo(ShowScore ? 1 : 0, 200);
        }
    }
}
