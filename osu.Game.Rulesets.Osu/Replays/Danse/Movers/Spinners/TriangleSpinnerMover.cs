// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.Danse.Movers.Spinners
{
    public class TriangleSpinnerMover : SpinnerMover
    {
        private Vector3[] indices =
        {
            new Vector3(-0.86602540378f, -0.5f, 0), new Vector3(0.86602540378f, -0.5f, 0), new Vector3(0, 1, 0)
        };

        public TriangleSpinnerMover(double startTime, double endTime, float spinRadiusStart, float spinRadiusEnd)
            : base(startTime, endTime, spinRadiusStart, spinRadiusEnd)
        {
        }

        public override Vector2 PositionAt(double time)
        {
            double duration = time - StartTime;
            var mat = Matrix3.CreateRotationZ((float)duration * 2000 * 2 * MathF.PI) * Matrix3.CreateScale(new Vector3(RadiusAt(time), RadiusAt(time), 1));
            int startIndex = ((int)(Math.Max(0, duration / 10)) % 3);

            var pt1 = indices[startIndex];
            var pt2 = indices[0];

            if (startIndex < 2)
                pt2 = indices[startIndex + 1];

            pt1 *= mat;
            pt2 *= mat;

            float t = (int)duration % 10 / 10f;
            return OsuAutoGeneratorBase.SPINNER_CENTRE + new Vector2((pt2.X - pt1.X) * t + pt1.X, (pt2.Y - pt1.Y) * t + pt1.Y);
        }
    }
}
