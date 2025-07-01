// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using osu.Framework.Logging;
using osu.Game.Beatmaps.Legacy;

namespace osu.Game.Tournament.IPC.MemoryIPC
{
    [SupportedOSPlatform("windows")]
    public class StableMemoryReader
    {
        private readonly MemoryReader memoryReader;

        #region Addresses

        private IntPtr GameBaseAddress;

        private IntPtr RulesetsAddress;

        private IntPtr PlayTimeAddress;

        #endregion

        private int playTime;

        public StableMemoryReader(MemoryReader memoryReader)
        {
            this.memoryReader = memoryReader;

            ProcessModule? osuModule = memoryReader.Process?.MainModule;
            if (osuModule == null || osuModule.ModuleName != "osu!.exe")
                throw new InvalidOperationException("osu! module not found");

            initializeAddress();
        }

        #region Pattern

        private readonly PatternInfo GameBasePattern = new PatternInfo("F8 01 74 04 83 65");

        private readonly PatternInfo RulesetsPattern = new PatternInfo("7D 15 A1 ?? ?? ?? ?? 85 C0");

        private readonly PatternInfo PlayTimePattern = new PatternInfo("5E 5F 5D C3 A1 ?? ?? ?? ?? 89 ?? 04");

        private void initializeAddress()
        {
            GameBaseAddress = memoryReader.ResolveFromPatternInfo(GameBasePattern) ?? throw new InvalidOperationException("GameBase address not found");
            RulesetsAddress = memoryReader.ResolveFromPatternInfo(RulesetsPattern) ?? throw new InvalidOperationException("Ruleset address not found");
            PlayTimeAddress = memoryReader.ResolveFromPatternInfo(PlayTimePattern) ?? throw new InvalidOperationException("PlayTime address not found");
        }

        #endregion

        public GameplayData GetGameplayData()
        {
            // Ruleset = [[Rulesets - 0xB] + 0x4]
            IntPtr rulesetAddr = memoryReader.ReadInt32(memoryReader.ReadInt32(RulesetsAddress - 0xb) + 0x4);
            if (rulesetAddr == IntPtr.Zero)
                throw new InvalidOperationException("Ruleset address not found");

            IntPtr gameplayBaseAddr = memoryReader.ReadInt32(rulesetAddr + 0x68);
            if (gameplayBaseAddr == IntPtr.Zero)
                throw new InvalidOperationException("GameplayBase address not found");

            IntPtr scoreAddr = memoryReader.ReadInt32(gameplayBaseAddr + 0x38);
            if (scoreAddr == IntPtr.Zero)
                throw new InvalidOperationException("Score address not found");

            IntPtr hpBarAddr = memoryReader.ReadInt32(gameplayBaseAddr + 0x40);
            if (hpBarAddr == IntPtr.Zero)
                throw new InvalidOperationException("HPBar address not found");

            // [[[Ruleset + 0x68] + 0x38] + 0x28]
            string playerName = ReadSharpString(memoryReader.ReadInt32(scoreAddr + 0x28))!;

            LegacyMods mods = (LegacyMods)
                (memoryReader.ReadInt32(memoryReader.ReadInt32(scoreAddr + 0x1c) + 0xc) ^
                 memoryReader.ReadInt32(memoryReader.ReadInt32(scoreAddr + 0x1c) + 0x8));

            int modeId = memoryReader.ReadInt32(scoreAddr + 0x64);

            int score = memoryReader.ReadInt32(rulesetAddr + 0x100);
            double hpSmooth = memoryReader.ReadDouble(hpBarAddr + 0x14);
            double hp = memoryReader.ReadDouble(hpBarAddr + 0x1c);
            double acc = memoryReader.ReadDouble(gameplayBaseAddr + 0x48) + 0xc;

            short hit100 = 0;
            short hit300 = 0;
            short hit50 = 0;
            short hitGeki = 0;
            short hitKatu = 0;
            short hitMiss = 0;
            short combo = 0;
            short maxCombo = 0;

            if (playTime > 1000)
            {
                hit100 = memoryReader.ReadShort(scoreAddr + 0x88);
                hit300 = memoryReader.ReadShort(scoreAddr + 0x8A);
                hit50 = memoryReader.ReadShort(scoreAddr + 0x8C);
                hitGeki = memoryReader.ReadShort(scoreAddr + 0x8E);
                hitKatu = memoryReader.ReadShort(scoreAddr + 0x90);
                hitMiss = memoryReader.ReadShort(scoreAddr + 0x92);
                combo = memoryReader.ReadShort(scoreAddr + 0x94);
                maxCombo = memoryReader.ReadShort(scoreAddr + 0x68);
            }

            return new GameplayData
            {
                PlayerName = playerName,
                Accuracy = acc,
                Mods = mods,
                RulesetId = modeId,
                Score = score,
                PlayerHPSmooth = hpSmooth,
                PlayerHP = hp,
                Hit100 = hit100,
                Hit300 = hit300,
                Hit50 = hit50,
                HitGeki = hitGeki,
                HitKatu = hitKatu,
                HitMiss = hitMiss,
                Combo = combo,
                MaxCombo = maxCombo
            };
        }

        public void UpdatePlayTime()
        {
            try
            {
                playTime = memoryReader.ReadInt32(PlayTimeAddress + 0x5);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to update playtime");
            }
        }

        public string? ReadSharpString(IntPtr objectAddress)
        {
            if (objectAddress == IntPtr.Zero)
                return null;

            const int offset = 4;
            int length = memoryReader.ReadInt32(objectAddress + offset);
            if (length <= 0 || length > 4096)
                return null;

            byte[] buffer = memoryReader.ReadBytes(objectAddress + offset + 4, length * 2);
            return Encoding.Unicode.GetString(buffer).TrimEnd('\0');
        }
    }
}
