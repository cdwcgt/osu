// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.Danse.Movers.Spinners
{
    public class PippiSpinnerMover : CircleSpinnerMover
    {
        public PippiSpinnerMover(Spinner spinner, float spinRadiusStart, float spinRadiusEnd)
            : base(spinner, spinRadiusStart, spinRadiusEnd)
        {
        }

        public override Vector2 PositionAt(double time)
        {
            return MoverUtilExtensions.ApplyPippiOffset(base.PositionAt(time), time, 100);
        }
    }
}
