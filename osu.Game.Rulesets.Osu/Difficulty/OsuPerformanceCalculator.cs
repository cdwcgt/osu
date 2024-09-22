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

        private const bool enable_csr = true;

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

            double effectiveMissCount = countMiss;

            double multiplier = 1.12; // This is being adjusted to keep the final pp value scaled around what it used to be when changing things

            // Custom multipliers for NoFail and SpunOut.
            if (mods.Any(m => m is OsuModNoFail))
                multiplier *= Math.Max(0.90, 1.0 - 0.02 * countMiss);

            if (score.Mods.Any(m => m is OsuModSpunOut) && totalHits > 0)
                multiplier *= 1.0 - Math.Pow((double)osuAttributes.SpinnerCount / totalHits, 0.85);

            int accuracyHitObjectsCount = osuAttributes.HitCircleCount;

            if (score.Mods.OfType<OsuModClassic>().All(m => !m.NoSliderHeadAccuracy.Value))
            {
                accuracyHitObjectsCount += osuAttributes.SliderCount;
            }
            else if (enable_csr)
            {
                effectiveMissCount = Math.Max(countMiss, calculateEffectiveMissCount(osuAttributes, scoreMaxCombo, countMiss, totalHits - countGreat));
            }

            double normalisedHitError = calculateNormalisedHitError(osuAttributes.OverallDifficulty, totalHits, accuracyHitObjectsCount, countGreat);

            double aimWeight = calculateAimWeight(normalisedHitError, scoreMaxCombo, osuAttributes.MaxCombo, totalHits, visualMods);
            double speedWeight = calculateSpeedWeight(normalisedHitError, scoreMaxCombo, osuAttributes.MaxCombo);
            double accuracyWeight = calculateAccuracyWeight(accuracyHitObjectsCount, visualMods);

            double aimValue = aimWeight * calculateSkillValue(osuAttributes.AimDifficulty) * calculateMissWeight(countMiss, osuAttributes.AimDifficultyStrainsCount);
            double jumpAimValue = aimWeight * calculateSkillValue(osuAttributes.JumpAimDifficulty) * calculateMissWeight(countMiss, osuAttributes.JumpAimDifficultyStrainsCount);
            double flowAimValue = aimWeight * calculateSkillValue(osuAttributes.FlowAimDifficulty) * calculateMissWeight(countMiss, osuAttributes.FlowAimDifficultyStrainsCount);
            double precisionValue = aimWeight * calculateSkillValue(osuAttributes.PrecisionDifficulty) * calculateMissWeight(countMiss, osuAttributes.AimDifficultyStrainsCount);
            double speedValue = speedWeight * calculateSkillValue(osuAttributes.SpeedDifficulty) * calculateMissWeight(countMiss, osuAttributes.SpeedDifficultyStrainsCount);
            double staminaValue = speedWeight * calculateSkillValue(osuAttributes.StaminaDifficulty) * calculateMissWeight(countMiss, osuAttributes.StaminaDifficultyStrainsCount);

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
                return 200 - od * 10;

            // Probability of landing a 300 where the player has a 20% chance of getting at least the given amount of 300s.
            double probability = Beta.InvCDF(relevant300Count, 1 + accuracyObjectCount - relevant300Count, 0.2);

            probability += (1 - probability) / 2; // Add the left tail of the normal distribution.
            double zValue = Normal.InvCDF(0, 1, probability); // The value on the x-axis for the given probability.

            double hitWindow = 79.5 - od * 6;
            return hitWindow / zValue; // Hit errors are normally distributed along the x-axis.
        }

        private static double calculateMissWeight(double misses, double difficultStrainCount) => enable_csr ? 0.96 / ((misses / (4 * Math.Pow(Math.Log(difficultStrainCount), 0.94))) + 1) : Math.Pow(0.97, misses);

        private static double calculateAimWeight(double normalizedHitError, int combo, int maxCombo, int objectCount, Mod[] visualMods)
        {
            double accuracyWeight = Math.Pow(0.995, normalizedHitError) * 1.04;
            double comboWeight = Math.Pow(combo, 0.8) / Math.Pow(maxCombo, 0.8);
            double flashlightLengthWeight = visualMods.Any(m => m is OsuModFlashlight) ? 1 + comboWeight * Math.Atan(objectCount / 2000.0) : 1;

            return accuracyWeight * (enable_csr ? 1 : comboWeight) * flashlightLengthWeight;
        }

        private static double calculateSpeedWeight(double normalizedHitError, int combo, int maxCombo)
        {
            double accuracyWeight = Math.Pow(0.985, normalizedHitError) * 1.12;
            double comboWeight = Math.Pow(combo, 0.4) / Math.Pow(maxCombo, 0.4);

            return accuracyWeight * (enable_csr ? 1 : comboWeight);
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

        private static double calculateAccuracyValue(double normalizedHitError) => 560 * Math.Pow(0.85, normalizedHitError);

        private static double calculateEffectiveMissCount(OsuDifficultyAttributes attributes, int scoreMaxCombo, int countMiss, int countMistakes)
        {
            // Guess the number of misses + slider breaks from combo
            double comboBasedMissCount = 0.0;

            if (attributes.SliderCount > 0)
            {
                double fullComboThreshold = attributes.MaxCombo - 0.1 * attributes.SliderCount;
                if (scoreMaxCombo < fullComboThreshold)
                    comboBasedMissCount = fullComboThreshold / Math.Max(1.0, scoreMaxCombo);
            }

            // Clamp miss count to maximum amount of possible breaks
            comboBasedMissCount = Math.Min(comboBasedMissCount, countMistakes);

            return Math.Max(countMiss, comboBasedMissCount);
        }
    }
}
