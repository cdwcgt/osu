// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Utils;
using osu.Game.Configuration;
using osu.Game.Rulesets.Osu.Replays.Danse.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.Danse.Movers
{
    public class LinearMover : Mover
    {
        protected readonly bool WaitForPreempt = CustomConfigManager.Instance.Get<bool>(CustomSetting.WaitForPreempt);
        protected new double StartTime;

        protected double GetReactionTime(double timeInstant) => ApplyModsToRate(timeInstant, 100);

        protected double ApplyModsToRate(double time, double rate)
        {
            foreach (var mod in TimeAffectingMods)
                rate = mod.ApplyToRate(time, rate);
            return rate;
        }

        public override Vector2 Update(double time) => Interpolation.ValueAt(time, StartPos, EndPos, StartTime, EndTime, Easing.Out);

        public override int SetObjects(List<DanceHitObject> objects)
        {
            base.SetObjects(objects);
            StartTime = base.StartTime;

            if (WaitForPreempt)
            {
                StartTime = Math.Max(StartTime, EndTime - End.BaseObject.TimePreempt - GetReactionTime(EndTime - End.BaseObject.TimePreempt));
            }

            return 2;
        }
    }
}
