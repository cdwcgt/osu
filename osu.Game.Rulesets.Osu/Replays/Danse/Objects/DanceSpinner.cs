// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Replays.Danse.Movers.Spinners;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.Danse.Objects
{
    public class DanceSpinner : DanceHitObject
    {
        private readonly SpinnerMover mover;

        public DanceSpinner(Spinner baseObject, SpinnerMover mover)
            : base(baseObject)
        {
            this.mover = mover;
        }

        public override Vector2 PositionAt(double time)
        {
            return mover.PositionAt(time);
        }
    }
}
