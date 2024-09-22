// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using System.Linq;
using System;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    public abstract class OsuStrainSkill : StrainSkill
    {
        protected OsuStrainSkill(Mod[] mods)
            : base(mods)
        {
        }

        protected List<double> ObjectStrains = new List<double>();
        protected double Difficulty;

        public override double DifficultyValue()
        {
            Difficulty = 0;
            double weight = 1;

            // Sections with 0 strain are excluded to avoid worst-case time complexity of the following sort (e.g. /b/2351871).
            // These sections will not contribute to the difficulty.
            var peaks = GetCurrentStrainPeaks().Where(p => p > 0);

            List<double> strains = peaks.OrderDescending().ToList();

            // Difficulty is the weighted sum of the highest strains from every section.
            // We're sorting from highest to lowest strain.
            foreach (double strain in strains.OrderDescending())
            {
                Difficulty += strain * weight;
                weight *= DecayWeight;
            }

            return Difficulty;
        }

        /// <summary>
        /// Returns the number of strains weighted against the top strain.
        /// The result is scaled by clock rate as it affects the total number of strains.
        /// </summary>
        public double CountDifficultStrains()
        {
            if (Difficulty == 0)
                return 0.0;

            double consistentTopStrain = Difficulty / 10; // What would the top strain be if all strain values were identical
            // Use a weighted sum of all strains. Constants are arbitrary and give nice values
            return ObjectStrains.Sum(s => 1.1 / (1 + Math.Exp(-10 * (s / consistentTopStrain - 0.88))));
        }
    }
}
