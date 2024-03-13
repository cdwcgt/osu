// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;
using osu.Framework.Graphics;

namespace osu.Game.Tournament.Models
{
    public class RoundBeatmap
    {
        public int ID;
        public string Mods = string.Empty;
        public Colour4 BackgroundColor = Colour4.Black;
        public Colour4 TextColor = Colour4.White;

        [JsonProperty("BeatmapInfo")]
        public TournamentBeatmap? Beatmap;
    }
}
