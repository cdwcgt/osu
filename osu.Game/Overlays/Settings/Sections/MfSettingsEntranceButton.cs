// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Localisation;

namespace osu.Game.Overlays.Settings.Sections
{
    public partial class MfSettingsEntranceButton : SettingsSubsection
    {
        protected override LocalisableString Header => "Super secret settings";

        public MfSettingsEntranceButton(MfSettingsPanel mfpanel)
        {
            Children = new Drawable[]
            {
                new SettingsButton
                {
                    Text = "Open settings menu",
                    TooltipText = "Settings here is not provided by official",
                    Action = mfpanel.ToggleVisibility
                },
            };
        }
    }
}
