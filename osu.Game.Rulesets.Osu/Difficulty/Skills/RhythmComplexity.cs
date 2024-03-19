// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    public class RhythmComplexity : Skill
    {
        // 圈的数量（不包括滑条和转盘） v2下可能需要改动
        private int circleCount;

        // 目前是第几个物件，包括滑条和转盘
        private int noteIndex;

        // 是否和前一个note不合拍
        private bool isPreviousOffbeat;
        private readonly List<int> previousDoubles = new List<int>();
        private double difficultyTotal;

        public RhythmComplexity(Mod[] mods) : base(mods)
        {
        }

        public override void Process(DifficultyHitObject current)
        {
            var osuCurrent = (OsuDifficultyHitObject)current;

            if (current.BaseObject is HitCircle)
            {
                difficultyTotal += calculateRhythmBonus(osuCurrent);
                circleCount++;
            }
            else
                // 当前物件没有头准确度判断，故永远合拍
                isPreviousOffbeat = false;

            noteIndex++;
        }

        public override double DifficultyValue()
        {
            //双曲正切值，但是不知道为什么
            double lengthRequirement = Math.Tanh(circleCount / 50.0);
            return 1 + difficultyTotal / circleCount * lengthRequirement;
        }

        private double calculateRhythmBonus(OsuDifficultyHitObject current)
        {
            double rhythmBonus = 0.05 * current.Flow;

            if (current.Index == 0)
                return rhythmBonus;

            // 获取上一个物件
            OsuDifficultyHitObject previous = (OsuDifficultyHitObject)current.Previous(0);

            // 在Flow的基础上计算RhythmBonus
            if (previous.BaseObject is HitCircle)
                rhythmBonus += calculateCircleToCircleRhythmBonus(current);
            else if (previous.BaseObject is Slider)
                rhythmBonus += calculateSliderToCircleRhythmBonus(current);
            else if (previous.BaseObject is Spinner)
                // 前一个是转盘，故永远合拍
                isPreviousOffbeat = false;

            return rhythmBonus;
        }

        private double calculateCircleToCircleRhythmBonus(OsuDifficultyHitObject current)
        {
            // 上一个物件
            var previous = (OsuDifficultyHitObject)current.Previous(0);
            double rhythmBonus = 0;

            // current.Flow 见 OsuDifficultyHitObject
            if (isPreviousOffbeat && Utils.IsRatioEqualGreater(1.5, current.GapTime, previous.GapTime))
            {
                rhythmBonus = 5; // Doubles, Quads etc.

                // 只取previousDoubles后面10个进行枚举
                foreach (int previousDouble in previousDoubles.Skip(Math.Max(0, previousDoubles.Count - 10)))
                {
                    if (previousDouble > 0) // -1 is used to mark 1/3s
                        rhythmBonus *= 1 - 0.5 * Math.Pow(0.9, noteIndex - previousDouble); // Reduce the value of repeated doubles.
                    else
                        rhythmBonus = 5;
                }

                //添加当前物件的index
                previousDoubles.Add(noteIndex);
            }
            else if (Utils.IsRatioEqual(0.667, current.GapTime, previous.GapTime))
            {
                rhythmBonus = 4 + 8 * current.Flow; // Transition to 1/3s
                if (current.Flow > 0.8)
                    previousDoubles.Add(-1);
            }
            else if (Utils.IsRatioEqual(0.333, current.GapTime, previous.GapTime))
                rhythmBonus = 0.4 + 0.8 * current.Flow; // Transition to 1/6s
            else if (Utils.IsRatioEqual(0.5, current.GapTime, previous.GapTime) || Utils.IsRatioEqualLess(0.25, current.GapTime, previous.GapTime))
                rhythmBonus = 0.1 + 0.2 * current.Flow; // Transition to triples, streams etc.

            //标记当前物件是否合拍
            if (Utils.IsRatioEqualLess(0.667, current.GapTime, previous.GapTime) && current.Flow > 0.8)
                isPreviousOffbeat = true;
            else if (Utils.IsRatioEqual(1, current.GapTime, previous.GapTime) && current.Flow > 0.8)
                isPreviousOffbeat = !isPreviousOffbeat;
            else
                isPreviousOffbeat = false;

            return rhythmBonus;
        }

        // 计算滑条到打击圈之间的节奏奖励
        private double calculateSliderToCircleRhythmBonus(OsuDifficultyHitObject current)
        {
            double rhythmBonus = 0;
            // 可以认为是上个滑条的时间长度，单位ms
            double sliderMS = current.StrainTime - current.GapTime;

            //上一个滑条的时间长度乘以0.5或0.25后是否与GapTime（与上一个物件结尾时间的长度）近似
            if (Utils.IsRatioEqual(0.5, current.GapTime, sliderMS) || Utils.IsRatioEqual(0.25, current.GapTime, sliderMS))
            {
                double endFlow = calculateSliderEndFlow(current);
                rhythmBonus = 0.3 * endFlow; // Triples, streams etc. starting with a slider end.

                if (endFlow > 0.8)
                    //大于0.8时认为这个物件不合拍（变量用于下一个物件）
                    isPreviousOffbeat = true;
                else
                    isPreviousOffbeat = false;
            }
            else
                isPreviousOffbeat = false;

            return rhythmBonus;
        }

        private static double calculateSliderEndFlow(OsuDifficultyHitObject current)
        {
            // 计算串的bpm，除以上个物件结尾时间和当前物件的开始时间的差
            double streamBpm = 15000 / current.GapTime;
            // 计算是否为串的速度，越接近1越可能是串
            // 值为120时为0，150时为1
            double isFlowSpeed = Utils.TransitionToTrue(streamBpm, 120, 30);

            double distanceOffset = (Math.Tanh((streamBpm - 140) / 20) + 2) * OsuDifficultyHitObject.NORMALISED_RADIUS;
            // 与上一个物件的距离越接近distanceOffset越可能是串
            // 值为 distanceOffset 为1，超过distanceOffset + NORMALISED_RADIUS(52) 时为0
            double isFlowDistance = Utils.TransitionToFalse(current.JumpDistance, distanceOffset, OsuDifficultyHitObject.NORMALISED_RADIUS);

            return isFlowSpeed * isFlowDistance;
        }
    }
}
