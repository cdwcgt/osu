// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.Danse.Movers.Spinners
{
    public class CircleSpinnerMover : SpinnerMover
    {
        public CircleSpinnerMover(Spinner spinner, float spinRadiusStart, float spinRadiusEnd)
            : base(spinner, spinRadiusStart, spinRadiusEnd)
        {
        }

        public override Vector2 PositionAt(double time)
            => OsuAutoGeneratorBase.SPINNER_CENTRE
               + MoverUtilExtensions.V2FromRad((float)(RPMS * (time - Spinner.StartTime) * 2 * Math.PI), RadiusAt(time));
    }
}
