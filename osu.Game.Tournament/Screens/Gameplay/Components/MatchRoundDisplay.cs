// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class MatchRoundDisplay : CircularContainer
    {
        private readonly Bindable<TournamentMatch?> currentMatch = new Bindable<TournamentMatch?>();
        private readonly Bindable<TournamentRound?> currentRound = new Bindable<TournamentRound?>();
        private TournamentSpriteTextWithBackground text = null!;

        [BackgroundDependencyLoader]
        private void load(LadderInfo ladder)
        {
            AutoSizeAxes = Axes.Both;
            Masking = true;

            InternalChild = text = new TournamentSpriteTextWithBackground();

            text.Text.Margin = new MarginPadding { Horizontal = 10f };

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
            text.Text.Text = currentMatch.Value?.Round.Value?.Name.Value ?? "Unknown Round";
        }
    }
}
