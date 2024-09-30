// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Configuration;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Replays.Danse.Objects;
using osuTK;
using static osu.Game.Rulesets.Osu.Replays.Danse.Movers.MoverUtilExtensions;

namespace osu.Game.Rulesets.Osu.Replays.Danse.Movers
{
    public class MomentumMover : Mover
    {
        private float offset => restrictAngle * MathF.PI / 180.0f;

        private Vector2 last;
        private BezierCurveCubic curve;
        private bool first = true;
        private readonly float jumpMult;
        private readonly float offsetMult;
        private readonly bool skipStacks;
        private readonly bool streamRestrict;
        private readonly float restrictArea;
        private readonly float restrictAngle;
        private readonly float streamMult;
        private readonly bool restrictInvert;
        private readonly float durationTrigger;
        private readonly float durationMult;

        private readonly float streamArea;
        private readonly float restrictAngleAdd;
        private readonly float restrictAngleSub;
        private readonly float equalPosBounce;
        private readonly bool sliderPredict;
        private readonly bool interpolateAngles;
        private readonly bool invertAngleInterpolation;

        public MomentumMover()
        {
            var config = CustomConfigManager.Instance;
            jumpMult = config.Get<float>(CustomSetting.JumpMult);
            offsetMult = config.Get<float>(CustomSetting.AngleOffset);
            skipStacks = config.Get<bool>(CustomSetting.SkipStackAngles);
            restrictInvert = config.Get<bool>(CustomSetting.RestrictInvert);
            restrictAngle = config.Get<float>(CustomSetting.RestrictAngle);
            restrictArea = config.Get<float>(CustomSetting.RestrictArea);
            streamRestrict = config.Get<bool>(CustomSetting.StreamRestrict);
            streamMult = config.Get<float>(CustomSetting.StreamMult);
            durationTrigger = config.Get<float>(CustomSetting.DurationTrigger);
            durationMult = config.Get<float>(CustomSetting.DurationMult);

            streamArea = config.Get<float>(CustomSetting.StreamArea);
            equalPosBounce = config.Get<float>(CustomSetting.EqualPosBounce);
            restrictAngleAdd = config.Get<float>(CustomSetting.RestrictAngleAdd);
            restrictAngleSub = config.Get<float>(CustomSetting.RestrictAngleSub);
            sliderPredict = config.Get<bool>(CustomSetting.SliderPredict);
            interpolateAngles = config.Get<bool>(CustomSetting.InterpolateAngles);
            invertAngleInterpolation = config.Get<bool>(CustomSetting.InvertAngleInterpolation);
        }

        private bool isSame(DanceHitObject o1, DanceHitObject o2) => isSame(o1, o2, skipStacks);

        private bool isSame(DanceHitObject o1, DanceHitObject o2, bool skipStacks) =>
            o1.StartPos == o2.StartPos || (skipStacks && o1.BaseObject.Position == o2.BaseObject.Position);

        public override int SetObjects(List<DanceHitObject> objects)
        {
            base.SetObjects(objects);
            DanceHitObject? next = null;

            if (objects.Count > 2) next = objects[2];

            float area = restrictArea * MathF.PI / 180f;
            float sarea = streamArea * MathF.PI / 180f;
            float mult = jumpMult;
            float distance = Vector2.Distance(StartPos, EndPos);

            bool fromLong = false;
            float a, a2 = 0;

            for (int i = 1; i < objects.Count; i++)
            {
                var o = objects[i];

                if (o.BaseObject is Slider)
                {
                    a2 = o.GetStartAngle();
                    fromLong = true;
                    break;
                }

                if (i == objects.Count - 1)
                {
                    a2 = last.AngleRV(StartPos);
                    break;
                }

                var o2 = objects[i + 1];
                a2 = o.StartPos.AngleRV(o2.StartPos);

                if (!isSame(o, o2))
                {
                    if (o2.BaseObject is Slider && sliderPredict)
                    {
                        var pos = StartPos;
                        var pos2 = EndPos;
                        float s2a = o2.GetStartAngle();
                        float dst2 = Vector2.Distance(pos, pos2);
                        pos2 = new Vector2(s2a, dst2 * mult) + pos2;
                        a2 = pos.AngleRV(pos2);
                    }
                    else if (!isSame(o, o2))
                    {
                        a2 = Start.StartPos.AngleRV(EndPos);
                    }

                    break;
                }
            }

            bool stream = false;
            float sq1 = 0, sq2 = 0;

            if (next != null)
            {
                stream = IsStream(Start, End, next) && streamRestrict;
                sq1 = Vector2.DistanceSquared(StartPos, EndPos);
                sq2 = Vector2.DistanceSquared(EndPos, next.StartPos);
            }

            float a1 = Start.BaseObject is Slider ? Start.GetEndAngle() : (first ? a2 + MathF.PI : StartPos.AngleRV(last));
            float ac = a2 - EndPos.AngleRV(StartPos);

            if (sarea > 0 && stream && anorm(ac) < anorm(2 * MathF.PI - sarea))
            {
                a = StartPos.AngleRV(EndPos);
                const float sangle = MathF.PI * 0.5f;

                if (anorm(a1 - a) > MathF.PI)
                    a2 = a - sangle;
                else
                    a2 = a + sangle;

                mult = streamMult;
            }
            else if (!fromLong && area > 0 && MathF.Abs(anorm2(ac)) < area)
            {
                a = EndPos.AngleRV(StartPos);

                if (anorm(a2 - a) < offset != restrictInvert)
                    a2 = a + restrictAngleAdd * MathF.PI / 180f;
                else
                    a2 = a - restrictAngleSub * MathF.PI / 180f;

                mult = jumpMult;
            }
            else if (next != null && !fromLong && interpolateAngles)
            {
                float r = sq1 / (sq1 + sq2);
                a = StartPos.AngleRV(EndPos);

                if (invertAngleInterpolation)
                    r = sq2 / (sq1 + sq2);

                if (!isSame(Start, End))
                    a2 = a + r * anorm2(a2 - a);

                mult = offsetMult;
            }

            bool bounce = !(End.BaseObject is IHasDuration) && isSame(Start, End, true);

            if (equalPosBounce > 0 && bounce)
            {
                a1 = StartPos.AngleRV(last);
                a2 = a1 + MathF.PI;
                distance = Vector2.Distance(last, StartPos);
                mult = equalPosBounce;
            }

            float duration = (float)(EndTime - StartTime);

            if (durationTrigger > 0 && duration >= durationTrigger)
                mult *= durationMult * (duration / durationTrigger);

            var p1 = V2FromRad(a1, distance * mult) + StartPos;
            var p2 = V2FromRad(a2, distance * mult) + EndPos;

            if (!bounce) last = p2;

            curve = new BezierCurveCubic(StartPos, EndPos, p1, p2);
            first = false;

            return 2;
        }

        private float anorm(float a)
        {
            const float pi2 = 2 * MathF.PI;
            a %= pi2;

            if (a < 0)
                a += pi2;

            return a;
        }

        private float anorm2(float a)
        {
            a = anorm(a);

            if (a > MathF.PI)
                a = -(2 * MathF.PI - a);

            return a;
        }

        public override Vector2 Update(double time) => curve.CalculatePoint(ProgressAt(time));
    }
}
