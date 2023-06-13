// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.Mover
{
    public class FlowerMover : OsuDanceGenerator
    {
        private readonly float mult;
        private readonly float nextMult;
        private readonly float offsetMult;

        private float offset => MathF.PI * offsetMult;
        private float invert = 1;
        private float lastAngle;
        private Vector2 lastPoint;
        private SliderPath? path;

        public FlowerMover(IBeatmap beatmap, IReadOnlyList<Mod> mods, float jumpMultiplier, float angleOffset)
            : base(beatmap, mods)
        {
            mult = nextMult = jumpMultiplier;
            offsetMult = angleOffset;
        }

        protected override void OnObjChange()
        {
            Vector2 p1, p2;
            float distance = Vector2.Distance(CurrentPosition, TargetPosition);
            float scaledDistance = mult * distance;
            float nextScaledDistance = nextMult * distance;

            // check if object is a Slider.
            Slider? start = CurrentObject as Slider;
            Slider? end = TargetObject as Slider;

            float newAngle = offset * invert;

            // start object and end object are all slider.
            if (start != null && end != null)
            {
                invert *= -1;
                p1 = MoverUtilExtensions.V2FromRad(start.GetAngle(true), scaledDistance) + CurrentPosition;
                p2 = MoverUtilExtensions.V2FromRad(end.GetAngle(), nextScaledDistance) + TargetPosition;
            }
            // start object is slider.
            else if (start != null)
            {
                invert *= -1;
                lastAngle = CurrentPosition.AngleRV(TargetPosition) - newAngle;

                p1 = MoverUtilExtensions.V2FromRad(start.GetAngle(true), scaledDistance) + CurrentPosition;
                p2 = MoverUtilExtensions.V2FromRad(lastAngle, nextScaledDistance) + TargetPosition;
            }
            // end object is slider.
            else if (end != null)
            {
                lastAngle += MathF.PI;
                p1 = MoverUtilExtensions.V2FromRad(lastAngle, scaledDistance) + CurrentPosition;
                p2 = MoverUtilExtensions.V2FromRad(end.GetAngle(), nextScaledDistance) + TargetPosition;
            }
            // all are not slider.
            else
            {
                if (MoverUtilExtensions.AngleBetween(CurrentPosition, lastPoint, TargetPosition) >= offset)
                    invert *= -1;

                newAngle = CurrentPosition.AngleRV(TargetPosition) - newAngle;

                p1 = MoverUtilExtensions.V2FromRad(lastAngle + MathF.PI, scaledDistance) + CurrentPosition;
                p2 = MoverUtilExtensions.V2FromRad(newAngle, nextScaledDistance) + TargetPosition;
                if (scaledDistance / mult > 2) lastAngle = newAngle;
            }

            lastPoint = CurrentPosition;
            path = new SliderPath(new[]
            {
                new PathControlPoint(CurrentPosition, PathType.Bezier),
                new PathControlPoint(p1),
                new PathControlPoint(p2),
                new PathControlPoint(TargetPosition)
            });
        }

        protected override Vector2 GetPosition(double time) => path?.PositionAt((float)((time - CurrentObjectTime) / Duration)) ?? Vector2.One;
    }
}
