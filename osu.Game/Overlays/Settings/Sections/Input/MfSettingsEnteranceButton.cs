﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Localisation;

namespace osu.Game.Overlays.Settings.Sections.Input
{
    public partial class MfSettingsEnteranceButton : SettingsSubsection
    {
        protected override LocalisableString Header => "Mf-osu选项";

        public MfSettingsEnteranceButton(MfSettingsPanel mfpanel)
        {
            Children = new Drawable[]
            {
                new SettingsButton
                {
                    Text = "打开选项菜单",
                    TooltipText = "更改Mf-osu的设置",
                    Action = mfpanel.ToggleVisibility
                },
            };
        }
    }
}
