// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using osu.Game.Configuration;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;
using static osu.Game.Rulesets.Osu.Replays.Danse.Movers.MoverUtilExtensions;

namespace osu.Game.Rulesets.Osu.Replays.Danse.Movers
{
    public class FlowerMover : Mover
    {
        private readonly float mult;
        private readonly float nextMult;
        private readonly float offsetMult;

        private float offset => MathF.PI * offsetMult;
        private float invert = 1;
        private float lastAngle;
        private Vector2 lastPoint;
        private BezierCurveCubic curve;

        public FlowerMover()
        {
            var config = MConfigManager.Instance;
            mult = config.Get<float>(MSetting.JumpMult);
            nextMult = config.Get<float>(MSetting.JumpMult);
            offsetMult = config.Get<float>(MSetting.AngleOffset);
        }

        public override void SetObjects(List<DanceHitObject> objects)
        {
            base.SetObjects(objects);

            Vector2 p1, p2;
            float dist = Vector2.Distance(StartPos, EndPos);
            float scaled = mult * dist;
            float next = nextMult * dist;

            Slider start = Start.BaseObject as Slider;
            Slider end = End.BaseObject as Slider;

            float newAngle = offset * invert;

            if (start != null && end != null)
            {
                invert *= -1;
                p1 = V2FromRad(start.GetEndAngle(), scaled) + StartPos;
                p2 = V2FromRad(end.GetStartAngle(), next) + EndPos;
            }
            else if (start != null)
            {
                invert *= -1;
                lastAngle = StartPos.AngleRV(EndPos) - newAngle;

                p1 = V2FromRad(start.GetEndAngle(), scaled) + StartPos;
                p2 = V2FromRad(lastAngle, next) + EndPos;
            }
            else if (end != null)
            {
                lastAngle += MathF.PI;
                p1 = V2FromRad(lastAngle, scaled) + StartPos;
                p2 = V2FromRad(end.GetStartAngle(), next) + EndPos;
            }
            else
            {
                if (AngleBetween(StartPos, lastPoint, EndPos) >= offset)
                    invert *= -1;

                newAngle = StartPos.AngleRV(EndPos) - newAngle;

                p1 = V2FromRad(lastAngle + MathF.PI, scaled) + StartPos;
                p2 = V2FromRad(newAngle, next) + EndPos;
                if (scaled / mult > 2) lastAngle = newAngle;
            }

            lastPoint = StartPos;
            curve = new BezierCurveCubic(StartPos, EndPos, p1, p2);
        }

        public override Vector2 Update(double time) => curve.CalculatePoint(ProgressAt(time));
    }
}
