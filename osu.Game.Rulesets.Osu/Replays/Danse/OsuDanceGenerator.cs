// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Replays;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Replays.Danse.Movers;
using osu.Game.Rulesets.Osu.Replays.Danse.Movers.Spinners;
using osu.Game.Rulesets.Osu.Replays.Danse.Objects;
using osu.Game.Rulesets.Osu.UI;
using osuTK;
using static osu.Game.Configuration.OsuDanceMover;

// Credit to danser-go https://github.com/Wieku/danser-go
namespace osu.Game.Rulesets.Osu.Replays.Danse
{
    public class OsuDanceGenerator : OsuAutoGeneratorBase
    {
        public static Mover GetMover(OsuDanceMover mover) =>
            mover switch
            {
                AxisAligned => new AxisAlignedMover(),
                Aggresive => new AggressiveMover(),
                Bezier => new BezierMover(),
                HalfCircle => new HalfCircleMover(),
                Flower => new FlowerMover(),
                Pippi => new PippiMover(),
                Linear => new LinearMover(),
                _ => new MomentumMover()
            };

        public static SpinnerMover GetSpinnerMover(OsuDanceSpinnerMover mover, double startTime, double endTime, float spinRadiusStart, float spinRadiusEnd) =>
            mover switch
            {
                OsuDanceSpinnerMover.Pippi => new PippiSpinnerMover(startTime, endTime, spinRadiusStart, spinRadiusEnd),
                OsuDanceSpinnerMover.Heart => new HeartSpinnerMover(startTime, endTime, spinRadiusStart, spinRadiusEnd),
                OsuDanceSpinnerMover.Square => new SquareSpinnerMover(startTime, endTime, spinRadiusStart, spinRadiusEnd),
                OsuDanceSpinnerMover.Triangle => new TriangleSpinnerMover(startTime, endTime, spinRadiusStart, spinRadiusEnd),
                OsuDanceSpinnerMover.Cube => new CubeSpinnerMover(startTime, endTime, spinRadiusStart, spinRadiusEnd),
                _ => new CircleSpinnerMover(startTime, endTime, spinRadiusStart, spinRadiusEnd)
            };

        public new OsuBeatmap Beatmap => (OsuBeatmap)base.Beatmap;
        private readonly Mover mover;
        private readonly OsuDanceSpinnerMover spinnerMover;
        private readonly float spinRadiusStart;
        private readonly float spinRadiusEnd;
        private readonly bool sliderDance;
        private readonly bool spinnerChangeFramerate;
        private readonly bool borderBounce;
        private readonly double normalFrameDelay;
        private double frameDelay;
        private InputProcessor input = null!;
        private List<DanceHitObject> hitObjects = null!;

        public OsuDanceGenerator(IBeatmap beatmap, IReadOnlyList<Mod> mods)
            : base(beatmap, mods)
        {
            var config = CustomConfigManager.Instance;
            mover = GetMover(config.Get<OsuDanceMover>(CustomSetting.DanceMover));
            spinnerMover = config.Get<OsuDanceSpinnerMover>(CustomSetting.DanceSpinnerMover);
            borderBounce = config.Get<bool>(CustomSetting.BorderBounce);
            frameDelay = normalFrameDelay = ApplyModsToRate(0, 1000.0 / config.Get<double>(CustomSetting.ReplayFramerate));
            spinRadiusStart = config.Get<float>(CustomSetting.SpinnerRadiusStart);
            spinRadiusEnd = config.Get<float>(CustomSetting.SpinnerRadiusEnd);
            sliderDance = config.Get<bool>(CustomSetting.SliderDance);
            spinnerChangeFramerate = config.Get<bool>(CustomSetting.SpinnerChangeFramerate);
            mover.TimeAffectingMods = mods.OfType<IApplicableToRate>().ToList();
            preProcessObjects();
        }

        private void preProcessObjects()
        {
            hitObjects = Beatmap.HitObjects.Where(h => h is not Spinner { SpinsRequired: 0 }).Select(h =>
            {
                switch (h)
                {
                    case Spinner spinner:
                        return new DanceSpinner(spinner, GetSpinnerMover(spinnerMover, spinner.StartTime, spinner.EndTime, spinRadiusStart, spinRadiusEnd));

                    default:
                        return new DanceHitObject(h);
                }
            }).ToList();

            for (int i = 0; i < hitObjects.Count; i++)
            {
                var h = hitObjects[i].BaseObject;

                if (h is Slider s)
                {
                    bool found = false;

                    // Resolving 2B conflicts
                    for (int j = i - 1; j >= 0; j--)
                    {
                        var o = hitObjects[i - 1];

                        if (o.EndTime >= h.StartTime)
                        {
                            found = true;
                            replaceSlider(i, ref hitObjects);

                            break;
                        }
                    }

                    if (!found && i + 1 < hitObjects.Count)
                    {
                        var o = hitObjects[i + 1];

                        if (o.StartTime <= s.EndTime)
                        {
                            replaceSlider(i, ref hitObjects);
                        }
                    }
                }
            }

            // Second 2B pass for spinners
            for (int i = 0; i < hitObjects.Count; i++)
            {
                if (hitObjects[i] is DanceSpinner s)
                {
                    var subSpinners = new List<DanceSpinner>();
                    double startTime = s.StartTime;

                    for (int j = i + 1; j < hitObjects.Count; j++)
                    {
                        var o = hitObjects[j];

                        if (o.StartTime - frameDelay >= s.EndTime) break;

                        double endTime = o.StartTime - frameDelay;

                        if (endTime > startTime)
                        {
                            subSpinners.Add(new DanceSpinner(new Spinner { StartTime = startTime, EndTime = endTime }, GetSpinnerMover(spinnerMover, startTime, endTime, s.Mover.RadiusAt(startTime), s.Mover.RadiusAt(endTime))));
                        }

                        startTime = o.EndTime + frameDelay;
                    }

                    if (subSpinners.Count > 0)
                    {
                        if (s.EndTime > startTime)
                        {
                            subSpinners.Add(new DanceSpinner(new Spinner { StartTime = startTime, EndTime = s.EndTime }, GetSpinnerMover(spinnerMover, startTime, s.EndTime, s.Mover.RadiusAt(startTime), s.Mover.RadiusAt(s.EndTime))));
                        }

                        hitObjects.RemoveAt(i);
                        hitObjects.InsertRange(i, subSpinners);
                        hitObjects = hitObjects.OrderBy(h => h.StartTime).ToList();
                    }
                }
            }

            for (int i = 0; i < hitObjects.Count; i++)
            {
                var h = hitObjects[i];

                switch (h.BaseObject)
                {
                    case Slider slider:
                    {
                        if (slider.IsRetarded() || sliderDance)
                        {
                            replaceSlider(i, ref hitObjects);
                        }

                        break;
                    }
                }
            }

            for (int i = 0; i < hitObjects.Count - 1; i++)
            {
                var current = hitObjects[i];
                var next = hitObjects[i + 1];

                if (current.BaseObject is HitCircle circle && next.BaseObject is HitCircle &&
                    (!current.SliderPoint || current.SliderPointStart) && (!next.SliderPoint || next.SliderPointStart))
                {
                    float dst = (next.StartPos - current.EndPos).Length;

                    if (dst <= circle.Radius * 1.995 && next.StartTime - current.EndTime <= Math.Max(frameDelay, 3))
                    {
                        double sTime = (next.StartTime + current.EndTime) / 2;
                        current.DoubleClick = true;
                        current.StartTime = current.EndTime = sTime;
                        current.StartPos = current.EndPos = (current.EndPos + next.StartPos) * 0.5f;
                        hitObjects.RemoveAt(i + 1);
                    }
                }
            }

            // Split circles that are too close in time
            for (int i = 0; i < hitObjects.Count; i++)
            {
                var current = hitObjects[i];

                for (int j = i + 1; j < hitObjects.Count; j++)
                {
                    var o = hitObjects[j];

                    if (current.EndTime + frameDelay < o.StartTime) break;

                    if (!o.SliderPoint || o.SliderPointStart)
                    {
                        // The minimum time we can delay is one frame
                        hitObjects[j].StartTime += frameDelay;
                    }
                }
            }

            for (int i = 0; i < hitObjects.Count; i++)
            {
                var h = hitObjects[i];

                if (h is not DanceSpinner spinner)
                    continue;

                /*if (i > 0)
                {
                    var prev = hitObjects[i - 1];
                    Vector2 startPos;
                    calcSpinnerStartPos(prev.EndPos, spinner.Mover.RadiusAt(spinner.StartTime), out startPos);
                    Vector2 difference = startPos - SPINNER_CENTRE;

                    float radius = difference.Length;
                    spinner.Mover.StartAngle = radius == 0 ? 0 : MathF.Atan2(difference.Y, difference.X) * 2f * MathF.PI;
                }*/

                spinner.StartPos = spinner.PositionAt(spinner.StartTime);
                spinner.EndPos = spinner.PositionAt(spinner.EndTime);
            }

            hitObjects = hitObjects.OrderBy(h => h.StartTime).ToList();
            input = new InputProcessor(hitObjects.ToList(), frameDelay, ApplyModsToTimeDelta);
            hitObjects.Insert(0, new DanceHitObject(new HitCircle { Position = hitObjects[0].StartPos, StartTime = hitObjects[0].StartTime - 500 }));
            int toRemove = mover.SetObjects(hitObjects) - 1;
            hitObjects = hitObjects[toRemove..];
        }

        private static void calcSpinnerStartPos(Vector2 prevPos, float radius, out Vector2 startPosition)
        {
            Vector2 spinCentreOffset = SPINNER_CENTRE - prevPos;
            float distFromCentre = spinCentreOffset.Length;
            float distToTangentPoint = MathF.Sqrt(distFromCentre * distFromCentre - radius * radius);

            if (distFromCentre > radius)
            {
                // Previous cursor position was outside spin circle, set startPosition to the tangent point.

                // Angle between centre offset and tangent point offset.
                float angle = MathF.Asin(radius / distFromCentre);

                // Rotate by angle so it's parallel to tangent line
                spinCentreOffset.X = spinCentreOffset.X * MathF.Cos(angle) - spinCentreOffset.Y * MathF.Sin(angle);
                spinCentreOffset.Y = spinCentreOffset.X * MathF.Sin(angle) + spinCentreOffset.Y * MathF.Cos(angle);

                // Set length to distToTangentPoint
                spinCentreOffset.Normalize();
                spinCentreOffset *= distToTangentPoint;

                // Move along the tangent line, now startPosition is at the tangent point.
                startPosition = prevPos + spinCentreOffset;
            }
            else if (spinCentreOffset.Length > 0)
            {
                // Previous cursor position was inside spin circle, set startPosition to the nearest point on spin circle.
                startPosition = SPINNER_CENTRE - spinCentreOffset * (radius / spinCentreOffset.Length);
            }
            else
            {
                // Degenerate case where cursor position is exactly at the centre of the spin circle.
                startPosition = SPINNER_CENTRE + new Vector2(0, -radius);
            }
        }

        private static void replaceSlider(int index, ref List<DanceHitObject> queue)
        {
            if (queue[index].BaseObject is not Slider s)
                return;

            queue.RemoveAt(index);

            if (s.IsRetarded())
            {
                queue.Insert(index, new DanceHitObject(new HitCircle { Position = s.Position, StartTime = s.StartTime, StackHeight = s.StackHeight }) { SliderPoint = true, SliderPointStart = true });
                return;
            }

            var p = s.NestedHitObjects.Cast<OsuHitObject>().Select(h =>
            {
                var d = new DanceHitObject(new HitCircle { Position = h.Position, StartTime = h.StartTime, StackHeight = h.StackHeight })
                {
                    SliderPoint = true
                };

                switch (h)
                {
                    case SliderHeadCircle:
                        d.SliderPointStart = true;
                        break;

                    case SliderTailCircle:
                        double t = Math.Max(s.StartTime + s.Duration / 2, s.EndTime + SliderEventGenerator.TAIL_LENIENCY);
                        d.StartTime = d.EndTime = t;
                        d.StartPos = d.EndPos = s.StackedPositionAt((t - s.StartTime) / s.Duration);
                        d.SliderPointEnd = true;
                        break;
                }

                return d;
            });
            queue.InsertRange(index, p);
            queue = queue.OrderBy(h => h.StartTime).ToList();
        }

        public override Replay Generate()
        {
            double lastTime = 0;
            var baseSize = OsuPlayfield.BASE_SIZE;

            float xf = baseSize.X / 0.8f * (4f / 3f);
            float x0 = (baseSize.X - xf) / 2f;
            float x1 = xf + x0;

            float yf = baseSize.Y / 0.8f;
            float y0 = (baseSize.Y - yf) / 2f;
            float y1 = yf + y0;

            double endTime = hitObjects[^1].EndTime + frameDelay * 5;

            for (double time = hitObjects[0].StartTime; time <= endTime; time += frameDelay)
            {
                double lastEndTime = 0;
                OsuAction[] action = input.Update(time);

                for (int i = 0; i < hitObjects.Count; i++)
                {
                    var h = hitObjects[i];

                    if (h.StartTime > time)
                        break;

                    lastEndTime = Math.Max(lastEndTime, h.EndTime);

                    if (lastTime <= h.StartTime || time <= h.EndTime)
                    {
                        frameDelay = normalFrameDelay;

                        switch (h.BaseObject)
                        {
                            case Spinner:
                                frameDelay = spinnerChangeFramerate ? normalFrameDelay : GetFrameDelay(time);
                                AddFrameToReplay(new OsuReplayFrame(time, mover.GetObjectPosition(time, h), action));
                                break;

                            default:
                                AddFrameToReplay(new OsuReplayFrame(time, mover.GetObjectPosition(time, h), action));
                                break;
                        }
                    }

                    if (time > h.EndTime)
                    {
                        // Ensure object is hit on time
                        AddFrameToReplay(new OsuReplayFrame(h.StartTime, mover.GetObjectPosition(h.StartTime, h), input.Update(h.StartTime)));
                        int upperLimit = hitObjects.Count;

                        for (int j = i; j < hitObjects.Count; j++)
                        {
                            if (hitObjects[j].EndTime >= lastEndTime)
                                break;

                            upperLimit = j + 1;
                        }

                        int toRemove = 1;

                        if (upperLimit - i > 1)
                        {
                            toRemove = mover.SetObjects(hitObjects[i..upperLimit]) - 1;
                        }

                        hitObjects.RemoveRange(i, toRemove);
                        i--;
                    }
                }

                lastTime = time;

                if (mover.EndTime >= time)
                {
                    var pos = mover.Update(time);

                    if (borderBounce)
                    {
                        if (pos.X < x0) pos.X = x0 - (pos.X - x0);
                        if (pos.Y < y0) pos.Y = y0 - (pos.Y - y0);

                        if (pos.X > x1)
                        {
                            float x = pos.X - x0;
                            int m = (int)(x / xf);
                            x %= xf;
                            x = m % 2 == 0 ? x : xf - x;
                            pos.X = x + x0;
                        }

                        if (pos.Y > y1)
                        {
                            float y = pos.Y - y0;
                            float m = (int)(y / yf);
                            y %= yf;
                            y = m % 2 == 0 ? y : yf - y;
                            pos.Y = y + y0;
                        }
                    }

                    AddFrameToReplay(new OsuReplayFrame(time, pos, action));
                }
            }

            var lastFrame = (OsuReplayFrame)Frames[^1];
            var newLastFrame = new OsuReplayFrame(lastFrame.Time + KEY_UP_DELAY, lastFrame.Position);
            AddFrameToReplay(newLastFrame);
            return Replay;
        }
    }
}
