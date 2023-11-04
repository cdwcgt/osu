// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Newtonsoft.Json;
using osu.Game.Rulesets.Difficulty;

namespace osu.Game.Rulesets.Osu.Difficulty
{
    public class OsuPerformanceAttributes : PerformanceAttributes
    {
        [JsonProperty("aim")]
        public double Aim { get; set; }

        [JsonProperty("jump aim")]
        public double JumpAim { get; set; }

        [JsonProperty("flow aim")]
        public double FlowAim { get; set; }

        [JsonProperty("precision")]
        public double Precision { get; set; }

        [JsonProperty("speed")]
        public double Speed { get; set; }

        [JsonProperty("stamina")]
        public double Stamina { get; set; }

        [JsonProperty("accuracy")]
        public double Accuracy { get; set; }

        public override IEnumerable<PerformanceDisplayAttribute> GetAttributesForDisplay()
        {
            foreach (var attribute in base.GetAttributesForDisplay())
                yield return attribute;

            yield return new PerformanceDisplayAttribute(nameof(Aim), "Aim", Aim);
            yield return new PerformanceDisplayAttribute(nameof(JumpAim), "Jump Aim", JumpAim);
            yield return new PerformanceDisplayAttribute(nameof(FlowAim), "Flow Aim", FlowAim);
            yield return new PerformanceDisplayAttribute(nameof(Precision), "Precision", Precision);
            yield return new PerformanceDisplayAttribute(nameof(Speed), "Speed", Speed);
            yield return new PerformanceDisplayAttribute(nameof(Stamina), "Stamina", Stamina);
            yield return new PerformanceDisplayAttribute(nameof(Accuracy), "Accuracy", Accuracy);
        }
    }
}
