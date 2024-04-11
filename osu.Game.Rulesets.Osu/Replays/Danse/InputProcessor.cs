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

                if (h.StartTime > time)
                    break;

                if (lastTime < h.StartTime && time >= h.StartTime)
                {
                    double timeDifference = applyModsToTimeDelta(previousEnd, h.StartTime);
                    bool shouldBeLeft = !wasLeftBefore && timeDifference < 266;

                    var action = shouldBeLeft ? OsuAction.LeftButton : OsuAction.RightButton;
                    keyUpTime[(int)action] = h.EndTime + AutoGenerator.KEY_UP_DELAY;

                    wasLeftBefore = shouldBeLeft;
                    previousEnd = h.EndTime;
                    hitObjects.RemoveAt(i);
                    i--;
                }
            }

            if (time < keyUpTime[0])
                actions.Add(OsuAction.LeftButton);
            if (time < keyUpTime[1])
                actions.Add(OsuAction.RightButton);
            if (actions.Count == 2)
                keyUpTime[(int)(wasLeftBefore ? OsuAction.RightButton : OsuAction.LeftButton)] = time;

            lastTime = time;
            return actions.ToArray();
        }
    }
}
