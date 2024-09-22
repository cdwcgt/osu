// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.UI;
using osuTK;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    /// <summary>
    /// Represents the skill required to correctly aim at every object in the map with a uniform CircleSize and normalized distances.
    /// </summary>
    public class Aim : OsuStrainSkill
    {
        public Aim(Mod[] mods)
            : base(mods)
        {
        }

        /// <summary>
        /// Collection of objects that have yet to be hit when the current object's preempt begins
        /// </summary>
        private ReverseQueue<OsuDifficultyHitObject> preemptHitObjects = new ReverseQueue<OsuDifficultyHitObject>(10);
        private double skillMultiplier => 1059;
        private double strainDecayBase => 0.15;

        private double currentStrain;

        private double strainDecay(double ms) => Math.Pow(strainDecayBase, ms / 1000);

        protected override double CalculateInitialStrain(double time, DifficultyHitObject current) => currentStrain * strainDecay(time - current.Previous(0).StartTime);

        protected override double StrainValueAt(DifficultyHitObject current)
        {
            currentStrain *= strainDecay(current.DeltaTime);

            var osuCurrent = (OsuDifficultyHitObject)current;

            double aimValue = CalculateAimValue(osuCurrent);
            double readingMultiplier = calculateReadingMultiplier(osuCurrent, Mods.Any(m => m is OsuModHidden), Mods.Any(m => m is OsuModFlashlight));

            currentStrain += aimValue * readingMultiplier * skillMultiplier;
            ObjectStrains.Add(currentStrain);

            return currentStrain;
        }

        protected virtual double CalculateAimValue(OsuDifficultyHitObject current)
        {
            double jumpAim = CalculateJumpAimValue(current);
            double flowAim = CalculateFlowAimValue(current);

            return (jumpAim + flowAim) * CalculateSmallCircleBonus(((OsuHitObject)current.BaseObject).Radius);
        }

        protected double CalculateJumpAimValue(OsuDifficultyHitObject current)
        {
            if (current.Flow == 1)
                return 0;

            double distance = current.JumpDistance / OsuDifficultyHitObject.NORMALISED_RADIUS;

            double jumpAimBase = distance / current.StrainTime;

            OsuDifficultyHitObject[] previousTwoObjects = new OsuDifficultyHitObject[0];

            OsuDifficultyHitObject? previousObject = null;

            double locationWeight = 1;
            if (current.Index > 0)
            {
                previousObject = (OsuDifficultyHitObject)current.Previous(0);
                locationWeight = calculateLocationWeight(((OsuHitObject)current.BaseObject).Position, ((OsuHitObject)previousObject.BaseObject).Position);
                if (current.Index > 1)
                {
                    previousTwoObjects = new OsuDifficultyHitObject[] { previousObject, (OsuDifficultyHitObject)current.Previous(1) };
                }
                else
                {
                    previousTwoObjects = new OsuDifficultyHitObject[] { previousObject };
                }
            }



            double angleWeight = calculateJumpAngleWeight(current.Angle, current.StrainTime, previousObject?.StrainTime ?? 0, previousObject?.JumpDistance ?? 0);
            double patternWeight = calculateJumpPatternWeight(current, previousTwoObjects);

            double jumpAim = jumpAimBase * angleWeight * patternWeight * locationWeight;
            return jumpAim * (1 - current.Flow);
        }

        protected double CalculateFlowAimValue(OsuDifficultyHitObject current)
        {
            if (current.Flow == 0)
                return 0;

            double distance = current.JumpDistance / OsuDifficultyHitObject.NORMALISED_RADIUS;

            // The 1.9 exponent roughly equals the inherent BPM based scaling the strain mechanism adds in the relevant BPM range.
            // This way the aim value of streams stays more or less consistent for a given velocity.
            // (300 BPM 20 spacing compared to 150 BPM 40 spacing for example.)
            double flowAimBase = (Math.Tanh(distance - 2) + 1) * 2.5 / current.StrainTime + (distance / 5) / current.StrainTime;

            double locationWeight = 1;
            OsuDifficultyHitObject? previousObject = null;
            if (current.Index > 0)
            {
                previousObject = (OsuDifficultyHitObject)current.Previous(0);
                locationWeight = calculateLocationWeight(((OsuHitObject)current.BaseObject).Position, ((OsuHitObject)previousObject.BaseObject).Position);
            }

            double angleWeight = calculateFlowAngleWeight(current.Angle);
            double patternWeight = calculateFlowPatternWeight(current, previousObject, distance);

            double flowAim = flowAimBase * angleWeight * patternWeight * (1 + (locationWeight - 1) / 2);
            return flowAim * current.Flow;
        }

        private double calculateReadingMultiplier(OsuDifficultyHitObject current, bool hiddenEnabled, bool flashlightEnabled)
        {
            // Remove objects that were hit before the current preempt begun
            while (preemptHitObjects.Count > 0 && preemptHitObjects[^1].StartTime < current.StartTime - current.Preempt)
                preemptHitObjects.Dequeue();

            double readingStrain = 0;

            foreach (var previousObject in preemptHitObjects)
                readingStrain += calculateReadingDensity(previousObject.BaseFlow, previousObject.JumpDistance);

            // ~10-15% relative aim bonus at higher density values.
            double densityBonus = Math.Pow(readingStrain, 1.5) / 100;

            double readingMultipler;
            if (hiddenEnabled)
                readingMultipler = 1.05 + densityBonus * 1.5;   // 5% flat aim bonus and density bonus increased by 50%.
            else
                readingMultipler = 1 + densityBonus;

            double flashlightMultiplier = calculateFlashlightMultiplier(flashlightEnabled, current.RawJumpDistance, ((OsuHitObject)current.BaseObject).Radius);
            double highApproachRateMultiplier = calculateHighApproachRateMultiplier(current.Preempt);

            preemptHitObjects.Enqueue(current);

            return readingMultipler * flashlightMultiplier * highApproachRateMultiplier;
        }

        private static double calculateJumpPatternWeight(OsuDifficultyHitObject current, OsuDifficultyHitObject[] previousTwoObjects)
        {
            double jumpPatternWeight = 1;
            foreach (var (previousObject, i) in previousTwoObjects.Select((o, i) => (o, i)))
            {
                double velocityWeight = 1.05;
                if (previousObject.JumpDistance > 0)
                {
                    double velocityRatio = (current.JumpDistance / current.StrainTime) / (previousObject.JumpDistance / previousObject.StrainTime) - 1;
                    if (velocityRatio <= 0)
                        velocityWeight = 1 + velocityRatio * velocityRatio / 2;
                    else if (velocityRatio < 1)
                        velocityWeight = 1 + (-Math.Cos(velocityRatio * Math.PI) + 1) / 40;
                }

                double angleWeight = 1;
                // An additional restriction to stop jumps that come after triples/streams from getting bonuses for the change in angles.
                if (Utils.IsRatioEqual(1, current.StrainTime, previousObject.StrainTime) && !Utils.IsNullOrNaN(current.Angle) && !Utils.IsNullOrNaN(previousObject.Angle))
                {
                    double angleChange = Math.Abs(current.Angle!.Value) - Math.Abs(previousObject.Angle!.Value);
                    if (Math.Abs(angleChange) >= Math.PI / 1.5)
                        angleWeight = 1.05;
                    else
                        angleWeight = 1 + (-Math.Sin(Math.Cos(angleChange * 1.5) * Math.PI / 2) + 1) / 40;
                }

                jumpPatternWeight *= Math.Pow(velocityWeight * angleWeight, 2 - i);
            }

            double distanceRequirement = 0;
            if (previousTwoObjects.Length > 0)
                distanceRequirement = calculateDistanceRequirement(current.StrainTime, previousTwoObjects[0].StrainTime, previousTwoObjects[0].JumpDistance);

            return 1 + (jumpPatternWeight - 1) * distanceRequirement; // Up to ~33% aim bonus.
        }

        private static double calculateFlowPatternWeight(OsuDifficultyHitObject current, OsuDifficultyHitObject? previousObject, double distance)
        {
            if (previousObject == null)
                return 1;

            double distanceRatio = 1;
            if (previousObject.JumpDistance > 0)    
                distanceRatio = current.JumpDistance / previousObject.JumpDistance - 1;

            double distanceBonus = 1;
            if (distanceRatio <= 0)
                distanceBonus = distanceRatio * distanceRatio;
            else if (distanceRatio < 1)
                distanceBonus = (-Math.Cos(distanceRatio * Math.PI) + 1) / 2;

            double angleBonus = 0;
            if (!Utils.IsNullOrNaN(current.Angle) && !Utils.IsNullOrNaN(previousObject.Angle))
            {
                // Only the change relative to a straight line is considred when the angle changes towards the opposite direction.
                // (So it is considered a zero change in angle when the stream just straightens out.)
                // The maximum possible change in either direction still needs to be the same.
                if (current.Angle > 0 && previousObject.Angle < 0 || current.Angle < 0 && previousObject.Angle > 0)
                {
                    double angleChange;
                    if (Math.Abs(current.Angle.Value) > (Math.PI - Math.Abs(previousObject.Angle.Value)) / 2)
                        angleChange = Math.PI - Math.Abs(current.Angle.Value);
                    else
                        angleChange = Math.Abs(previousObject.Angle.Value) + Math.Abs(current.Angle.Value);

                    angleBonus = (-Math.Cos(Math.Sin(angleChange / 2) * Math.PI) + 1) / 2;
                }
                else if (Math.Abs(current.Angle!.Value) < Math.Abs(previousObject.Angle!.Value))
                {
                    double angleChange = current.Angle.Value - previousObject.Angle.Value;
                    angleBonus = (-Math.Cos(Math.Sin(angleChange / 2) * Math.PI) + 1) / 2;
                }

                if (angleBonus > 0)
                {
                    // Restrict the bonus for repeated absolute angles (zigzag streams).
                    double angleChange = Math.Abs(current.Angle.Value) - Math.Abs(previousObject.Angle.Value);
                    angleBonus = Math.Min(angleBonus, (-Math.Cos(Math.Sin(angleChange / 2) * Math.PI) + 1) / 2);
                }
            }

            double isStreamJump = Utils.TransitionToTrue(distanceRatio, 0, 1);

            double distanceWeight = (1 + distanceBonus) * calculateStreamJumpWeight(current.JumpDistance, isStreamJump, distance);
            double angleWeight = 1 + angleBonus * (1 - isStreamJump);

            // The bonuses only apply within streams, not between jump and stream notes.
            return 1 + (distanceWeight * angleWeight - 1) * previousObject.Flow; // Up to 100% aim bonus.
        }

        private static double calculateJumpAngleWeight(double? angle, double deltaTime, double previousDeltaTime, double previousDistance)
        {
            if (angle == null || double.IsNaN(angle.Value))
                return 1;
            else
            {
                double distanceRequirement = calculateDistanceRequirement(deltaTime, previousDeltaTime, previousDistance);
                return 1 + (-Math.Sin(Math.Cos(angle.Value) * Math.PI / 2) + 1) / 10 * distanceRequirement; // Up to 20% aim bonus.
            }
        }

        private static double calculateFlowAngleWeight(double? angle)
        {
            if (angle == null || double.IsNaN(angle.Value))
                return 1;
            else
                return 1 + (Math.Cos(angle.Value) + 1) / 10; // Up to 20% aim bonus.
        }

        private static double calculateStreamJumpWeight(double jumpDistance, double isStreamJump, double distance) // Lower distance scaling for streamjumps.
        {
            if (jumpDistance > 0)
            {
                double flowAimRevertFactor = 1 / ((Math.Tanh(distance - 2) + 1) * 2.5 + distance / 5);
                return (1 - isStreamJump) * 1 + isStreamJump * flowAimRevertFactor * distance;
            }
            else
                return 1;
        }

        private static double calculateLocationWeight(Vector2 position, Vector2 previousPosition)
        {
            // Base the bonus on the middle point of the jump, so jumps along the sides of the screen get a bonus.
            double x = (position.X + previousPosition.X) * 0.5;
            double y = (position.Y + previousPosition.Y) * 0.5;

            // Move origo to playfield center from upper left corner.
            x -= OsuPlayfield.BASE_SIZE.X / 2;
            y -= OsuPlayfield.BASE_SIZE.Y / 2;

            // Easiest area is a tilted ellipse, buff grows outwards.
            double angle = Math.PI / 3;
            double a = (x * Math.Cos(angle) + y * Math.Sin(angle)) / 750;
            double b = (x * Math.Sin(angle) - y * Math.Cos(angle)) / 1000;

            // ~6% aim bonus for big jumps along the edges of the play area.
            // ~12% for small jumps in upper left or lower right corners.
            // These figures are the top end and are rarely if ever reached.
            double locationBonus = a * a + b * b;

            return 1 + locationBonus;
        }

        // [0, 1] interval boolean, used to stop overlapped notes from enabling unintended bonuses.
        private static double calculateDistanceRequirement(double deltaTime, double previousDeltaTime, double previousDistance)
        {
            // Restrict cases when the previous note was significantly slower, as that plays similar to an overlap.
            if (Utils.IsRatioEqualGreater(1, deltaTime, previousDeltaTime))
            {
                // In half the time only half as much movement is required between two circles.
                double overlapDistance = previousDeltaTime / deltaTime * OsuDifficultyHitObject.NORMALISED_RADIUS * 2;
                return Utils.TransitionToTrue(previousDistance, 0, overlapDistance);
            }
            else
                return 0;
        }

        protected static double CalculateSmallCircleBonus(double radius) => 1 + 120 / Math.Pow(radius, 2);

        private static double calculateReadingDensity(double previousBaseFlow, double previousJumpDistance)
        {
            return (1 - previousBaseFlow * 0.75) * (1 + previousBaseFlow * 0.5 * previousJumpDistance / OsuDifficultyHitObject.NORMALISED_RADIUS);
        }

        private static double calculateFlashlightMultiplier(bool flashlightEnabled, double rawJumpDistance, double radius)
        {
            if (flashlightEnabled)
            {
                // 30% aim bonus for notes completely out of the visible area relative to the previous note.
                // The primary goal is to prevent maps sightreadable with Flashlight from getting a bonus.
                // (For example old CS7 maps where every circle is well within flashlight range at all times.)
                return 1 + Utils.TransitionToTrue(rawJumpDistance, OsuPlayfield.BASE_SIZE.Y / 4, radius) * 0.3;
            }
            else
                return 1;
        }

        private static double calculateHighApproachRateMultiplier(double preempt)
        {
            return 1 + (-Math.Tanh((preempt - 325) / 30) + 1) / 15;
        }
    }
}
