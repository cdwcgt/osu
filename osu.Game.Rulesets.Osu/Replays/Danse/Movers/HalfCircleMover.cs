// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using osuTK;
using static osu.Game.Rulesets.Osu.Replays.Danse.Movers.MoverUtilExtensions;

namespace osu.Game.Rulesets.Osu.Replays.Danse.Movers
{
    public class HalfCircleMover : Mover
    {
        private Vector2 middle = Vector2.Zero;
        private float radius;
        private float ang;

        public override void SetObjects(List<DanceHitObject> objects)
        {
            base.SetObjects(objects);
            middle = (StartPos + EndPos) / 2;
            ang = StartPos.AngleRV(middle);
            radius = Vector2.Distance(middle, StartPos);
        }

        public override Vector2 Update(double time) => middle + V2FromRad(ang + ProgressAt(time) * MathF.PI, radius);
    }
}
