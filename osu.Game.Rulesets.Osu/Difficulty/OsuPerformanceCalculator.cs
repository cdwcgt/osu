// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Distributions;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Osu.Difficulty
{
    public class OsuPerformanceCalculator : PerformanceCalculator
    {
        public OsuPerformanceCalculator()
            : base(new OsuRuleset())
        {
        }

        protected override PerformanceAttributes CreatePerformanceAttributes(ScoreInfo score, DifficultyAttributes attributes)
        {
            Mod[] mods = score.Mods;
            Mod[] visualMods = mods.Where(m => m is ModWithVisibilityAdjustment).ToArray();
            var osuAttributes = (OsuDifficultyAttributes)attributes;

            double accuracy = score.Accuracy;
            int scoreMaxCombo = score.MaxCombo;
            int countGreat = score.Statistics.GetValueOrDefault(HitResult.Great);
            int countOk = score.Statistics.GetValueOrDefault(HitResult.Ok);
            int countMeh = score.Statistics.GetValueOrDefault(HitResult.Meh);
            int countMiss = score.Statistics.GetValueOrDefault(HitResult.Miss);
            int totalHits = countGreat + countOk + countMeh + countMiss;

            double multiplier = 1.12; // This is being adjusted to keep the final pp value scaled around what it used to be when changing things

            // Custom multipliers for NoFail and SpunOut.
            if (mods.Any(m => m is OsuModNoFail))
                multiplier *= Math.Max(0.90, 1.0 - 0.02 * countMiss);

            if (score.Mods.Any(m => m is OsuModSpunOut) && totalHits > 0)
                multiplier *= 1.0 - Math.Pow((double)osuAttributes.SpinnerCount / totalHits, 0.85);

            int accuracyHitObjectsCount = osuAttributes.HitCircleCount;
            if (score.Mods.OfType<OsuModClassic>().All(m => !m.NoSliderHeadAccuracy.Value))
                accuracyHitObjectsCount += osuAttributes.SliderCount;

            double normalisedHitError = calculateNormalisedHitError(osuAttributes.OverallDifficulty, totalHits, accuracyHitObjectsCount, countGreat);
            double missWeight = calculateMissWeight(countMiss);
            double aimWeight = calculateAimWeight(missWeight, normalisedHitError, scoreMaxCombo, osuAttributes.MaxCombo, totalHits, visualMods);
            double speedWeight = calculateSpeedWeight(missWeight, normalisedHitError, scoreMaxCombo, osuAttributes.MaxCombo);
            double accuracyWeight = calculateAccuracyWeight(accuracyHitObjectsCount, visualMods);

            double aimValue = calculateSkillValue(osuAttributes.AimDifficulty) * aimWeight;
            double jumpAimValue = calculateSkillValue(osuAttributes.JumpAimDifficulty) * aimWeight;
            double flowAimValue = calculateSkillValue(osuAttributes.FlowAimDifficulty) * aimWeight;
            double precisionValue = calculateSkillValue(osuAttributes.PrecisionDifficulty) * aimWeight;
            double speedValue = calculateSkillValue(osuAttributes.SpeedDifficulty) * speedWeight;
            double staminaValue = calculateSkillValue(osuAttributes.StaminaDifficulty) * speedWeight;
            double accuracyValue = calculateAccuracyValue(normalisedHitError) * osuAttributes.AccuracyDifficulty * accuracyWeight;

            double totalValue = Math.Pow(
                Math.Pow(aimValue, 1.1) +
                Math.Pow(Math.Max(speedValue, staminaValue), 1.1) +
                Math.Pow(accuracyValue, 1.1),
                1.0 / 1.1
            ) * multiplier;

            PerformanceAttributes result = new OsuPerformanceAttributes()
            {
                Aim = aimValue,
                JumpAim = jumpAimValue,
                FlowAim = flowAimValue,
                Precision = precisionValue,
                Speed = speedValue,
                Stamina = staminaValue,
                Accuracy = accuracyValue,
                Total = totalValue,
            };

            return result;
        }

        private static double calculateSkillValue(double skillDiff) => Math.Pow(skillDiff, 3) * 3.9;

        private static double calculateNormalisedHitError(double od, int objectCount, int accuracyObjectCount, int count300)
        {
            int relevant300Count = count300 - (objectCount - accuracyObjectCount);
            if (relevant300Count <= 0)
                return double.NaN;

            // Probability of landing a 300 where the player has a 20% chance of getting at least the given amount of 300s.
            double probability = Beta.InvCDF(relevant300Count, 1 + accuracyObjectCount - relevant300Count, 0.2);

            probability += (1 - probability) / 2; // Add the left tail of the normal distribution.
            double zValue = Normal.InvCDF(0, 1, probability); // The value on the x-axis for the given probability.

            double hitWindow = 79.5 - od * 6;
            return hitWindow / zValue; // Hit errors are normally distributed along the x-axis.
        }

        private static double calculateMissWeight(int misses) => Math.Pow(0.97, misses);

        private static double calculateAimWeight(double missWeight, double normalizedHitError, int combo, int maxCombo, int objectCount, Mod[] visualMods)
        {
            double accuracyWeight = double.IsNaN(normalizedHitError) ? 0 : Math.Pow(0.995, normalizedHitError) * 1.04;
            double comboWeight = Math.Pow(combo, 0.8) / Math.Pow(maxCombo, 0.8);
            double flashlightLengthWeight = visualMods.Any(m => m is OsuModFlashlight) ? 1 + Math.Atan(objectCount / 2000.0) : 1;

            return accuracyWeight * comboWeight * missWeight * flashlightLengthWeight;
        }

        private static double calculateSpeedWeight(double missWeight, double normalizedHitError, int combo, int maxCombo)
        {
            double accuracyWeight = double.IsNaN(normalizedHitError) ? 0 : Math.Pow(0.985, normalizedHitError) * 1.12;
            double comboWeight = Math.Pow(combo, 0.4) / Math.Pow(maxCombo, 0.4);

            return accuracyWeight * comboWeight * missWeight;
        }

        private static double calculateAccuracyWeight(int accuracyObjectCount, Mod[] visualMods)
        {
            double lengthWeight = Math.Tanh((accuracyObjectCount + 400) / 1050.0) * 1.2;

            double modWeight = 1;
            if (visualMods.Any(m => m is OsuModHidden))
                modWeight *= 1.02;
            if (visualMods.Any(m => m is OsuModFlashlight))
                modWeight *= 1.04;

            return lengthWeight * modWeight;
        }

        private static double calculateAccuracyValue(double normalizedHitError) => double.IsNaN(normalizedHitError) ? 0 : 560 * Math.Pow(0.85, normalizedHitError);
    }
}
