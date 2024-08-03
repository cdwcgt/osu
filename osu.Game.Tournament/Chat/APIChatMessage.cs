// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace osu.Game.Tournament.Chat
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class APIChatMessage
    {
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public double Timestamp { get; set; }
    }
}
