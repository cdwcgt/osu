// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Tournament.Models;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Gameplay.Components.MatchHeader
{
    public partial class MatchRoundDisplay : Container
    {
        private readonly Bindable<TournamentMatch?> currentMatch = new Bindable<TournamentMatch?>();
        private readonly Bindable<TournamentRound?> currentRound = new Bindable<TournamentRound?>();
        private readonly Bindable<string> currentRoundName = new Bindable<string>();
        private TournamentSpriteText roundName = null!;
        private TournamentSpriteText roundInfo = null!;

        private readonly BindableList<BeatmapChoice> banPicks = new BindableList<BeatmapChoice>();

        [BackgroundDependencyLoader]
        private void load(LadderInfo ladder)
        {
            RelativeSizeAxes = Axes.Y;
            AutoSizeAxes = Axes.X;

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.White,
                },
                roundName = new TournamentSpriteText
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Font = OsuFont.Torus.With(size: 13),
                    Colour = Color4.Black,
                },
                roundInfo = new TournamentSpriteText
                {
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Font = OsuFont.Torus.With(size: 11),
                    Colour = Color4Extensions.FromHex("#808080"),
                },
            };

            roundName.Margin = new MarginPadding { Horizontal = 10f };

            currentMatch.BindValueChanged(matchChanged);
            currentMatch.BindTo(ladder.CurrentMatch);

            currentRoundName.BindValueChanged(_ => Scheduler.AddOnce(updateDisplay));
            banPicks.BindCollectionChanged((_, _) => Scheduler.AddOnce(updateDisplay));
        }

        private void matchChanged(ValueChangedEvent<TournamentMatch?> match)
        {
            banPicks.UnbindBindings();
            currentRoundName.UnbindBindings();

            Scheduler.AddOnce(updateMatch);
        }

        private void updateMatch()
        {
            var match = currentMatch.Value;

            if (match == null) return;

            match.StartMatch();

            currentRound.Value = match.Round.Value;

            banPicks.BindTo(match.PicksBans);
            currentRoundName.BindTo(currentRound.Value?.Name);
        }

        private void updateDisplay()
        {
            if (currentRound.Value == null) return;

            roundName.Text = currentRoundName.Value ?? "Unknown Round";

            roundInfo.Text = $"BO{currentRound.Value.BestOf} 回合{banPicks.Count(p => p.Type == ChoiceType.Pick)}";
        }
    }
}
