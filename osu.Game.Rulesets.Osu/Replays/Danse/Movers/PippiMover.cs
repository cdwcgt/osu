// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osuTK;
using static osu.Game.Rulesets.Osu.Replays.Danse.Movers.MoverUtilExtensions;

namespace osu.Game.Rulesets.Osu.Replays.Danse.Movers
{
    public class PippiMover : LinearMover
    {
        public override Vector2 Update(double time) => ApplyPippiOffset(base.Update(time), time, -1);

        public override void SetObjects(List<DanceHitObject> objects)
        {
            base.SetObjects(objects);
            var start = objects[0];
            start.StartPos = ApplyPippiOffset(start.StartPos, start.StartTime, -1);
            start.EndPos = ApplyPippiOffset(start.EndPos, start.EndTime, -1);

            if (objects.Count > 1)
            {
                var end = objects[1];
                end.StartPos = ApplyPippiOffset(end.StartPos, end.StartTime, -1);
            }
        }
    }
}
