// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Utils;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.Danse.Movers.Spinners
{
    public abstract class SpinnerMover
    {
        protected Spinner Spinner { get; set; }
        protected float SpinRadiusStart { get; set; }
        protected float SpinRadiusEnd { get; set; }

        protected SpinnerMover(Spinner spinner, float spinRadiusStart, float spinRadiusEnd)
        {
            Spinner = spinner;
            SpinRadiusStart = spinRadiusStart;
            SpinRadiusEnd = spinRadiusEnd;
        }

        public abstract Vector2 PositionAt(double time);

        protected float RadiusAt(double time) => Interpolation.ValueAt(time, SpinRadiusStart, SpinRadiusEnd, Spinner.StartTime, Spinner.EndTime);
    }
}
