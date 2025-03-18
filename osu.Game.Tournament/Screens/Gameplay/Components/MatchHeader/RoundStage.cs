// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Models;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Gameplay.Components.MatchHeader
{
    public partial class RoundStage : TournamentSpriteTextWithBackground
    {
        [Resolved]
        private LadderInfo ladder { get; set; } = null!;

        private readonly Bindable<TournamentMatch?> currentMatch = new Bindable<TournamentMatch?>();
        private readonly Bindable<int?> team1Score = new Bindable<int?>();
        private readonly Bindable<int?> team2Score = new Bindable<int?>();

        public BindableBool WarmUp { get; } = new BindableBool();

        public RoundStage()
        {
            AutoSizeAxes = Axes.None;
            Height = 16;
            Width = 120;

            Text.Alpha = 0;
            Background.Alpha = 0;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            currentMatch.BindTo(ladder.CurrentMatch);
            currentMatch.BindValueChanged(matchChanged);
            WarmUp.BindValueChanged(_ => updateDisplay());

            updateMatch();
        }

        private void matchChanged(ValueChangedEvent<TournamentMatch?> match)
        {
            team1Score.UnbindBindings();
            team2Score.UnbindBindings();

            Scheduler.AddOnce(updateMatch);
        }

        private void updateMatch()
        {
            var match = currentMatch.Value;

            if (match == null) return;

            match.StartMatch();

            team1Score.BindTo(match.Team1Score);
            team2Score.BindTo(match.Team2Score);

            updateDisplay();
        }

        private void updateDisplay()
        {
            if (currentMatch.Value == null)
            {
                this.FadeOut(200);
                return;
            }

            if (WarmUp.Value)
            {
                this.FadeIn(200);
                Background.FadeColour(Color4Extensions.FromHex("#FFC300"), 200);
                Text.FadeColour(Color4.Black, 200);
                Text.Text = "热 身 阶 段";
                return;
            }

            int pointToWin = currentMatch.Value.PointsToWin;
            int score1 = team1Score.Value ?? 0;
            int score2 = team2Score.Value ?? 0;

            bool matchPoint = Math.Max(score1, score2) >= pointToWin - 1;

            if (!matchPoint)
            {
                this.FadeOut(200);
                return;
            }

            this.FadeIn(200);
            Text.FadeColour(Color4.White, 200);

            if (score1 + score2 >= pointToWin * 2 - 1)
            {
                Background.FadeColour(Color4Extensions.FromHex("#E33C64"), 200);
                Text.Text = "决 胜 局";
            }
            else
            {
                Background.FadeColour(Color4Extensions.FromHex("#2A82E4"), 200);
                Text.Text = "赛 点";
            }
        }
    }
}
