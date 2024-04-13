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

        public static SpinnerMover GetSpinnerMover(OsuDanceSpinnerMover mover, Spinner spinner, float spinRadiusStart, float spinRadiusEnd) =>
            mover switch
            {
                OsuDanceSpinnerMover.Pippi => new PippiSpinnerMover(spinner, spinRadiusStart, spinRadiusEnd),
                OsuDanceSpinnerMover.Heart => new HeartSpinnerMover(spinner, spinRadiusStart, spinRadiusEnd),
                OsuDanceSpinnerMover.Cube => new CubeSpinnerMover(spinner, spinRadiusStart, spinRadiusEnd),
                _ => new CircleSpinnerMover(spinner, spinRadiusStart, spinRadiusEnd)
            };

        public new OsuBeatmap Beatmap => (OsuBeatmap)base.Beatmap;
        private readonly Mover mover;
        private readonly OsuDanceSpinnerMover spinnerMover;
        private readonly float spinRadiusStart;
        private readonly float spinRadiusEnd;
        private readonly bool sliderDance;
        private readonly bool skipShortSliders;
        private readonly bool spinnerChangeFramerate;
        private readonly bool borderBounce;
        private readonly MConfigManager config;
        private readonly double normalFrameDelay;
        private double frameDelay;
        private InputProcessor input = null!;
        private List<DanceHitObject> hitObjects = null!;

        public OsuDanceGenerator(IBeatmap beatmap, IReadOnlyList<Mod> mods)
            : base(beatmap, mods)
        {
            config = MConfigManager.Instance;
            mover = GetMover(config.Get<OsuDanceMover>(MSetting.DanceMover));
            spinnerMover = config.Get<OsuDanceSpinnerMover>(MSetting.DanceSpinnerMover);
            borderBounce = config.Get<bool>(MSetting.BorderBounce);
            frameDelay = normalFrameDelay = 1000.0 / config.Get<double>(MSetting.ReplayFramerate);
            spinRadiusStart = config.Get<float>(MSetting.SpinnerRadiusStart);
            spinRadiusEnd = config.Get<float>(MSetting.SpinnerRadiusEnd);
            sliderDance = config.Get<bool>(MSetting.SliderDance);
            skipShortSliders = config.Get<bool>(MSetting.SkipShortSlider);
            spinnerChangeFramerate = config.Get<bool>(MSetting.SpinnerChangeFramerate);
            mover.TimeAffectingMods = mods.OfType<IApplicableToRate>().ToList();
            preProcessObjects();
        }

        private void preProcessObjects()
        {
            hitObjects = Beatmap.HitObjects.SkipWhile(h => h is Spinner { SpinsRequired: 0 }).Select(h =>
            {
                switch (h)
                {
                    case Spinner spinner:
                        return new DanceSpinner(spinner, GetSpinnerMover(spinnerMover, spinner, spinRadiusStart, spinRadiusEnd));

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
                if (hitObjects[i].BaseObject is Spinner s)
                {
                    var subSpinners = new List<Spinner>();
                    double startTime = s.StartTime;

                    for (int j = i + 1; j < hitObjects.Count; j++)
                    {
                        var o = hitObjects[j];

                        if (o.StartTime >= s.EndTime) break;

                        double endTime = o.StartTime - 30;

                        if (endTime > startTime)
                        {
                            subSpinners.Add(new Spinner { StartTime = startTime, EndTime = endTime });
                        }

                        startTime = o.EndTime + 30;
                    }

                    if (subSpinners.Count > 0)
                    {
                        if (s.EndTime > startTime)
                        {
                            subSpinners.Add(new Spinner { StartTime = startTime, EndTime = s.EndTime });
                        }

                        hitObjects.RemoveAt(i);
                        hitObjects.InsertRange(i, subSpinners.Select(h => new DanceSpinner(h, GetSpinnerMover(spinnerMover, h, spinRadiusStart, spinRadiusEnd))));

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

                    case Spinner:
                        h.StartPos = h.PositionAt(h.StartTime);

                        if (i > 0)
                        {
                            var last = hitObjects[i - 1];
                            if (last.BaseObject is Spinner)
                                h.StartPos = last.EndPos;
                        }

                        h.EndPos = h.PositionAt(h.EndTime);
                        break;

                    default:
                        h.StartPos = h.PositionAt(h.StartTime);
                        break;
                }
            }

            for (int i = 0; i < hitObjects.Count - 1; i++)
            {
                var current = hitObjects[i];
                var next = hitObjects[i + 1];

                if (current.BaseObject is HitCircle circle && next.BaseObject is HitCircle &&
                    (!current.SliderPoint || current.SliderPointStart) && (!next.SliderPoint || next.SliderPointStart))
                {
                    float dst = (current.EndPos - next.StartPos).LengthSquared;

                    if (dst <= circle.Radius * 1.995 && next.StartTime - current.EndTime <= 3)
                    {
                        double sTime = (next.StartTime + current.EndTime) / 2;
                        current.DoubleClick = true;
                        current.StartTime = sTime;
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
                        hitObjects[j].StartTime += frameDelay;
                    }
                }
            }

            input = new InputProcessor(ApplyModsToTimeDelta, hitObjects.ToList());
            hitObjects.Insert(0, new DanceHitObject(new HitCircle { Position = hitObjects[0].StartPos, StartTime = -500 }));
            int toRemove = mover.SetObjects(hitObjects) - 1;
            hitObjects = hitObjects[toRemove..];
        }

        private void replaceSlider(int index, ref List<DanceHitObject> queue)
        {
            if (queue[index].BaseObject is Slider s)
            {
                queue.RemoveAt(index);

                if (s.IsRetarded())
                {
                    queue.Insert(index, new DanceHitObject(new HitCircle { Position = s.Position, StartTime = s.StartTime, StackHeight = s.StackHeight }) { SliderPoint = true, SliderPointStart = true});
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
