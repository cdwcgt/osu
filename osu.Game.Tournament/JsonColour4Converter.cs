// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Newtonsoft.Json;
using osu.Framework.Graphics;

namespace osu.Game.Tournament
{
    /// <summary>
    /// We made a change from using SixLabors.ImageSharp.Point to System.Drawing.Point at some stage.
    /// This handles converting to a standardised format on json serialize/deserialize operations.
    /// </summary>
    internal class JsonColour4Converter : JsonConverter<Colour4>
    {
        public override void WriteJson(JsonWriter writer, Colour4 value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.ToHex());
        }

        public override Colour4 ReadJson(JsonReader reader, Type objectType, Colour4 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string? str = (string?)reader.Value ?? "#FFFFFF";
            return Colour4.FromHex(str);
        }
    }
}
