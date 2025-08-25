// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Newtonsoft.Json;
using osu.Game.Online.API;

namespace osu.Game.Tournament.Online.Requests.Responses
{
    public class MatchScore
    {
        [JsonProperty("user_id")]
        public int UserID { get; set; }

        [JsonProperty("match")]
        public MatchSlotInfo SlotInfo { get; set; } = null!;

        [JsonProperty("total_score")]
        public long Score { get; set; }

        [JsonProperty("mods")]
        public List<APIMod> Mods { get; set; } = new List<APIMod>();
    }

    public class MatchSlotInfo
    {
        [JsonProperty("slot")]
        public int SlotID { get; set; }

        [JsonProperty("team")]
        public string? Team { get; set; }

        [JsonProperty("pass")]
        public bool Pass { get; set; }
    }
}
