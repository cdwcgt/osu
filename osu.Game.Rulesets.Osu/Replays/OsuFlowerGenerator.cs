// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Game.Replays;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays
{
    public class OsuFlowerGenerator : OsuAutoGenerator
    {
        /// <summary>
        /// The first control point's distance multiplier.
        /// </summary>
        private readonly float multiplier;

        /// <summary>
        /// The second control point's distance multiplier.
        /// </summary>
        private readonly float nextMultiplier;

        private readonly float angleOffset;

        private float angle => MathF.PI * angleOffset;

        /// <summary>
        /// Determine the direction of rotation.
        /// </summary>
        private float invert = 1;

        private float lastAngle;
        private Vector2 previousPotision;

        protected override int FrameRate => 120;

        public OsuFlowerGenerator(IBeatmap beatmap, IReadOnlyList<Mod> mods, float jumpMultiplier, float angleOffset)
            : base(beatmap, mods)
        {
            multiplier = nextMultiplier = jumpMultiplier;
            this.angleOffset = angleOffset;
        }

        public override Replay Generate()
        {
            if (Beatmap.HitObjects.Count == 0)
                return Replay;

            ButtonIndex = 0;

            AddFrameToReplay(new OsuReplayFrame(Beatmap.HitObjects[0].StartTime - 1500, new Vector2(256, 500)));

            for (int i = 0; i < Beatmap.HitObjects.Count; i++)
            {
                OsuHitObject h = Beatmap.HitObjects[i];

                OsuHitObject? prev = i > 0 ? Beatmap.HitObjects[i - 1] : null;

                addHitObjectReplay(h, prev);
            }

            return Replay;
        }

        private void addHitObjectReplay(OsuHitObject h, OsuHitObject? prev)
        {
            Vector2 controlPoint1, controlPoint2;
            Vector2 currentPosition = prev?.StackedEndPosition ?? new Vector2(256, 500);
            Vector2 targetPosition = h.StackedPosition;

            double currentTime = Frames[^1].Time;
            double targetTime = h.StartTime;

            float spinnerDirection = -1;

            // Move only if object is visible.
            if (targetTime - currentTime > h.TimePreempt)
            {
                currentTime = targetTime - h.TimePreempt;
            }

            // just the same as auto.
            if (h is Spinner spinner)
            {
                if (spinner.SpinsRequired == 0)
                    return;

                CalcSpinnerStartPosAndDirection(((OsuReplayFrame)Frames[^1]).Position, out targetPosition, out spinnerDirection);
            }

            float distance = Vector2.Distance(currentPosition, targetPosition);
            float scaledDistance = multiplier * distance;
            float nextScaledDistance = nextMultiplier * distance;

            // check if object is a Slider.
            Slider? start = prev as Slider;
            Slider? end = h as Slider;

            float newAngle = angle * invert;

            // start object and end object are all slider.
            // Take the exit and entry angles of the slider as the angle of control point.
            if (start != null && end != null)
            {
                invert *= -1;
                controlPoint1 = RadToVec2(GetSliderAngle(start, true), scaledDistance) + currentPosition;
                controlPoint2 = RadToVec2(GetSliderAngle(end), nextScaledDistance) + targetPosition;
            }
            // start object is slider.
            else if (start != null)
            {
                invert *= -1;
                lastAngle = Vec2ToAngle(targetPosition, currentPosition) - newAngle;

                controlPoint1 = RadToVec2(GetSliderAngle(start, true), scaledDistance) + currentPosition;
                controlPoint2 = RadToVec2(lastAngle, nextScaledDistance) + targetPosition;
            }
            // end object is slider.
            else if (end != null)
            {
                lastAngle += MathF.PI;
                controlPoint1 = RadToVec2(lastAngle, scaledDistance) + currentPosition;
                controlPoint2 = RadToVec2(GetSliderAngle(end), nextScaledDistance) + targetPosition;
            }
            // all are not slider.
            else
            {
                if (AngleBetween(currentPosition, previousPotision, targetPosition) >= angle)
                    invert *= -1;

                newAngle = Vec2ToAngle(targetPosition, currentPosition) - newAngle;

                controlPoint1 = RadToVec2(lastAngle + MathF.PI, scaledDistance) + currentPosition;
                controlPoint2 = RadToVec2(newAngle, nextScaledDistance) + targetPosition;

                // do not change angle when circle was stacked.
                if (h.StackHeight == 0)
                    lastAngle = newAngle;
            }

            previousPotision = currentPosition;
            SliderPath path = new SliderPath(new[]
            {
                new PathControlPoint(currentPosition, PathType.BEZIER),
                new PathControlPoint(controlPoint1),
                new PathControlPoint(controlPoint2),
                new PathControlPoint(targetPosition)
            });

            for (double time = currentTime; time < h.StartTime; time += GetFrameDelay(time))
            {
                AddFrameToReplay(new OsuReplayFrame(time, path.PositionAt((time - currentTime) / (targetTime - currentTime))));
            }

            double timeDifference = ApplyModsToTimeDelta(currentTime, h.StartTime);
            if (timeDifference > 0 && timeDifference < 266)
                ButtonIndex++;
            else
                ButtonIndex = 0;

            // Flower do not have any extra handle for click, so use Auto's method.
            AddHitObjectClickFrames(h, targetPosition, spinnerDirection);
        }

        #region Helper / Calculator

        public static float Vec2ToAngle(Vector2 v1, Vector2 v2) => MathF.Atan2(v2.Y - v1.Y, v2.X - v1.X);
        public static Vector2 RadToVec2(float rad, float radius) => new Vector2(MathF.Cos(rad), MathF.Sin(rad)) * radius;

        /// <summary>
        /// get entry or exit angle of the slider.
        /// </summary>
        /// <param name="slider">The slider for which you want to calculate the angle.</param>
        /// <param name="end">if true, calculate exit angle, else calculate entry angle.</param>
        /// <returns></returns>
        public static float GetSliderAngle(Slider slider, bool end = false) =>
            Vec2ToAngle(slider.StackedPositionAt(end ? (slider.Duration - 1) / slider.Duration : 1 / slider.Duration), end ? slider.StackedEndPosition : slider.StackedPosition);

        public static float AngleBetween(Vector2 centre, Vector2 v1, Vector2 v2)
        {
            float a = Vector2.Distance(centre, v1);
            float b = Vector2.Distance(centre, v2);
            float c = Vector2.Distance(v1, v2);
            return MathF.Acos((a * a + b * b - c * c) / (2 * a * b));
        }

        #endregion
    }
}
