// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Rulesets.Mods;
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

        public virtual void SetObjects(List<DanceHitObject> objects)
        {
            Start = objects[0];
            End = objects[Math.Min(objects.Count - 1, 1)];
            StartTime = Start.EndTime;
            EndTime = End.StartTime;
            StartPos = Start.EndPos;
            EndPos = End.StartPos;
        }

        public abstract Vector2 Update(double time);
    }
}
