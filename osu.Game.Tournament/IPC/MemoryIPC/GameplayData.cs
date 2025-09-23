// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Beatmaps.Legacy;

namespace osu.Game.Tournament.IPC.MemoryIPC
{
    public record struct GameplayData()
    {
        public string PlayerName = string.Empty;
        public LegacyMods Mods = LegacyMods.None;
        public int RulesetId = 0;
        public int Score = 0;
        public double PlayerHPSmooth = 0;
        public double PlayerHP = 0;
        public double Accuracy = 0;
        public int Hit100 = 0;
        public int Hit300 = 0;
        public int Hit50 = 0;
        public int HitGeki = 0;
        public int HitKatu = 0;
        public int HitMiss = 0;
        public int Combo = 0;
        public int MaxCombo = 0;
    }
}
