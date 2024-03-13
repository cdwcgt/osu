// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Replays;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Replays.Danse.Movers;
using osu.Game.Rulesets.Osu.UI;
using osuTK;
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

        public new OsuBeatmap Beatmap => (OsuBeatmap)base.Beatmap;
        private readonly Mover mover;
        private readonly float spinRadiusStart;
        private readonly float spinRadiusEnd;
        private readonly bool sliderDance;
        private readonly bool pippiSpinner;
        private readonly bool pippiStream;
        private readonly bool skipShortSliders;
        private readonly bool spinnerChangeFramerate;
        //private bool isStream = false;
        private readonly MConfigManager config;
        private readonly double frameDelay;
        private int buttonIndex;
        private List<DanceHitObject> hitObjects = null!;
        private readonly bool isPippi;
        private readonly double[] keyUpTime = { -20000, -20000 };

        public OsuDanceGenerator(IBeatmap beatmap, IReadOnlyList<Mod> mods)
            : base(beatmap, mods)
        {
            config = MConfigManager.Instance;
            mover = GetMover(config.Get<OsuDanceMover>(MSetting.DanceMover));
            isPippi = mover is PippiMover;
            frameDelay = 1000.0 / config.Get<float>(MSetting.ReplayFramerate);
            spinRadiusStart = config.Get<float>(MSetting.SpinnerRadiusStart);
            spinRadiusEnd = config.Get<float>(MSetting.SpinnerRadiusEnd);
            sliderDance = config.Get<bool>(MSetting.SliderDance);
            pippiSpinner = config.Get<bool>(MSetting.PippiSpinner) || isPippi;
            pippiStream = config.Get<bool>(MSetting.PippiStream);
            skipShortSliders = config.Get<bool>(MSetting.SkipShortSlider);
            spinnerChangeFramerate = config.Get<bool>(MSetting.SpinnerChangeFramerate);
            mover.TimeAffectingMods = mods.OfType<IApplicableToRate>().ToList();
            preProcessObjects();
        }

        private void preProcessObjects()
        {
            hitObjects = Beatmap.HitObjects.SkipWhile(h => h is Spinner { SpinsRequired: 0 }).Select(h => new DanceHitObject(h)).ToList();

            for (int i = 0; i < hitObjects.Count; i++)
            {
                if (hitObjects[i].BaseObject is Slider slider)
                {
                    if (slider.IsRetarded())
                    {
                        replaceSlider(i, hitObjects);
                    }
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

            // Because we go from Start.EndPos to End.StartPos, create a dummy hit object to avoid missing the first object
            hitObjects.Insert(0, new DanceHitObject(new HitCircle { Position = hitObjects[0].StartPos, StartTime = -10000 }));
        }

        private void replaceSlider(int index, List<DanceHitObject> queue)
        {
            if (queue[index].BaseObject is Slider s)
            {
                queue.RemoveAt(index);
                queue.InsertRange(index, s.NestedHitObjects.Cast<OsuHitObject>().Select(h => new DanceHitObject(h)));

                queue = queue.OrderBy(h => h.StartTime).ToList();
            }
        }

        private void updateAction(DanceHitObject h, DanceHitObject last)
        {
            double timeDifference = ApplyModsToTimeDelta(last.EndTime, h.StartTime);

            if (timeDifference > 0 && timeDifference < 266)
                buttonIndex++;
            else
                buttonIndex = 0;

            var action = buttonIndex % 2 == 0 ? OsuAction.LeftButton : OsuAction.RightButton;
            keyUpTime[(int)action] = h.EndTime + KEY_UP_DELAY;
        }

        private OsuAction[] getAction(double time)
        {
            var actions = new List<OsuAction>(2);

            if (time <= keyUpTime[0])
                actions.Add(OsuAction.LeftButton);
            if (time <= keyUpTime[1])
                actions.Add(OsuAction.RightButton);

            if (actions.Count == 2)
            {
                var lastAction = (buttonIndex - 1) % 2 == 0 ? OsuAction.LeftButton : OsuAction.RightButton;
                keyUpTime[(int)lastAction] = time;
            }

            return actions.ToArray();
        }

        private void addHitObjectClickFrames(DanceHitObject h, DanceHitObject prev)
        {
            Vector2 startPosition = h.StartPos;
            Vector2 difference = startPosition - SPINNER_CENTRE;
            float radius = difference.Length;
            float angle = radius == 0 ? 0 : MathF.Atan2(difference.Y, difference.X);
            Vector2 pos;
            updateAction(h, prev);

            switch (h.BaseObject)
            {
                case Slider slider:
                    AddFrameToReplay(new OsuReplayFrame(h.StartTime, h.StartPos, getAction(h.StartTime)));

                    var points = slider.NestedHitObjects.SkipWhile(p => p is SliderRepeat).Cast<OsuHitObject>()
                                       .OrderBy(p => p.StartTime)
                                       .ToList();

                    var mid = h.EndPos;

                    if (skipShortSliders && Math.Abs(Vector2.Distance(startPosition, mid)) <= slider.Radius * 1.6)
                    {
                        mid = slider.Path.PositionAt(1);

                        if (slider.RepeatCount == 1 || Vector2.Distance(startPosition, mid) <= slider.Radius * 1.6)
                        {
                            mid = slider.Path.PositionAt(0.5);

                            if (Vector2.Distance(startPosition, mid) <= slider.Radius * 1.6)
                            {
                                AddFrameToReplay(new OsuReplayFrame(h.EndTime, mid, getAction(h.EndTime)));
                                h.EndPos = mid;
                                return;
                            }
                        }
                    }

                    if (sliderDance && points.Count > 2)
                    {
                        for (int i = 0; i < points.Count - 1; i++)
                        {
                            var point = points[i];
                            var next = points[i + 1];
                            double duration = next.StartTime - point.StartTime;

                            if (i == points.Count - 2)
                                duration += 36;

                            for (double j = GetFrameDelay(point.StartTime); j < duration; j += GetFrameDelay(point.StartTime + j))
                            {
                                double scaleFactor = j / duration;
                                pos = point.StackedPosition + (next.StackedPosition - point.StackedPosition) * (float)scaleFactor;

                                AddFrameToReplay(new OsuReplayFrame(point.StartTime + j, pos, getAction(point.StartTime + j)));
                            }
                        }
                    }
                    else
                    {
                        for (double j = GetFrameDelay(slider.StartTime); j < slider.Duration; j += GetFrameDelay(slider.StartTime + j))
                        {
                            pos = slider.StackedPositionAt(j / slider.Duration);
                            AddFrameToReplay(new OsuReplayFrame(h.StartTime + j, pos, getAction(h.StartTime + j)));
                        }
                    }

                    break;

                case Spinner spinner:
                    double radiusStart = spinner.SpinsRequired > 3 ? spinRadiusStart : spinRadiusEnd;
                    double rEndTime = spinner.StartTime + spinner.Duration * 0.7;
                    double previousFrame = h.StartTime;
                    double delay;

                    for (double nextFrame = h.StartTime + GetFrameDelay(h.StartTime); nextFrame < spinner.EndTime; nextFrame += delay)
                    {
                        delay = spinnerChangeFramerate ? ApplyModsToRate(nextFrame, frameDelay) : GetFrameDelay(previousFrame);
                        double t = ApplyModsToTimeDelta(previousFrame, nextFrame) * -1;
                        angle += (float)t / 20;
                        double r = nextFrame > rEndTime ? spinRadiusEnd : Interpolation.ValueAt(nextFrame, radiusStart, spinRadiusEnd, spinner.StartTime, rEndTime, Easing.In);
                        pos = SPINNER_CENTRE + CirclePosition(angle, r);
                        AddFrameToReplay(new OsuReplayFrame((int)nextFrame, pos, getAction(nextFrame)));

                        previousFrame = nextFrame;
                    }

                    break;

                default:
                    AddFrameToReplay(new OsuReplayFrame(h.StartTime, h.StartPos, getAction(h.StartTime)));
                    break;
            }
        }

        public override Replay Generate()
        {
            var h = hitObjects[0];

            AddFrameToReplay(new OsuReplayFrame(-10000, h.StartPos));

            Vector2 baseSize = OsuPlayfield.BASE_SIZE;

            float xf = baseSize.X / 0.8f * (4f / 3f);
            float x0 = (baseSize.X - xf) / 2f;
            float x1 = xf + x0;

            float yf = baseSize.Y / 0.8f;
            float y0 = (baseSize.Y - yf) / 2f;
            float y1 = yf + y0;

            mover.SetObjects(hitObjects);

            for (int i = 0; i < hitObjects.Count; i++)
            {
                var prev = h;
                h = hitObjects[i];

                if (i > 1)
                {
                    // HACK: mover moves from hitObject[i - 1] to hitObject[i], so input has to be hitObject[i - 2] to hitObject[i - 1]
                    var prevPrev = hitObjects[i - 2];
                    addHitObjectClickFrames(prev, prevPrev);
                }

                for (double time = mover.StartTime; time < mover.EndTime; time += frameDelay)
                {
                    var currentPosition = mover.Update(time);

                    if (config.Get<bool>(MSetting.BorderBounce))
                    {
                        if (currentPosition.X < x0) currentPosition.X = x0 - (currentPosition.X - x0);
                        if (currentPosition.Y < y0) currentPosition.Y = y0 - (currentPosition.Y - y0);

                        if (currentPosition.X > x1)
                        {
                            float x = currentPosition.X - x0;
                            int m = (int)(x / xf);
                            x %= xf;
                            x = m % 2 == 0 ? x : xf - x;
                            currentPosition.X = x + x0;
                        }

                        if (currentPosition.Y > y1)
                        {
                            float y = currentPosition.Y - y0;
                            float m = (int)(y / yf);
                            y %= yf;
                            y = m % 2 == 0 ? y : yf - y;
                            currentPosition.Y = y + y0;
                        }
                    }

                    AddFrameToReplay(new OsuReplayFrame(time, currentPosition, getAction(time)));
                }

                mover.SetObjects(hitObjects.GetRange(i, hitObjects.Count - i));
            }

            addHitObjectClickFrames(hitObjects[^1], hitObjects[^2]);
            var lastFrame = (OsuReplayFrame)Frames[^1];
            var newLastFrame = new OsuReplayFrame(lastFrame.Time + 50, lastFrame.Position);
            AddFrameToReplay(newLastFrame);

            return Replay;
        }
    }
}
