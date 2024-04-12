// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.Danse.Objects
{
    public class DanceHitObject
    {
        public readonly OsuHitObject BaseObject;
        public Vector2 StartPos;
        public Vector2 EndPos;
        public double StartTime;
        public double EndTime;
        public bool SliderPoint;
        public bool SliderPointStart;
        public bool SliderPointEnd;
        public bool DoubleClick;

        public DanceHitObject(OsuHitObject baseObject)
        {
            BaseObject = baseObject;
            StartTime = baseObject.StartTime;
            EndTime = baseObject.GetEndTime();
            StartPos = BaseObject.StackedPosition;
            EndPos = BaseObject.StackedEndPosition;
        }

        public virtual Vector2 PositionAt(double time) => BaseObject switch
        {
            Slider slider => slider.StackedPositionAt((time - StartTime) / slider.Duration),
            _ => EndPos
        };
    }
}
