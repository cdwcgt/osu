// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;

namespace osu.Game.Tournament.Models
{
    public class RoundBeatmap
    {
        public int ID;
        public string Mods = string.Empty;

        public MapGameMode MapGameMode = MapGameMode.Standard;

        [JsonProperty("BeatmapInfo")]
        public TournamentBeatmap? Beatmap;
    }

    public enum MapGameMode
    {
        Standard,
        Taiko,
        Catch,
        Mania,
    }
}
