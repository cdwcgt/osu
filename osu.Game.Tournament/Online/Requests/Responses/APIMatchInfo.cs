// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Tournament.Online.Requests.Responses
{
    public class APIMatchInfo
    {
        [JsonProperty("match")]
        public APIMatch APIMatch { get; set; } = null!;

        [JsonProperty("events")]
        public APIMatchEvent[] Events { get; set; } = null!;

        [JsonProperty("users")]
        public APIUser[] Users { get; set; } = null!;

        [JsonProperty("first_event_id")]
        public long FirstEventID { get; set; }

        [JsonProperty("last_event_id")]
        public long LastEventID { get; set; }
    }
}
