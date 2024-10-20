// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Models;
using osuTK;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class TeamCoinDIffDisplay : CompositeDrawable
    {
        private readonly TeamColour teamColour;

        private readonly Bindable<double?> currentTeamCoin = new Bindable<double?>();
        private readonly Bindable<double?> opponentTeamCoin = new Bindable<double?>();
        private readonly RollingSignNumberContainer coinDiffContainer;

        public TeamCoinDIffDisplay(TeamColour colour)
        {
            teamColour = colour;

            AutoSizeAxes = Axes.Both;

            InternalChild = new Container
            {
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                CornerRadius = 10f,
                Size = new Vector2(60, 20),
                Masking = true,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = TournamentGame.GetTeamColour(colour)
                    },
                    coinDiffContainer = new RollingMultDiffNumberContainer
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre
                    }
                }
            };
        }

        private partial class RollingMultDiffNumberContainer : RollingSignNumberContainer
        {
            protected override double RollingDuration => 1000;
        }

        [BackgroundDependencyLoader]
        private void load(LadderInfo ladder)
        {
            var currentMatch = ladder.CurrentMatch.Value;

            if (currentMatch == null)
                return;

            currentTeamCoin.BindTo(teamColour == TeamColour.Red ? currentMatch.Team1Coin : currentMatch.Team2Coin);
            opponentTeamCoin.BindTo(teamColour == TeamColour.Blue ? currentMatch.Team1Coin : currentMatch.Team2Coin);

            currentTeamCoin.BindValueChanged(_ => updateDisplay(), true);
            opponentTeamCoin.BindValueChanged(_ => updateDisplay(), true);
        }

        private void updateDisplay() => Scheduler.AddOnce(() =>
        {
            FinishTransforms(true);
            using (BeginDelayedSequence(2000))
                coinDiffContainer.Current.Value = (currentTeamCoin.Value ?? 0) - (opponentTeamCoin.Value ?? 0);
        });
    }
}
