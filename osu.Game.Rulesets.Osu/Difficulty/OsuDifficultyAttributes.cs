// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Newtonsoft.Json;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;

namespace osu.Game.Rulesets.Osu.Difficulty
{
    public class OsuDifficultyAttributes : DifficultyAttributes
    {
        /// <summary>
        /// The difficulty corresponding to the aim skill.
        /// </summary>
        [JsonProperty("aim_difficulty")]
        public double AimDifficulty { get; set; }

        [JsonProperty("jump_aim_difficulty")]
        public double JumpAimDifficulty { get; set; }

        [JsonProperty("flow_aim_difficulty")]
        public double FlowAimDifficulty { get; set; }

        [JsonProperty("precision_difficulty")]
        public double PrecisionDifficulty { get; set; }

        /// <summary>
        /// The difficulty corresponding to the speed skill.
        /// </summary>
        [JsonProperty("speed_difficulty")]
        public double SpeedDifficulty { get; set; }

        [JsonProperty("stamina_difficulty")]
        public double StaminaDifficulty { get; set; }

        [JsonProperty("accuracy_difficulty")]
        public double AccuracyDifficulty { get; set; }

        /// <summary>
        /// The perceived approach rate inclusive of rate-adjusting mods (DT/HT/etc).
        /// </summary>
        /// <remarks>
        /// Rate-adjusting mods don't directly affect the approach rate difficulty value, but have a perceived effect as a result of adjusting audio timing.
        /// </remarks>
        [JsonProperty("approach_rate")]
        public double ApproachRate { get; set; }

        /// <summary>
        /// The perceived overall difficulty inclusive of rate-adjusting mods (DT/HT/etc).
        /// </summary>
        /// <remarks>
        /// Rate-adjusting mods don't directly affect the overall difficulty value, but have a perceived effect as a result of adjusting audio timing.
        /// </remarks>
        [JsonProperty("overall_difficulty")]
        public double OverallDifficulty { get; set; }

        /// <summary>
        /// The beatmap's drain rate. This doesn't scale with rate-adjusting mods.
        /// </summary>
        public double DrainRate { get; set; }

        /// <summary>
        /// The number of hitcircles in the beatmap.
        /// </summary>
        public int HitCircleCount { get; set; }

        /// <summary>
        /// The number of sliders in the beatmap.
        /// </summary>
        public int SliderCount { get; set; }

        /// <summary>
        /// The number of spinners in the beatmap.
        /// </summary>
        public int SpinnerCount { get; set; }

        public override IEnumerable<(int attributeId, object value)> ToDatabaseAttributes()
        {
            foreach (var v in base.ToDatabaseAttributes())
                yield return v;

            yield return (ATTRIB_ID_AIM, AimDifficulty);
            yield return (ATTRIB_ID_SPEED, SpeedDifficulty);
            yield return (ATTRIB_ID_OVERALL_DIFFICULTY, OverallDifficulty);
            yield return (ATTRIB_ID_APPROACH_RATE, ApproachRate);
        }

        public override void FromDatabaseAttributes(IReadOnlyDictionary<int, double> values, IBeatmapOnlineInfo onlineInfo)
        {
            base.FromDatabaseAttributes(values, onlineInfo);

            AimDifficulty = values[ATTRIB_ID_AIM];
            SpeedDifficulty = values[ATTRIB_ID_SPEED];
            OverallDifficulty = values[ATTRIB_ID_OVERALL_DIFFICULTY];
            ApproachRate = values[ATTRIB_ID_APPROACH_RATE];
            StarRating = values[ATTRIB_ID_DIFFICULTY];

            DrainRate = onlineInfo.DrainRate;
            HitCircleCount = onlineInfo.CircleCount;
            SliderCount = onlineInfo.SliderCount;
            SpinnerCount = onlineInfo.SpinnerCount;
        }
    }
}
