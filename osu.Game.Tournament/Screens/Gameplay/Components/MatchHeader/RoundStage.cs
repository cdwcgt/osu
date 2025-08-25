// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
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

        private bool isExtend;

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

            fadeTextColour(Color4.White);

            if (WarmUp.Value)
            {
                extend();
                fadeBackgroundColour(Color4Extensions.FromHex("#AC33C1"));
                Text.Text = "热 身 阶 段";
                return;
            }

            int beatOf = currentMatch.Value.Round.Value?.BestOf.Value ?? 0;

            bool isTiebreaker = banPicks.Count(p => p.Type == ChoiceType.Pick) == beatOf;

            if (!isTiebreaker)
            {
                retract();
                return;
            }

            extend();

            if (isTiebreaker)
            {
                fadeBackgroundColour(Color4Extensions.FromHex("#FFC300"));
                fadeTextColour(Color4.Black);
                Text.Text = "加 时 赛";
            }
        }

        private void fadeBackgroundColour(ColourInfo colour)
        {
            if (isExtend)
            {
                Background.FadeColour(colour, 200);
            }
            else
            {
                Background.FadeColour(colour);
            }
        }

        private void fadeTextColour(ColourInfo colour)
        {
            if (isExtend)
            {
                Text.FadeColour(colour, 200);
            }
            else
            {
                Text.FadeColour(colour);
            }
        }

        private void retract()
        {
            isExtend = false;
            this.MoveToY(0, 400, Easing.InElastic);
        }

        private void extend()
        {
            isExtend = true;
            this.MoveToY(30, 400, Easing.OutElastic);
        }
    }
}
