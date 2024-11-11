// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;

namespace osu.Game.Tournament.Online.Requests.Responses
{
    public class MatchScore
    {
        [JsonProperty("user_id")]
        public int UserID { get; set; }

        [JsonProperty("match")]
        public MatchSlotInfo SlotInfo { get; set; } = null!;

        [JsonProperty("score")]
        public long Score { get; set; }
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
