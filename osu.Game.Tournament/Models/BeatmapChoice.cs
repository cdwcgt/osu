// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using osu.Framework.Bindables;

namespace osu.Game.Tournament.Models
{
    /// <summary>
    /// A beatmap choice by a team from a tournament's map pool.
    /// </summary>
    [Serializable]
    public class BeatmapChoice
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public TeamColour Team;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public ChoiceType Type;

        public Bindable<TeamColour?> Winner = new Bindable<TeamColour?>();

        public int BeatmapID;

        public bool CalculatedByApi;
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum TeamColour
    {
        Red,
        Blue
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ChoiceType
    {
        Pick,
        Ban,
        Protected,
    }

    public static class BeatmapChoiceExtensions
    {
        public static bool IsConsumed(this BeatmapChoice choice) => choice.Type == ChoiceType.Pick || choice.Type == ChoiceType.Ban;
    }
}
