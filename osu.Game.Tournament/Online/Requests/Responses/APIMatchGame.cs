// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Tournament.Online.Requests.Responses
{
    public class APIMatchGame
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("beatmap_id")]
        public int BeatmapId { get; set; }

        [JsonPropertyName("start_time")]
        public DateTimeOffset StartTime { get; set; }

        [JsonPropertyName("end_time")]
        public DateTimeOffset? EndTime { get; set; }

        [JsonPropertyName("ruleset_id")]
        public int RulesetId { get; set; }

        [JsonPropertyName("mods")]
        public List<string> Mods { get; set; }

        [JsonPropertyName("scores")]
        public List<SoloScoreInfo> Scores { get; set; } = null!;
    }
}
