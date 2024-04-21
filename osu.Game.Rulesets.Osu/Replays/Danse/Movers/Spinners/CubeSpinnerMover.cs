// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.Danse.Movers.Spinners
{
    public class CubeSpinnerMover : SpinnerMover
    {
        private Vector4[] cubeVertices =
        {
            new Vector4(-1, -1, -1, 1),
            new Vector4(-1, 1, -1, 1),
            new Vector4(1, 1, -1, 1),
            new Vector4(1, -1, -1, 1),
            new Vector4(-1, -1, 1, 1),
            new Vector4(-1, 1, 1, 1),
            new Vector4(1, 1, 1, 1),
            new Vector4(1, -1, 1, 1)
        };

        private int[] cubeIndices = { 0, 1, 2, 3, 0, 4, 5, 1, 5, 6, 2, 6, 7, 3, 7, 4 };

        public CubeSpinnerMover(Spinner spinner, float spinRadiusStart, float spinRadiusEnd)
            : base(spinner, spinRadiusStart, spinRadiusEnd)
        {
        }

        public override Vector2 PositionAt(double time)
        {
            double duration = time - Spinner.StartTime;
            float radY = MathF.Sin((float)duration / 9000f * 2 * MathF.PI) * 3.0f / 18 * MathF.PI;
            float radX = MathF.Sin((float)duration / 5000f * 2 * MathF.PI) * 3.0f / 18 * MathF.PI;
            float scale = (1.0f + MathF.Sin((float)duration / 4500 * 2 * MathF.PI) * 0.3f) * RadiusAt(time);
            var mat = Matrix4.CreateRotationY(radY) * Matrix4.CreateRotationX(radX) * Matrix4.CreateScale(scale);
            int startIndex = ((int)Math.Max(0, duration) / 4) % cubeIndices.Length;

            int i1 = cubeIndices[startIndex];
            int i2 = cubeIndices[0];

            if (startIndex < cubeIndices.Length - 1)
            {
                i2 = cubeIndices[startIndex + 1];
            }

            var pt1 = cubeVertices[i1];
            var pt2 = cubeVertices[i2];
            float t = (int)duration % 4 / 4f;
            var pt = new Vector4((pt2.X - pt1.X) * t + pt1.X, (pt2.Y - pt1.Y) * t + pt1.Y, (pt2.Z - pt1.Z) * t + pt1.Z, 1);
            pt *= mat;
            pt.X *= 1 + pt[2] / scale / 10f;
            pt.Y *= 1 + pt[2] / scale / 10f;
            return OsuAutoGeneratorBase.SPINNER_CENTRE + new Vector2(pt.X, pt.Y);
        }
    }
}
