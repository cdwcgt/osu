// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Replays.Danse.Objects;
//using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.Danse.Movers
{
    public abstract class Mover
    {
        public double StartTime { get; protected set; }
        public double EndTime { get; protected set; }
        protected double Duration => EndTime - StartTime;

        protected DanceHitObject Start = null!;
        protected DanceHitObject End = null!;
        protected Vector2 StartPos;
        protected Vector2 EndPos;

        protected float ProgressAt(double time) => (float)((time - StartTime) / Duration);

        public IReadOnlyList<IApplicableToRate> TimeAffectingMods { set; protected get; } = null!;

        public virtual int SetObjects(List<DanceHitObject> objects)
        {
            Start = objects[0];
            End = objects[1];
            StartTime = Start.EndTime;
            EndTime = End.StartTime;
            StartPos = Start.EndPos;
            EndPos = End.StartPos;

            return 2;
        }

        public abstract Vector2 Update(double time);
    }
}
