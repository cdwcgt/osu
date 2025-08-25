// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Tournament.Online.Requests.Responses
{
    public class MatchScore : SoloScoreInfo
    {
        [JsonProperty("match")]
        public MatchSlotInfo SlotInfo { get; set; } = null!;
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
