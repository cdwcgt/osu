// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Replays;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Replays.Mover;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays
{
    public abstract class OsuDanceGenerator : OsuAutoGeneratorBase
    {
        public new OsuBeatmap Beatmap => (OsuBeatmap)base.Beatmap;

        private List<OsuHitObject> hitObjects = new List<OsuHitObject>();

        public override int FrameRate => 120;

        protected OsuDanceGenerator(IBeatmap beatmap, IReadOnlyList<Mod> mods)
            : base(beatmap, mods)
        {
            TimeAffectingMods = mods.OfType<IApplicableToRate>().ToList();
            preProcessObjects();
        }

        private void preProcessObjects()
        {
            for (int i = 0; i < Beatmap.HitObjects.Count; i++)
            {
                var h = Beatmap.HitObjects[i];

                if (h is Spinner { SpinsRequired: 0 })
                    continue;

                hitObjects.Add(h);
            }

            hitObjects = hitObjects.OrderBy(h => h.StartTime).ToList();
        }

        public override Replay Generate()
        {
            if (Beatmap.HitObjects.Count == 0)
                return Replay;

            var h = targetObject = Beatmap.HitObjects[0];
            currentObject = new HitCircle
            {
                StartTime = h.StartTime - 1500,
                Position = new Vector2(256, 500)
            };

            // the part generate entry curve
            CurrentPosition = new Vector2(256, 500);
            TargetPosition = h.StackedPosition;
            OnObjChange();
            applyMoverPosition(h.StartTime - 1500, h.StartTime - 1);

            // Initialize the mover
            CurrentPosition = h.StackedPosition;
            OnObjChange();
            currentObject = null;
            targetObject = null;

            for (int i = 0; i < hitObjects.Count; i++)
            {
                var prev = h;
                h = hitObjects[i];
                CurrentPosition = AddHitObjectClickFrames(h, prev);

                // Apply the next object to the mover
                var next = hitObjects[Math.Min(i + 1, hitObjects.Count - 1)];
                TargetPosition = next.StackedPosition;
                ObjectIndex = Math.Min(Math.Max(hitObjects.Count - 2, 0), i);
                OnObjChange();
                applyMoverPosition(h.GetEndTime(), TargetObject.StartTime);
            }

            // clear all key action when finish.
            var lastFrame = (OsuReplayFrame)Frames[^1];
            AddFrameToReplay(new OsuReplayFrame(lastFrame.Time + 1, lastFrame.Position));

            return Replay;

            // Computes the cursor position for all replayable frames between two objects
            void applyMoverPosition(double currentTime, double targetTime)
            {
                for (double time = currentTime + GetFrameDelay(currentTime); time < targetTime; time += GetFrameDelay(time))
                {
                    AddFrameToReplay(new OsuReplayFrame(time, MoverUtilExtensions.ApplyOffset(GetPosition(time), time, 0), GetAction(time)));
                }
            }
        }

        #region Helper subroutines

        /// <summary>
        /// Which button (left or right) to use for the current hitobject.
        /// Even means LMB will be used to click, odd means RMB will be used.
        /// This keeps track of the button previously used for alt/singletap logic.
        /// </summary>
        private int buttonIndex;

        /// <summary>
        /// Save the last time the key was released.
        /// [0] for LeftButton, [1] for RightButton.
        /// </summary>
        private readonly double[] keyUpTime = { -10000, -10000 };

        protected void UpdateAction(OsuHitObject h, OsuHitObject last)
        {
            double timeDifference = ApplyModsToTimeDelta(last.GetEndTime(), h.StartTime);

            if (timeDifference > 0 && timeDifference < 266)
                buttonIndex++;
            else
                buttonIndex = 0;

            var action = buttonIndex % 2 == 0 ? OsuAction.LeftButton : OsuAction.RightButton;
            keyUpTime[(int)action] = h.GetEndTime() + KEY_UP_DELAY;
        }

        protected OsuAction[] GetAction(double time)
        {
            var actions = new List<OsuAction>(2);

            // When one of the keys is held down, the other is always held down.
            if (time < keyUpTime[0])
                actions.Add(OsuAction.LeftButton);
            if (time < keyUpTime[1])
                actions.Add(OsuAction.RightButton);

            // If both can be held down, implement Alternate.
            if (actions.Count == 2)
            {
                var lastAction = (buttonIndex - 1) % 2 == 0 ? OsuAction.LeftButton : OsuAction.RightButton;
                keyUpTime[(int)lastAction] = time;
            }

            return actions.ToArray();
        }

        protected abstract Vector2 AddHitObjectClickFrames(OsuHitObject h, OsuHitObject prev);

        protected void AddOffSetFrame(OsuReplayFrame frame, float radius)
        {
            frame.Position = MoverUtilExtensions.ApplyOffset(frame.Position, frame.Time, radius);
            AddFrameToReplay(frame);
        }

        #endregion

        #region Mover

        public int ObjectIndex { set; protected get; }

        private OsuHitObject? currentObject;
        private OsuHitObject? targetObject;
        protected OsuHitObject CurrentObject => currentObject ?? hitObjects[ObjectIndex];
        public OsuHitObject TargetObject => targetObject ?? hitObjects[Math.Min(ObjectIndex + 1, hitObjects.Count - 1)];

        protected Vector2 CurrentPosition;

        protected Vector2 TargetPosition;
        protected double CurrentObjectTime => CurrentObject.GetEndTime();
        protected double TargetObjectTime => TargetObject.StartTime;
        protected double Duration => TargetObjectTime - CurrentObjectTime;

        protected IReadOnlyList<IApplicableToRate> TimeAffectingMods { set; get; }

        protected virtual void OnObjChange() { }
        protected abstract Vector2 GetPosition(double time);

        #endregion
    }
}
