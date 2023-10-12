// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.Danse.Movers
{
    public class AxisAlignedMover : Mover
    {
        private SliderPath path = null!;

        public override void SetObjects(List<DanceHitObject> objects)
        {
            base.SetObjects(objects);

            float startX = StartPos.X;
            float startY = StartPos.Y;
            float endX = EndPos.X;
            float endY = EndPos.Y;
            var midP = MathF.Abs((EndPos - StartPos).X) < MathF.Abs((EndPos - EndPos).X)
                ? new Vector2(startX, endY)
                : new Vector2(endX, startY);

            path = new SliderPath(new[]
            {
                new PathControlPoint(StartPos, PathType.Linear),
                new PathControlPoint(midP),
                new PathControlPoint(EndPos)
            });
        }

        public override Vector2 Update(double time) => path.PositionAt(ProgressAt(time));
    }
}
