// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions;
using osu.Framework.Localisation;
using osu.Game.Overlays.Settings;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.Screens.MapPool
{
    public partial class SettingsWinnerDropdown : SettingsDropdown<TeamColour?>
    {
        public override IEnumerable<LocalisableString> FilterTerms => base.FilterTerms.Concat(Control.Items.Select(i => i.GetLocalisableDescription()));

        public SettingsWinnerDropdown()
        {
            Control.AddDropdownItem(TeamColour.Red);
            Control.AddDropdownItem(TeamColour.Blue);
        }
    }
}
