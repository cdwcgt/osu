// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Newtonsoft.Json;
using osu.Game.Tournament.Online.Requests.Responses;

namespace osu.Game.Tournament.Online
{
    public class MatchEventTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(MatchEventType);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            string? enumText = reader.Value?.ToString();

            if (string.IsNullOrEmpty(enumText))
                return MatchEventType.Unknown;

            // 去掉破折号并将字符串转为 PascalCase 格式
            string formattedText = enumText.Replace("-", "");

            if (Enum.TryParse(typeof(MatchEventType), formattedText, ignoreCase: true, out object? parsedEnum))
            {
                return parsedEnum;
            }

            return MatchEventType.Unknown;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is MatchEventType enumValue)
            {
                // 将枚举值转换为带破折号的小写字符串
                string jsonValue = enumValue.ToString()
                                            .Replace("MatchCreated", "match-created")
                                            .Replace("HostChanged", "host-changed")
                                            .Replace("MatchDisbanded", "match-disbanded")
                                            .Replace("PlayerJoined", "player-joined")
                                            .Replace("PlayerKicked", "player-kicked")
                                            .Replace("PlayerLeft", "player-left")
                                            .Replace("Other", "other");

                writer.WriteValue(jsonValue);
            }
        }
    }
}
