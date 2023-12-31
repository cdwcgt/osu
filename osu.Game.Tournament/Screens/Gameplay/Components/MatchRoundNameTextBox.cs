// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Game.Overlays.Settings;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class MatchRoundNameTextBox : SettingsTextBox
    {
        private readonly Bindable<string> name = new Bindable<string>("");

        [Resolved]
        protected LadderInfo LadderInfo { get; private set; } = null!;

        public MatchRoundNameTextBox()
        {
            Current = name;
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
                name.BindTo(LadderInfo.CurrentMatch.Value.Round.Value?.Name);
                name.Default = LadderInfo.CurrentMatch.Value.Round.Value?.Name.Value ?? string.Empty;
            }
        }
    }
}
