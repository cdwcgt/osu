// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.Mover
{
    public static class MoverUtilExtensions
    {
        public static float AngleRV(this Vector2 v1, Vector2 v2) => MathF.Atan2(v1.Y - v2.Y, v1.X - v2.X);
        public static Vector2 V2FromRad(float rad, float radius) => new Vector2(MathF.Cos(rad), MathF.Sin(rad)) * radius;

        /// <summary>
        /// get entry or exit angle of the slider.
        /// </summary>
        /// <param name="slider">The slider for which you want to calculate the angle.</param>
        /// <param name="end">if true, calculate exit angle, else calculate entry angle.</param>
        /// <returns></returns>
        public static float GetAngle(this Slider slider, bool end = false) =>
            (end ? slider.StackedEndPosition : slider.StackedPosition).AngleRV(slider.StackedPositionAt(
                end ? (slider.Duration - 1) / slider.Duration : 1 / slider.Duration
            ));

        public static float AngleBetween(Vector2 centre, Vector2 v1, Vector2 v2)
        {
            float a = Vector2.Distance(centre, v1);
            float b = Vector2.Distance(centre, v2);
            float c = Vector2.Distance(v1, v2);
            return MathF.Acos((a * a + b * b - c * c) / (2 * a * b));
        }

        public static Vector2 ApplyOffset(Vector2 pos, double time, float radius)
        {
            if (radius < 0f)
            {
                radius = OsuHitObject.OBJECT_RADIUS / 2 * 0.98f;
            }

            return pos + V2FromRad((float)time / 100f, radius);
        }
    }
}
