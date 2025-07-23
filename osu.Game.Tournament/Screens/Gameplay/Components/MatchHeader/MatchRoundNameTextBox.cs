// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Game.Overlays.Settings;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.Screens.Gameplay.Components.MatchHeader
{
    public partial class MatchRoundNameTextBox : SettingsTextBox
    {
        [Resolved]
        protected LadderInfo LadderInfo { get; private set; } = null!;

        public MatchRoundNameTextBox()
        {
            LabelText = "Round Name";
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            LadderInfo.CurrentMatch.BindValueChanged(_ => Schedule(reload), true);
        }

        private void reload()
        {
            if (LadderInfo.CurrentMatch.Value?.Round.Value != null)
            {
                Current = LadderInfo.CurrentMatch.Value.Round.Value?.Name.GetBoundCopy()!;
            }
        }
    }
}
