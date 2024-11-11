// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Text.Json.Serialization;

namespace osu.Game.Tournament.Online.Requests.Responses
{
    public class APIMatchEvent
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("detail")]
        public MatchEventDetail Detail { get; set; } = null!;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("user_id")]
        public int? UserID { get; set; }

        [JsonPropertyName("game")]
        public APIMatchGame? Game { get; set; }
    }

    public class MatchEventDetail
    {
        [JsonPropertyName("type")]
        public MatchEventType Type { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    public enum MatchEventType
    {
        [JsonPropertyName("host-changed")]
        HostChanged,

        [JsonPropertyName("match-created")]
        MatchCreated,

        [JsonPropertyName("match-disbanded")]
        MatchDisbanded,

        [JsonPropertyName("other")]
        Other,

        [JsonPropertyName("player-joined")]
        PlayerJoined,

        [JsonPropertyName("player-kicked")]
        PlayerKicked,

        [JsonPropertyName("player-left")]
        PlayerLeft
    }
}
