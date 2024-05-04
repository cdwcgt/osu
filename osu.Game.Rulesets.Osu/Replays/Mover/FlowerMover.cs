// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Utils;
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
                new PathControlPoint(CurrentPosition, PathType.BEZIER),
                new PathControlPoint(p1),
                new PathControlPoint(p2),
                new PathControlPoint(TargetPosition)
            });
        }

        protected override Vector2 GetPosition(double time) => path?.PositionAt((float)((time - CurrentObjectTime) / Duration)) ?? Vector2.One;

        protected override Vector2 AddHitObjectClickFrames(OsuHitObject h, OsuHitObject prev)
        {
            Vector2 startPosition = h.StackedPosition;
            Vector2 difference = startPosition - SPINNER_CENTRE;
            float radius = difference.Length;
            float angle = radius == 0 ? 0 : MathF.Atan2(difference.Y, difference.X);
            Vector2 pos = h.StackedEndPosition;
            UpdateAction(h, prev);

            switch (h)
            {
                case Slider slider:
                    AddFrameToReplay(new OsuReplayFrame(h.StartTime, h.StackedPosition, GetAction(h.StartTime)));

                    for (double j = GetFrameDelay(slider.StartTime); j < slider.Duration; j += GetFrameDelay(slider.StartTime + j))
                    {
                        pos = slider.StackedPositionAt(j / slider.Duration);
                        AddFrameToReplay(new OsuReplayFrame(h.StartTime + j, pos, GetAction(h.StartTime + j)));
                    }

                    break;

                case Spinner spinner:
                    double rEndTime = spinner.StartTime + spinner.Duration * 0.7;
                    double previousFrame = h.StartTime;
                    double delay;

                    for (double nextFrame = h.StartTime + GetFrameDelay(h.StartTime); nextFrame < spinner.EndTime; nextFrame += delay)
                    {
                        delay = GetFrameDelay(previousFrame);
                        double t = ApplyModsToTimeDelta(previousFrame, nextFrame) * -1;
                        angle += (float)t / 20;
                        double r = nextFrame > rEndTime ? 50 : Interpolation.ValueAt(nextFrame, 50, 50, spinner.StartTime, rEndTime, Easing.In);
                        pos = SPINNER_CENTRE + CirclePosition(angle, r);
                        AddOffSetFrame(new OsuReplayFrame((int)nextFrame, pos, GetAction(nextFrame)), 0);

                        previousFrame = nextFrame;
                    }

                    break;

                default:
                    AddOffSetFrame(new OsuReplayFrame(h.StartTime, GetPosition(h.StartTime), GetAction(h.StartTime)), 0);
                    break;
            }

            return pos;
        }
    }
}
