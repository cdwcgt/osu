// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Osu.Mods
{
    public class OsuModDoubleTime : ModDoubleTime
    {
        public override double ScoreMultiplier
        {
            get
            {
                // Round to the nearest multiple of 0.1.
                double value = (int)(SpeedChange.Value * 10) / 10.0;

                // Offset back to 0.
                value -= 1;

                return 1.1 + value / 5;
            }
        }
    }
}
