// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class MatchRoundDisplay : TournamentSpriteTextWithBackground
    {
        private readonly Bindable<TournamentMatch?> currentMatch = new Bindable<TournamentMatch?>();
        private readonly Bindable<TournamentRound?> currentRound = new Bindable<TournamentRound?>();

        [BackgroundDependencyLoader]
        private void load(LadderInfo ladder)
        {
            currentMatch.BindValueChanged(matchChanged);
            currentMatch.BindTo(ladder.CurrentMatch);
        }

        private void matchChanged(ValueChangedEvent<TournamentMatch?> match)
        {
            currentRound.Value = match.NewValue?.Round.Value;
            currentRound.Value?.Name.BindValueChanged(_ => Schedule(updateText), true);
        }

        private void updateText()
        {
            Text.Text = currentMatch.Value?.Round.Value?.Name.Value ?? "Unknown Round";
        }
    }
}
