// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Beatmaps.Legacy;

namespace osu.Game.Tournament.IPC.MemoryIPC
{
    public class GameplayData
    {
        public string PlayerName = string.Empty;
        public LegacyMods Mods;
        public int RulesetId;
        public int Score;
        public double PlayerHPSmooth;
        public double PlayerHP;
        public double Accuracy;
        public int Hit100;
        public int Hit300;
        public int Hit50;
        public int HitGeki;
        public int HitKatu;
        public int HitMiss;
        public int Combo;
        public int MaxCombo;
    }
}
