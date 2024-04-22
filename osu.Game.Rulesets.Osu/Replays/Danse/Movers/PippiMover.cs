// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Replays.Danse.Objects;
using osuTK;
using static osu.Game.Rulesets.Osu.Replays.Danse.Movers.MoverUtilExtensions;

namespace osu.Game.Rulesets.Osu.Replays.Danse.Movers
{
    public class PippiMover : LinearMover
    {
        public override Vector2 Update(double time) => ApplyPippiOffset(base.Update(time), time, -1);

        public override Vector2 GetObjectPosition(double time, DanceHitObject h)
        {
            var pos = base.GetObjectPosition(time, h);

            if (h.BaseObject is Spinner)
                return pos;

            return ApplyPippiOffset(pos, time, -1);
        }
    }
}
