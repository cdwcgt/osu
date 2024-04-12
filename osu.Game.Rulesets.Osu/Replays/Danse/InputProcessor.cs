// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Rulesets.Osu.Replays.Danse.Objects;
using osu.Game.Rulesets.Replays;

namespace osu.Game.Rulesets.Osu.Replays.Danse
{
    public class InputProcessor
    {
        private bool wasLeftBefore;
        private double lastTime = double.NegativeInfinity;
        private double previousEnd = double.NegativeInfinity;
        private readonly List<DanceHitObject> hitObjects;
        private readonly Func<double, double, double> applyModsToTimeDelta;
        private readonly double[] keyUpTime = { double.NegativeInfinity, double.NegativeInfinity };

        public InputProcessor(Func<double, double, double> applyModsToTimeDelta, List<DanceHitObject> hitObjects)
        {
            this.applyModsToTimeDelta = applyModsToTimeDelta;
            this.hitObjects = hitObjects;
        }

        public OsuAction[] Update(double time)
        {
            var actions = new List<OsuAction>(2);

            for (int i = 0; i < hitObjects.Count; i++)
            {
                var h = hitObjects[i];
                bool isDoubleClick = h.DoubleClick;

                if (h.StartTime > time)
                    break;

                if (lastTime < h.StartTime && time >= h.StartTime)
                {
                    double endTime = h.EndTime;
                    double releaseAt = endTime + AutoGenerator.KEY_UP_DELAY;

                    if (i + 1 < hitObjects.Count)
                    {
                        int j = i + 1;

                        for (; j < hitObjects.Count; j++)
                        {
                            var c = hitObjects[j];

                            if (c.SliderPoint && !c.SliderPointStart)
                            {
                                endTime = c.EndTime;
                                releaseAt = endTime + AutoGenerator.KEY_UP_DELAY;
                            }
                            else
                                break;
                        }

                        if (j > i + 1)
                            hitObjects.RemoveRange(i + 1, j - (i + 1));

                        if (i + 1 < hitObjects.Count)
                        {
                            DanceHitObject? obj = null;

                            if (isDoubleClick || hitObjects[i + 1].DoubleClick)
                            {
                                obj = hitObjects[i + 1];
                            }
                            else if (i + 2 < hitObjects.Count)
                            {
                                obj = hitObjects[i + 2];
                            }

                            if (obj != null)
                            {
                                double nTime = obj.StartTime;
                                releaseAt = Math.Clamp(nTime - 1, endTime + 1, releaseAt);
                            }
                        }
                    }

                    double timeDifference = applyModsToTimeDelta(previousEnd, h.StartTime);
                    bool shouldBeLeft = !wasLeftBefore && timeDifference < 266;

                    if (isDoubleClick)
                    {
                        keyUpTime[0] = keyUpTime[1] = releaseAt;
                    }
                    else
                    {
                        int action = (int)(shouldBeLeft ? OsuAction.LeftButton : OsuAction.RightButton);
                        keyUpTime[action] = releaseAt;
                    }

                    wasLeftBefore = shouldBeLeft;
                    previousEnd = endTime;
                    hitObjects.RemoveAt(i);
                    i--;
                }
            }

            if (time < keyUpTime[0])
                actions.Add(OsuAction.LeftButton);
            if (time < keyUpTime[1])
                actions.Add(OsuAction.RightButton);

            lastTime = time;
            return actions.ToArray();
        }
    }
}
