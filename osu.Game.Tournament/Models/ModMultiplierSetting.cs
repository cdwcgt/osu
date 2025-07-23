// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Game.Beatmaps.Legacy;

namespace osu.Game.Tournament.Models
{
    public class ModMultiplierSetting
    {
        public Bindable<LegacyMods> Mods { get; set; } = new Bindable<LegacyMods>();

        public BindableDouble Multiplier { get; set; } = new BindableDouble
        {
            MinValue = 0,
            MaxValue = 3,
            Precision = 0.1,
        };
    }
}
