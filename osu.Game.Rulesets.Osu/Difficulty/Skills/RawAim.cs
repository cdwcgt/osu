// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    public class RawAim : Aim
    {
        protected override double CalculateAimValue(OsuDifficultyHitObject current) => CalculateJumpAimValue(current) + CalculateFlowAimValue(current);

        public RawAim(Mod[] mods) : base(mods)
        {
        }
    }
}
