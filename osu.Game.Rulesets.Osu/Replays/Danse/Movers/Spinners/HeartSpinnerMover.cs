// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.Danse.Movers.Spinners
{
    public class HeartSpinnerMover : SpinnerMover
    {
        public HeartSpinnerMover(double startTime, double endTime, float spinRadiusStart, float spinRadiusEnd)
            : base(startTime, endTime, spinRadiusStart, spinRadiusEnd)
        {
        }

        public override Vector2 PositionAt(double time)
        {
            float rad = StartAngle + RPMS * (float)(time - StartTime) * 2 * MathF.PI;
            float x = MathF.Pow(MathF.Sin(rad), 3);
            float y = (13 * MathF.Cos(rad) - 5 * MathF.Cos(2 * rad) - 2 * MathF.Cos(3 * rad) - MathF.Cos(4 * rad)) / 16;
            return OsuAutoGeneratorBase.SPINNER_CENTRE + new Vector2(x, -y) * RadiusAt(time);
        }
    }
}
