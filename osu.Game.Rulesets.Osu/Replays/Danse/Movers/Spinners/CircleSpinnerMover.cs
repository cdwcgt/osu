// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.Danse.Movers.Spinners
{
    public class CircleSpinnerMover : SpinnerMover
    {
        public CircleSpinnerMover(double startTime, double endTime, float spinRadiusStart, float spinRadiusEnd)
            : base(startTime, endTime, spinRadiusStart, spinRadiusEnd)
        {
        }

        public override Vector2 PositionAt(double time)
            => OsuAutoGeneratorBase.SPINNER_CENTRE
               + MoverUtilExtensions.V2FromRad(StartAngle + (float)(RPMS * (time - StartTime) * 2 * Math.PI), RadiusAt(time));
    }
}
