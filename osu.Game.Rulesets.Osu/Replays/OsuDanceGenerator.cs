// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Replays;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Replays.Mover;
using osu.Game.Rulesets.Osu.UI;
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

            var h = Beatmap.HitObjects[0];

            AddFrameToReplay(new OsuReplayFrame(h.StartTime - 1500, h.StackedPosition));

            // Initialize the mover
            CurrentPostion = h.StackedPosition;
            OnObjChange();

            for (int i = 0; i < hitObjects.Count; i++)
            {
                var prev = h;
                h = hitObjects[i];
                CurrentPostion = addHitObjectClickFrames(h, prev);

                // Apply the next object to the mover
                //CurrentPostion = h.StackedPosition;
                var next = hitObjects[Math.Min(i + 1, hitObjects.Count - 1)];
                TargetPostion = next.StackedPosition;
                ObjectIndex = Math.Min(Math.Max(hitObjects.Count - 2, 0), i);
                OnObjChange();

                // Computes the cursor position for all replayable frames between two objects
                for (double time = h.GetEndTime() + GetFrameDelay(h.GetEndTime()); time < TargetObject.StartTime; time += GetFrameDelay(time))
                {
                    AddFrameToReplay(new OsuReplayFrame(time, MoverUtilExtensions.ApplyOffset(Update(time), time, 0), getAction(time)));
                }
            }

            if (!(h is IHasDuration))
            {
                //addHitObjectClickFrames(hitObjects[^1], hitObjects[^2]);
            }

            var lastFrame = (OsuReplayFrame)Frames[^1];
            lastFrame.Actions.Clear();
            AddFrameToReplay(lastFrame);

            return Replay;
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

        private void updateAction(OsuHitObject h, OsuHitObject last)
        {
            double timeDifference = ApplyModsToTimeDelta(last.GetEndTime(), h.StartTime);

            if (timeDifference > 0 && timeDifference < 266)
                buttonIndex++;
            else
                buttonIndex = 0;

            var action = buttonIndex % 2 == 0 ? OsuAction.LeftButton : OsuAction.RightButton;
            keyUpTime[(int)action] = h.GetEndTime() + KEY_UP_DELAY;
        }

        private OsuAction[] getAction(double time)
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

        private Vector2 addHitObjectClickFrames(OsuHitObject h, OsuHitObject prev)
        {
            Vector2 startPosition = h.StackedPosition;
            Vector2 difference = startPosition - SPINNER_CENTRE;
            float radius = difference.Length;
            float angle = radius == 0 ? 0 : MathF.Atan2(difference.Y, difference.X);
            Vector2 pos = h.StackedEndPosition;
            updateAction(h, prev);

            switch (h)
            {
                case Slider slider:
                    AddFrameToReplay(new OsuReplayFrame(h.StartTime, h.StackedPosition, getAction(h.StartTime)));

                    for (double j = GetFrameDelay(slider.StartTime); j < slider.Duration; j += GetFrameDelay(slider.StartTime + j))
                    {
                        pos = slider.StackedPositionAt(j / slider.Duration);
                        AddFrameToReplay(new OsuReplayFrame(h.StartTime + j, pos, getAction(h.StartTime + j)));
                    }

                    break;

                case Spinner spinner:
                    double rEndTime = spinner.StartTime + spinner.Duration * 0.7;
                    double previousFrame = h.StartTime;
                    double delay;

                    for (double nextFrame = h.StartTime + GetFrameDelay(h.StartTime); nextFrame < spinner.EndTime; nextFrame += delay)
                    {
                        delay = GetFrameDelay(previousFrame);
                        double t = ApplyModsToTimeDelta(previousFrame, nextFrame) * -1;
                        angle += (float)t / 20;
                        double r = nextFrame > rEndTime ? 50 : Interpolation.ValueAt(nextFrame, 50, 50, spinner.StartTime, rEndTime, Easing.In);
                        pos = SPINNER_CENTRE + CirclePosition(angle, r);
                        addOffSetFrame(new OsuReplayFrame((int)nextFrame, pos, getAction(nextFrame)), 0);

                        previousFrame = nextFrame;
                    }

                    break;

                default:
                    addOffSetFrame(new OsuReplayFrame(h.StartTime, Update(h.StartTime), getAction(h.StartTime)), 0);
                    break;
            }

            return pos;
        }

        private void addOffSetFrame(OsuReplayFrame frame, float radius)
        {
            frame.Position = MoverUtilExtensions.ApplyOffset(frame.Position, frame.Time, radius);
            AddFrameToReplay(frame);
        }

        #endregion

        #region Mover

        public int ObjectIndex { set; protected get; }

        protected OsuHitObject CurrentObject => hitObjects[ObjectIndex];

        public OsuHitObject TargetObject => hitObjects[Math.Min(ObjectIndex + 1, hitObjects.Count - 1)];

        protected Vector2 CurrentPostion;

        protected Vector2 TargetPostion;
        protected double CurrentObjectTime => CurrentObject.GetEndTime();
        protected double TargetObjectTime => TargetObject.StartTime;
        protected double Duration => TargetObjectTime - CurrentObjectTime;

        protected IReadOnlyList<IApplicableToRate> TimeAffectingMods { set; get; }

        protected virtual void OnObjChange() { }
        protected abstract Vector2 Update(double time);

        #endregion

        private class OsuKeyDownReplayFrame : OsuReplayFrame
        {
            public OsuKeyDownReplayFrame(double time, Vector2 position)
                : base(time, position)
            {
            }
        }

        private class OsuKeyUpReplayFrame : OsuReplayFrame
        {
            public OsuKeyUpReplayFrame(double time, Vector2 position)
                : base(time, position)
            {
            }
        }
    }
}
