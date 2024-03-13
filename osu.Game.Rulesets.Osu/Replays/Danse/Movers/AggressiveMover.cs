// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;
using static osu.Game.Rulesets.Osu.Replays.Danse.Movers.MoverUtilExtensions;

namespace osu.Game.Rulesets.Osu.Replays.Danse.Movers
{
    public class AggressiveMover : Mover
    {
        private BezierCurve curve;
        private float lastAngle;

        public override void SetObjects(List<DanceHitObject> objects)
        {
            base.SetObjects(objects);

            float scaledDistance = StartPos == EndPos ? 1 : (float)(EndTime - StartTime);
            float newAngle = lastAngle + MathF.PI;

            if (Start.BaseObject is Slider start)
                newAngle = start.GetEndAngle();

            var p1 = V2FromRad(newAngle, scaledDistance) + StartPos;
            var p2 = Vector2.Zero;

            if (scaledDistance > 1)
                lastAngle = p1.AngleRV(EndPos);

            if (End.BaseObject is Slider end)
            {
                p2 = V2FromRad(end.GetStartAngle(), scaledDistance) + EndPos;
            }

            curve = new BezierCurve(StartPos, p1);
            if (p2 != Vector2.Zero) curve.Points.Add(p2);
            curve.Points.Add(EndPos);
        }

        public override Vector2 Update(double time) => curve.CalculatePoint(ProgressAt(time));
    }
}
