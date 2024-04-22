// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.Danse.Movers.Spinners
{
    public class PippiSpinnerMover : CircleSpinnerMover
    {
        public PippiSpinnerMover(double startTime, double endTime, float spinRadiusStart, float spinRadiusEnd)
            : base(startTime, endTime, spinRadiusStart, spinRadiusEnd)
        {
        }

        public override Vector2 PositionAt(double time) => MoverUtilExtensions.ApplyPippiOffset(base.PositionAt(time), time, 100);
    }
}
