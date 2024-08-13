// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Configuration;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Replays.Danse.Objects;
using osuTK;
using static osu.Game.Rulesets.Osu.Replays.Danse.Movers.MoverUtilExtensions;

namespace osu.Game.Rulesets.Osu.Replays.Danse.Movers
{
    public class BezierMover : Mover
    {
        private BezierCurve curve;
        private Vector2 pt = OsuAutoGeneratorBase.SPINNER_CENTRE;
        private float previousSpeed;
        private readonly float aggressiveness;
        private readonly float sliderAggressiveness;

        public BezierMover()
        {
            var config = CustomConfigManager.Instance;
            aggressiveness = config.Get<float>(MSetting.BezierAggressiveness);
            sliderAggressiveness = config.Get<float>(MSetting.BezierSliderAggressiveness);
            previousSpeed = -1;
        }

        public override int SetObjects(List<DanceHitObject> objects)
        {
            base.SetObjects(objects);
            float dist = Vector2.Distance(StartPos, EndPos);

            if (previousSpeed < 0)
            {
                previousSpeed = (float)(dist / Duration);
            }

            float genScale = previousSpeed;

            bool ok1 = Start.BaseObject is Slider;
            bool ok2 = End.BaseObject is Slider;
            float dst = 0, dst2 = 0, startAngle = 0, endAngle = 0;

            if (ok1)
            {
                dst = Vector2.Distance(Start.PositionAt((StartTime - 10 - Start.StartTime) / Start.Duration), StartPos);
                endAngle = Start.GetEndAngle();
            }

            if (ok2)
            {
                dst2 = Vector2.Distance(End.PositionAt((EndTime + 10 - End.StartTime) / End.Duration), EndPos);
                startAngle = End.GetStartAngle();
            }

            if (StartPos == EndPos)
                curve = new BezierCurve(StartPos, EndPos);
            else if (ok1 && ok2)
            {
                pt = V2FromRad(endAngle, dst * aggressiveness * sliderAggressiveness / 10) + StartPos;
                var pt2 = V2FromRad(startAngle, dst2 * aggressiveness * sliderAggressiveness / 10) + EndPos;

                curve = new BezierCurve(StartPos, pt, pt2, EndPos);
            }
            else if (ok1)
            {
                var pt1 = V2FromRad(endAngle, dst * aggressiveness * sliderAggressiveness / 10) + StartPos;
                pt = V2FromRad(EndPos.AngleRV(pt), genScale * aggressiveness) + EndPos;

                curve = new BezierCurve(StartPos, pt1, pt, EndPos);
            }
            else if (ok2)
            {
                pt = V2FromRad(StartPos.AngleRV(pt), genScale * aggressiveness) + StartPos;

                var pt1 = V2FromRad(startAngle, dst2 * aggressiveness * sliderAggressiveness / 10) + EndPos;

                curve = new BezierCurve(StartPos, pt, pt1, EndPos);
            }
            else
            {
                float angle = StartPos.AngleRV(pt);

                if (float.IsNaN(angle))
                    angle = 0;

                pt = V2FromRad(angle, previousSpeed * aggressiveness) + StartPos;
                curve = new BezierCurve(StartPos, pt, EndPos);
            }

            previousSpeed = (dist + 1.0f) / (float)Duration;

            return 2;
        }

        public override Vector2 Update(double time) => curve.CalculatePoint(ProgressAt(time));
    }
}
