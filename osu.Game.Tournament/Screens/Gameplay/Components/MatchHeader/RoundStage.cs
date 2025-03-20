// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Game.Graphics;
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

        private readonly BindableList<BeatmapChoice> banPicks = new BindableList<BeatmapChoice>();

        public BindableBool WarmUp { get; } = new BindableBool();

        public RoundStage()
        {
            AutoSizeAxes = Axes.None;
            Height = 16;
            Width = 120;

            Text.Anchor = Anchor.Centre;
            Text.Origin = Anchor.Centre;
            Text.Font = OsuFont.Torus.With(size: 16);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            currentMatch.BindTo(ladder.CurrentMatch);
            currentMatch.BindValueChanged(matchChanged);
            WarmUp.BindValueChanged(_ => updateDisplay());
            banPicks.BindCollectionChanged((_, _) => updateDisplay());

            updateMatch();
        }

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

        private void updateDisplay()
        {
            if (currentMatch.Value == null)
            {
                retract();
                return;
            }

            if (WarmUp.Value)
            {
                extend();
                Background.FadeColour(Color4Extensions.FromHex("#FFC300"));
                Text.FadeColour(Color4.Black);
                Text.Text = "热 身 阶 段";
                return;
            }

            int pointToWin = currentMatch.Value.PointsToWin;
            int score1 = team1Score.Value ?? 0;
            int score2 = team2Score.Value ?? 0;

            bool matchPoint = Math.Max(score1, score2) >= pointToWin - 1;

            if (!matchPoint)
            {
                retract();
                return;
            }

            extend();
            Text.FadeColour(Color4.White);

            bool isTiebreaker = score1 + score2 >= pointToWin * 2 - 2 && Math.Max(score1, score2) != pointToWin;

            if (isTiebreaker)
            {
                Background.FadeColour(Color4Extensions.FromHex("#E33C64"));
                Text.Text = "决 胜 局";
            }
            else
            {
                Background.FadeColour(Color4Extensions.FromHex("#2A82E4"));
                Text.Text = "赛 点";
            }
        }

        private void retract() => this.MoveToY(-16, 400, Easing.InElastic);
        private void extend() => this.MoveToY(5, 400, Easing.OutElastic);
    }
}
