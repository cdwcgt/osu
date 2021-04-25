// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Distributions;
using osu.Framework.Extensions;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Osu.Difficulty
{
    public class OsuPerformanceCalculator : PerformanceCalculator
    {
        public new OsuDifficultyAttributes Attributes => (OsuDifficultyAttributes)base.Attributes;

        public OsuPerformanceCalculator(Ruleset ruleset, DifficultyAttributes attributes, ScoreInfo score)
            : base(ruleset, attributes, score)
        {
        }

        public override double Calculate(Dictionary<string, double> categoryRatings = null)
        {
            Mod[] mods = Score.Mods;
            Mod[] visualMods = mods.Where(m => m is ModWithVisibilityAdjustment).ToArray();
            int scoreMaxCombo = Score.MaxCombo;
            int countGreat = Score.Statistics.GetOrDefault(HitResult.Great);
            int countOk = Score.Statistics.GetOrDefault(HitResult.Ok);
            int countMeh = Score.Statistics.GetOrDefault(HitResult.Meh);
            int countMiss = Score.Statistics.GetOrDefault(HitResult.Miss);
            int totalHits = countGreat + countOk + countMeh + countMiss;

            // Don't count scores made with supposedly unranked mods
            if (mods.Any(m => !m.Ranked))
                return 0;

            double multiplier = 1.12; // This is being adjusted to keep the final pp value scaled around what it used to be when changing things

            // Custom multipliers for NoFail and SpunOut.
            if (mods.Any(m => m is OsuModNoFail))
                multiplier *= Math.Max(0.90, 1.0 - 0.02 * countMiss);

            if (mods.Any(m => m is OsuModSpunOut))
                multiplier *= 1.0 - Math.Pow((double)Attributes.SpinnerCount / totalHits, 0.85);

            double normalisedHitError = calculateNormalisedHitError(Attributes.OverallDifficulty, totalHits, Attributes.HitCircleCount, countGreat);
            double missWeight = calculateMissWeight(countMiss);
            double aimWeight = calculateAimWeight(missWeight, normalisedHitError, scoreMaxCombo, Attributes.MaxCombo, totalHits, visualMods);
            double speedWeight = calculateSpeedWeight(missWeight, normalisedHitError, scoreMaxCombo, Attributes.MaxCombo);
            double accuracyWeight = calculateAccuracyWeight(Attributes.HitCircleCount, visualMods);

            double aimValue = calculateSkillValue(Attributes.AimStrain) * aimWeight;
            double jumpAimValue = calculateSkillValue(Attributes.JumpAimStrain) * aimWeight;
            double flowAimValue = calculateSkillValue(Attributes.FlowAimStrain) * aimWeight;
            double precisionValue = calculateSkillValue(Attributes.PrecisionStrain) * aimWeight;
            double speedValue = calculateSkillValue(Attributes.SpeedStrain) * speedWeight;
            double staminaValue = calculateSkillValue(Attributes.StaminaStrain) * speedWeight;
            double accuracyValue = calculateAccuracyValue(normalisedHitError) * Attributes.AccuracyStrain * accuracyWeight;

            double totalValue = Math.Pow(
                Math.Pow(aimValue, 1.1) +
                Math.Pow(Math.Max(speedValue, staminaValue), 1.1) +
                Math.Pow(accuracyValue, 1.1),
                1.0 / 1.1
            ) * multiplier;

            if (categoryRatings != null)
            {
                categoryRatings.Add("Aim", aimValue);
                categoryRatings.Add("Jump Aim", jumpAimValue);
                categoryRatings.Add("Flow Aim", flowAimValue);
                categoryRatings.Add("Precision", precisionValue);
                categoryRatings.Add("Speed", speedValue);
                categoryRatings.Add("Stamina", staminaValue);
                categoryRatings.Add("Accuracy", accuracyValue);
                categoryRatings.Add("OD", Attributes.OverallDifficulty);
                categoryRatings.Add("AR", Attributes.ApproachRate);
                categoryRatings.Add("Max Combo", Attributes.MaxCombo);
            }

            return totalValue;
        }

        private static double calculateSkillValue(double skillDiff) => Math.Pow(skillDiff, 3) * 3.9;

        private static double calculateNormalisedHitError(double od, int objectCount, int circleCount, int count300)
        {
            int circle300Count = count300 - (objectCount - circleCount);
            if (circle300Count <= 0)
                return double.NaN;

            // Probability of landing a 300 where the player has a 20% chance of getting at least the given amount of 300s.
            double probability = Beta.InvCDF(circle300Count, 1 + circleCount - circle300Count, 0.2);

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

        private static double calculateAccuracyWeight(int circleCount, Mod[] visualMods)
        {
            double lengthWeight = Math.Tanh((circleCount + 400) / 1050.0) * 1.2;

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
