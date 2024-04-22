// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Utils;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.Danse.Movers.Spinners
{
    public abstract class SpinnerMover
    {
        protected const float RPMS = 0.00795f;

        public float StartAngle { get; set; }
        protected double StartTime { get; set; }
        protected double EndTime { get; set; }
        protected float SpinRadiusStart { get; set; }
        protected float SpinRadiusEnd { get; set; }

        protected SpinnerMover(double startTime, double endTime, float spinRadiusStart, float spinRadiusEnd)
        {
            StartTime = startTime;
            EndTime = endTime;
            SpinRadiusStart = spinRadiusStart;
            SpinRadiusEnd = spinRadiusEnd;
        }

        public abstract Vector2 PositionAt(double time);

        public float RadiusAt(double time) => Interpolation.ValueAt(time, SpinRadiusStart, SpinRadiusEnd, StartTime, EndTime);
    }
}
