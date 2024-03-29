// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using System.Linq;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    /// <summary>
    /// 基于应变的skill基类
    /// </summary>
    public abstract class OsuStrainSkill : StrainSkill
    {
        /// <summary>
        /// The default multiplier applied by <see cref="OsuStrainSkill"/> to the final difficulty value after all other calculations.
        /// May be overridden via <see cref="DifficultyMultiplier"/>.
        /// 默认的难度乘数值，可以被DifficultyMultiplier改写
        /// </summary>
        public const double DEFAULT_DIFFICULTY_MULTIPLIER = 1.00;

        /// <summary>
        /// The final multiplier to be applied to <see cref="DifficultyValue"/> after all other calculations.
        /// 最终会应用的难度乘数
        /// </summary>
        protected virtual double DifficultyMultiplier => DEFAULT_DIFFICULTY_MULTIPLIER;

        // 应变衰减的算法
        // 两个物件相隔时间越短值越接近1
        protected double StrainDecay(double ms) => Math.Pow(StrainDecayBase, ms / 1000);

        protected OsuStrainSkill(Mod[] mods)
            : base(mods)
        {
        }

        public override double DifficultyValue()
        {
            double difficulty = 0;
            double weight = 1;

            // Sections with 0 strain are excluded to avoid worst-case time complexity of the following sort (e.g. /b/2351871).
            // These sections will not contribute to the difficulty.
            var peaks = GetCurrentStrainPeaks().Where(p => p > 0);

            List<double> strains = peaks.OrderDescending().ToList();

            // Difficulty is the weighted sum of the highest strains from every section.
            // We're sorting from highest to lowest strain.
            foreach (double strain in strains.OrderDescending())
            {
                difficulty += strain * weight;
                weight *= DecayWeight;
            }

            return difficulty * DifficultyMultiplier;
        }
    }
}
