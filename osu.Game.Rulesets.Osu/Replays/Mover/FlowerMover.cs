// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
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
        private BezierCurveCubic curve;

        public FlowerMover(IBeatmap beatmap, IReadOnlyList<Mod> mods, float jumpMultiplier, float angleOffset)
            : base(beatmap, mods)
        {
            mult = jumpMultiplier;
            nextMult = jumpMultiplier;
            offsetMult = angleOffset;
        }

        protected override void OnObjChange()
        {
            Vector2 p1, p2;
            float dist = Vector2.Distance(CurrentPostion, TargetPostion);
            float scaled = mult * dist;
            float next = nextMult * dist;

            Slider? start = CurrentObject as Slider;
            Slider? end = TargetObject as Slider;

            float newAngle = offset * invert;

            if (start != null && end != null)
            {
                invert *= -1;
                p1 = MoverUtilExtensions.V2FromRad(start.GetEndAngle(), scaled) + CurrentPostion;
                p2 = MoverUtilExtensions.V2FromRad(end.GetStartAngle(), next) + TargetPostion;
            }
            else if (start != null)
            {
                invert *= -1;
                lastAngle = CurrentPostion.AngleRV(TargetPostion) - newAngle;

                p1 = MoverUtilExtensions.V2FromRad(start.GetEndAngle(), scaled) + CurrentPostion;
                p2 = MoverUtilExtensions.V2FromRad(lastAngle, next) + TargetPostion;
            }
            else if (end != null)
            {
                lastAngle += MathF.PI;
                p1 = MoverUtilExtensions.V2FromRad(lastAngle, scaled) + CurrentPostion;
                p2 = MoverUtilExtensions.V2FromRad(end.GetStartAngle(), next) + TargetPostion;
            }
            else
            {
                if (MoverUtilExtensions.AngleBetween(CurrentPostion, lastPoint, TargetPostion) >= offset)
                    invert *= -1;

                newAngle = CurrentPostion.AngleRV(TargetPostion) - newAngle;

                p1 = MoverUtilExtensions.V2FromRad(lastAngle + MathF.PI, scaled) + CurrentPostion;
                p2 = MoverUtilExtensions.V2FromRad(newAngle, next) + TargetPostion;
                if (scaled / mult > 2) lastAngle = newAngle;
            }

            lastPoint = CurrentPostion;
            curve = new BezierCurveCubic(CurrentPostion, TargetPostion, p1, p2);
        }

        protected override Vector2 Update(double time) => curve.CalculatePoint((float)((time - CurrentObjectTime) / Duration));
    }
}
