// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Osu.Difficulty.Preprocessing
{
    public class OsuDifficultyHitObject : DifficultyHitObject
    {
        /// <summary>
        /// A distance by which all distances should be scaled in order to assume a uniform circle size.
        /// </summary>
        public const int NORMALISED_RADIUS = 52; // Change radius to 50 to make 100 the diameter. Easier for mental maths.

        private const int min_delta_time = 25;

        protected new OsuHitObject BaseObject => (OsuHitObject)base.BaseObject;

        /// <summary>
        /// Raw distance from the end position of the previous <see cref="OsuDifficultyHitObject"/> to the start position of this <see cref="OsuDifficultyHitObject"/>.
        /// </summary>
        public double RawJumpDistance { get; private set; }

        /// <summary>
        /// Normalized distance from the end position of the previous <see cref="OsuDifficultyHitObject"/> to the start position of this <see cref="OsuDifficultyHitObject"/>.
        /// 和上面不同的是会根据圈大小调整
        /// </summary>
        public double JumpDistance { get; private set; }

        /// <summary>
        /// 与上一个 <see cref="OsuDifficultyHitObject"/> 之间的时间差，至少为50，单位ms.
        /// </summary>
        public readonly double StrainTime;

        /// <summary>
        /// Normalised distance between the start and end position of this <see cref="OsuDifficultyHitObject"/>.
        /// </summary>
        public double TravelDistance { get; private set; }

        /// <summary>
        /// Angle the player has to take to hit this <see cref="OsuDifficultyHitObject"/>.
        /// Calculated as the angle between the circles (current-2, current-1, current).
        /// </summary>
        public double? Angle { get; private set; }

        /// <summary>
        /// Approach rate of <see cref="OsuDifficultyHitObject"/> in milliseconds.
        /// </summary>
        public double Preempt { get; private set; }

        /// <summary>
        /// Measure of angle leniency to be given when calculating the flow values of the next <see cref="OsuDifficultyHitObject"/> (scale of [0, 1]).
        /// </summary>
        public double AngleLeniency { get; private set; }

        /// <summary>
        /// Measure of expected aim flowiness based on time and distance from the previous <see cref="OsuDifficultyHitObject"/> (scale of [0, 1]).
        /// </summary>
        public double BaseFlow { get; private set; }

        /// <summary>
        /// Measure of expected aim flowiness based on <see cref="BaseFlow"/> and pattern context made up of the previous <see cref="OsuDifficultyHitObject"/>s (scale of [0, 1]).
        /// </summary>
        public double Flow { get; private set; }

        /// <summary>
        /// Milliseconds elapsed since the start time of the <see cref="OsuDifficultyHitObject"/> before the previous, with a minimum of 100ms.
        /// </summary>
        public double LastTwoStrainTime { get; private set; }

        /// <summary>
        /// Milliseconds elapsed since the end time of the previous <see cref="OsuDifficultyHitObject"/>, with a minimum of 50ms.
        /// 上一个物件的结尾时间与这个物件之间的时间差，最小为50
        /// </summary>
        public double GapTime { get; private set; }

        /// <summary>
        /// Retrieves the full hit window for a Great <see cref="HitResult"/>.
        /// </summary>
        public double HitWindowGreat { get; private set; }

        private readonly OsuHitObject? lastLastObject;
        private readonly OsuHitObject lastObject;
        private readonly OsuDifficultyHitObject lastLastDifficultyObject;
        private readonly OsuDifficultyHitObject lastDifficultyObject;

        public OsuDifficultyHitObject(HitObject hitObject, HitObject lastLastObject, HitObject lastObject, OsuDifficultyHitObject lastLastDifficultyObject, OsuDifficultyHitObject lastDifficultyObject, double clockRate, List<DifficultyHitObject> objects, int index)
            : base(hitObject, lastObject, clockRate, objects, index)
        {
            this.lastLastObject = lastLastObject as OsuHitObject;
            this.lastObject = (OsuHitObject)lastObject;
            this.lastLastDifficultyObject = lastLastDifficultyObject;
            this.lastDifficultyObject = lastDifficultyObject;

            setDistances(clockRate);

            // Capped to 25ms to prevent difficulty calculation breaking from simultaneous objects.
            //应该是ppv2的，下面ppp的覆盖了

            //StrainTime = Math.Max(DeltaTime, min_delta_time);

            Preempt = ((OsuHitObject)hitObject).TimePreempt / clockRate;

            // Every strain interval is hard capped at the equivalent of 375 BPM streaming speed as a safety measure
            StrainTime = Math.Max(50, DeltaTime);

            if (lastLastObject == null)
                LastTwoStrainTime = 100;
            else
                LastTwoStrainTime = Math.Max(100, (hitObject.StartTime - lastLastObject.StartTime) / clockRate);

            if (lastObject is HitCircle)
                GapTime = StrainTime;
            else if (lastObject is Slider lastSlider)
                GapTime = Math.Max(50, (hitObject.StartTime - lastSlider.EndTime) / clockRate);
            else if (lastObject is Spinner lastSpinner)
                GapTime = Math.Max(50, (hitObject.StartTime - lastSpinner.EndTime) / clockRate);

            setFlowValues();
        }

        // 貌似没用 ppv2的东西 ppp没用到
        public double OpacityAt(double time, bool hidden)
        {
            if (time > BaseObject.StartTime)
            {
                // Consider a hitobject as being invisible when its start time is passed.
                // In reality the hitobject will be visible beyond its start time up until its hittable window has passed,
                // but this is an approximation and such a case is unlikely to be hit where this function is used.
                return 0.0;
            }

            double fadeInStartTime = BaseObject.StartTime - BaseObject.TimePreempt;
            double fadeInDuration = BaseObject.TimeFadeIn;

            if (hidden)
            {
                // Taken from OsuModHidden.
                double fadeOutStartTime = BaseObject.StartTime - BaseObject.TimePreempt + BaseObject.TimeFadeIn;
                double fadeOutDuration = BaseObject.TimePreempt * OsuModHidden.FADE_OUT_DURATION_MULTIPLIER;

                return Math.Min
                (
                    Math.Clamp((time - fadeInStartTime) / fadeInDuration, 0.0, 1.0),
                    1.0 - Math.Clamp((time - fadeOutStartTime) / fadeOutDuration, 0.0, 1.0)
                );
            }

            return Math.Clamp((time - fadeInStartTime) / fadeInDuration, 0.0, 1.0);
        }

        private void setDistances(double clockRate)
        {
            // We will scale distances by this factor, so we can assume a uniform CircleSize among beatmaps.
            float scalingFactor = NORMALISED_RADIUS / (float)BaseObject.Radius;

            if (BaseObject.Radius < 30)
            {
                float smallCircleBonus = Math.Min(30 - (float)BaseObject.Radius, 5) / 50;
                scalingFactor *= 1 + smallCircleBonus;
            }

            if (lastObject is Slider lastSlider)
            {
                computeSliderCursorPosition(lastSlider);
                TravelDistance = lastSlider.LazyTravelDistance * scalingFactor;
            }

            Vector2 lastCursorPosition = getEndCursorPosition(lastObject);

            // Don't need to jump to reach spinners
            if (!(BaseObject is Spinner))
                RawJumpDistance = (BaseObject.StackedPosition - lastCursorPosition).Length;
            JumpDistance = (BaseObject.StackedPosition * scalingFactor - lastCursorPosition * scalingFactor).Length;

            if (lastLastObject != null)
            {
                Vector2 lastLastCursorPosition = getEndCursorPosition(lastLastObject);

                Vector2 v1 = lastLastCursorPosition - lastObject.StackedPosition;
                Vector2 v2 = BaseObject.StackedPosition - lastCursorPosition;

                float dot = Vector2.Dot(v1, v2);
                float det = v1.X * v2.Y - v1.Y * v2.X;

                Angle = Math.Abs(Math.Atan2(det, dot));
            }
        }

        // 计算滑条距离参数
        private void computeSliderCursorPosition(Slider slider)
        {
            if (slider.LazyEndPosition != null)
                return;

            // 滑条头位置
            slider.LazyEndPosition = slider.StackedPosition;

            float approxFollowCircleRadius = (float)(slider.Radius * 3);

            // Skip the head circle
            var scoringTimes = slider.NestedHitObjects.Skip(1).Select(t => t.StartTime);
            foreach (var time in scoringTimes)
                computeVertex(time);

            // 本地方法
            void computeVertex(double t)
            {
                //滑条进度 [0,1]
                double progress = (t - slider.StartTime) / slider.SpanDuration;

                if (progress % 2 >= 1)
                    progress = 1 - progress % 1;
                else
                    progress %= 1;

                // 二维坐标
                Vector2 diff = slider.StackedPosition + slider.Path.PositionAt(progress) - slider.LazyEndPosition.Value;
                // 坐标与零点之间的长度 (float)Math.Sqrt(X * X + Y * Y)
                float dist = diff.Length;

                if (dist > approxFollowCircleRadius)
                {
                    // The cursor would be outside the follow circle, we need to move it
                    // 将坐标标准化到单位圆
                    diff.Normalize(); // Obtain direction of diff
                    dist -= approxFollowCircleRadius;
                    slider.LazyEndPosition = slider.LazyEndPosition! + diff * dist;
                    slider.LazyTravelDistance += dist;
                }
            }
        }

        /// <summary>
        /// 获取物件结束后的位置
        /// 滑条就是滑条尾的坐标，圈就是圈的位置
        /// </summary>
        /// <param name="hitObject"></param>
        /// <returns></returns>
        private Vector2 getEndCursorPosition(OsuHitObject hitObject)
        {
            Vector2 pos = hitObject.StackedPosition;

            if (hitObject is Slider slider)
            {
                computeSliderCursorPosition(slider);
                pos = slider.LazyEndPosition ?? pos;
            }

            return pos;
        }

        private void setFlowValues()
        {
            BaseFlow = calculateBaseFlow();
            Flow = calculateFlow();
        }

        private double calculateBaseFlow()
        {
            // 当上一个物件的 StrainTime 乘以 ratio (第一个参数) 大于这个物件的 StrainTime 时返回true
            if (lastDifficultyObject == null || Utils.IsRatioEqualLess(0.667, StrainTime, lastDifficultyObject.StrainTime))
                return calculateSpeedFlow() * calculateDistanceFlow(); // No angle checks for the first actual note of the stream.

            // a * 1.25 > b && a / 1.25 < b;
            if (Utils.IsRoughlyEqual(StrainTime, lastDifficultyObject.StrainTime))
                return calculateSpeedFlow() * calculateDistanceFlow(calculateAngleScalingFactor(Angle));

            return 0;
        }

        private double calculateSpeedFlow()
        {
            // 布尔函数
            // TransitionToTrue(double value, double transitionStart, double transitionInterval)
            // (-Math.Cos((value - transitionStart) * Math.PI / transitionInterval) + 1) / 2;
            // 最大为 1 最低为 0
            // 详见 Utils.cs

            // Sine curve transition from 0 to 1 starting at 90 BPM, reaching 1 at 90 + 30 = 120 BPM.
            return Utils.TransitionToTrue(streamBpm, 90, 30);
        }

        private double calculateDistanceFlow(double angleScalingFactor = 1)
        {
            double distanceOffset = (Math.Tanh((streamBpm - 140) / 20) + 2) * NORMALISED_RADIUS;

            // TransitionToTrue 函数一致，但是取相反值
            return Utils.TransitionToFalse(JumpDistance, distanceOffset * angleScalingFactor, distanceOffset);
        }

        /// <summary>
        /// 角度缩放因子
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        private double calculateAngleScalingFactor(double? angle)
        {
            // 检查 angle 是否是有效数字
            if (!Utils.IsNullOrNaN(angle))
            {
                double angleScalingFactor = (-Math.Sin(Math.Cos(angle!.Value) * Math.PI / 2) + 3) / 4;
                return angleScalingFactor + (1 - angleScalingFactor) * lastDifficultyObject.AngleLeniency;
            }
            else
                return 0.5;
        }

        private double calculateFlow()
        {
            if (lastDifficultyObject == null)
                return BaseFlow;

            // No angle check and a larger distance is allowed if the speed matches the previous notes, and those were flowy without a question.
            // (streamjumps, sharp turns)
            double irregularFlow = calculateIrregularFlow();

            // The next note will have lenient angle checks after a note with irregular flow.
            // (the stream section after the streamjump can take any direction too)
            AngleLeniency = (1 - BaseFlow) * irregularFlow;

            return Math.Max(BaseFlow, irregularFlow);
        }

        private double calculateIrregularFlow()
        {
            double irregularFlow = calculateExtendedDistanceFlow();

            if (Utils.IsRoughlyEqual(StrainTime, lastDifficultyObject.StrainTime))
                irregularFlow *= lastDifficultyObject.BaseFlow;
            else
                irregularFlow = 0;

            if (lastLastDifficultyObject != null)
            {
                if (Utils.IsRoughlyEqual(StrainTime, lastLastDifficultyObject.StrainTime))
                    irregularFlow *= lastLastDifficultyObject.BaseFlow;
                else
                    irregularFlow = 0;
            }

            return irregularFlow;
        }

        private double calculateExtendedDistanceFlow()
        {
            double distanceOffset = (Math.Tanh((streamBpm - 140) / 20) * 1.75 + 2.75) * NORMALISED_RADIUS;
            return Utils.TransitionToFalse(JumpDistance, distanceOffset, distanceOffset);
        }

        private double streamBpm => 15000 / StrainTime;
    }
}
