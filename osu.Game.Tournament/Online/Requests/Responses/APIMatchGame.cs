// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace osu.Game.Tournament.Online.Requests.Responses
{
    public class APIMatchGame
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("beatmap_id")]
        public int BeatmapId { get; set; }

        [JsonProperty("start_time")]
        public DateTimeOffset StartTime { get; set; }

        [JsonProperty("end_time")]
        public DateTimeOffset? EndTime { get; set; }

        [JsonProperty("ruleset_id")]
        public int RulesetId { get; set; }

        [JsonProperty("mods")]
        public List<string>? Mods { get; set; }

        [JsonProperty("scores")]
        public List<MatchScore> Scores { get; set; } = null!;
    }
}
