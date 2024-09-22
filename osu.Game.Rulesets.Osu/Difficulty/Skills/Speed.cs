// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    /// <summary>
    /// Represents the skill required to press keys with regards to keeping up with the speed at which objects need to be hit.
    /// </summary>
    public class Speed : OsuStrainSkill
    {
        protected virtual double SkillMultiplier => 2.6;
        protected virtual double StrainDecayBase => 0.1;

        private double currentStrain;

        public Speed(Mod[] mods) : base(mods)
        {
        }

        protected double StrainDecay(double ms) => Math.Pow(StrainDecayBase, ms / 1000);

        protected override double CalculateInitialStrain(double time, DifficultyHitObject current) => currentStrain * StrainDecay(time - current.Previous(0).StartTime);
        protected override double StrainValueAt(DifficultyHitObject current)
        {
            currentStrain *= StrainDecay(((OsuDifficultyHitObject)current).StrainTime);
            var osuCurrent = (OsuDifficultyHitObject)current;

            double ms = osuCurrent.LastTwoStrainTime / 2;

            // Curves are similar to 2.5 / ms for tapValue and 1 / ms for streamValue, but scale better at high BPM.
            double tapValue = 30 / Math.Pow(ms - 20, 2) + 2 / ms;
            double streamValue = 12.5 / Math.Pow(ms - 20, 2) + 0.25 / ms + 0.005;

            currentStrain += ((1 - osuCurrent.Flow) * tapValue + osuCurrent.Flow * streamValue) * 1000 * SkillMultiplier;

            return currentStrain;
        }
    }
}
