// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Replays;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Replays.Danse.Movers;
using osu.Game.Rulesets.Osu.Replays.Danse.Movers.Spinners;
using osu.Game.Rulesets.Osu.Replays.Danse.Objects;
using osu.Game.Rulesets.Osu.UI;
using static osu.Game.Configuration.OsuDanceMover;

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
                var h = hitObjects[i];

                switch (h.BaseObject)
                {
                    case Slider slider:
                    {
                        if (slider.IsRetarded())
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
                }
            }

            /*
            for (int i = 0; i < hitObjects.Count; i++)
            {
                var h = hitObjects[i].BaseObject;

                bool found = false;

                if (h is Slider)
                {
                    // Resolving 2B conflicts
                    for (int j = i - 1; j >= 0; j--)
                    {
                        var o = hitObjects[Math.Max(0, i - 1)].BaseObject;

                        if (o.GetEndTime() >= h.StartTime)
                        {
                            found = true;
                            replaceSlider(i, hitObjects);

                            break;
                        }
                    }

                    if (!found && i + 1 < hitObjects.Count)
                    {
                        var o = hitObjects[Math.Min(hitObjects.Count, i + 1)];

                        if (o.StartTime <= h.GetEndTime())
                        {
                            replaceSlider(i, hitObjects);
                        }
                    }
                }
            }*/

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
                        hitObjects.InsertRange(i, subSpinners.Select(h => new DanceHitObject(h)));

                        hitObjects = hitObjects.OrderBy(h => h.StartTime).ToList();
                    }
                }
            }

            // Split circles that are too close in time
            for (int i = 0; i < hitObjects.Count; i++)
            {
                var current = hitObjects[i];

                for (int j = i + 1; j < hitObjects.Count; j++)
                {
                    var o = hitObjects[j].BaseObject;

                    if (current.EndTime < hitObjects[j].StartTime) break;

                    if (o is HitCircle || o is Slider)
                    {
                        hitObjects[j].StartTime++;
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
                queue.InsertRange(index, s.NestedHitObjects.Cast<OsuHitObject>().Select(h => new DanceHitObject(h)));

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

            double endTime = hitObjects[^1].EndTime;

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
                            case Slider slider:
                                AddFrameToReplay(new OsuReplayFrame(time, slider.StackedPositionAt((time - h.StartTime) / slider.Duration), action));
                                break;

                            case Spinner:
                                frameDelay = spinnerChangeFramerate ? normalFrameDelay : GetFrameDelay(time);
                                AddFrameToReplay(new OsuReplayFrame(time, h.PositionAt(time), action));
                                break;

                            default:
                                AddFrameToReplay(new OsuReplayFrame(time, mover.Update(time), action));
                                break;
                        }
                    }

                    if (time > h.EndTime)
                    {
                        AddFrameToReplay(new OsuReplayFrame(h.StartTime, mover.Update(h.StartTime), input.Update(h.StartTime)));
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
