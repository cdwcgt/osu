// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Framework.Screens;
using osu.Game.Screens;
using osu.Game.Screens.MapGuess;

namespace osu.Game.Overlays.Settings.Sections
{
    public partial class CustomSettingsEntranceButton : SettingsSubsection
    {
        protected override LocalisableString Header => "Super secret settings";

        [Resolved]
        private IPerformFromScreenRunner runner { get; set; } = null!;

        public CustomSettingsEntranceButton(CustomSettingsPanel mfpanel)
        {
            Children = new Drawable[]
            {
                new SettingsButton
                {
                    Text = "Open settings menu",
                    TooltipText = "Settings here is not provided by official",
                    Action = mfpanel.ToggleVisibility
                },
                new SettingsButton
                {
                    Text = "Open map guess",
                    Action = () => runner.PerformFromScreen(s => s.Push(new MapGuessConfigScreen()))
                },
            };
        }
    }
}
