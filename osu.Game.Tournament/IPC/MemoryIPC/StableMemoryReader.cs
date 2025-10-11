// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Logging;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.IPC.MemoryIPC
{
    // Memory Pattern and Address are borrowed from tosu
    // https://github.com/tosuapp/tosu
    [SupportedOSPlatform("windows")]
    public class StableMemoryReader : MemoryReader
    {
        #region Addresses

        protected IntPtr GameBaseAddress;

        protected IntPtr RulesetsAddress;

        protected IntPtr PlayTimeAddress;

        protected IntPtr SpectatingUser;

        protected IntPtr ModsPointerAddress;

        #endregion

        public int PlayTime { get; private set; }

        public AttachStatus Status { get; private set; }

        public bool CheckInitialized()
        {
            if (Status != AttachStatus.Attached)
                return false;

            if (!IsAttached)
            {
                Status = AttachStatus.UnAttached;
                return false;
            }

            return true;
        }

        public Task<bool> AttachToProcessAsync(Process process) => Task.Run(() => AttachToProcess(process));

        public Task<bool> AttachToProcessByTitleNameAsync(string titleName) => Task.Run(() => AttachToProcessByTitleName(titleName));

        public override bool AttachToProcess(Process process)
        {
            Status = AttachStatus.UnAttached;

            if (!base.AttachToProcess(process))
                return false;

            ProcessModule? osuModule = Process?.MainModule;
            if (osuModule == null || osuModule.ModuleName != "osu!.exe")
                throw new InvalidOperationException("osu! module not found");

            return InitializeAddressAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        #region Pattern

        protected static readonly PatternInfo GAME_BASE_PATTERN = new PatternInfo("F8 01 74 04 83 65");

        protected static readonly PatternInfo RULESETS_PATTERN = new PatternInfo("7D 15 A1 ?? ?? ?? ?? 85 C0");

        protected static readonly PatternInfo PLAY_TIME_PATTERN = new PatternInfo("5E 5F 5D C3 A1 ?? ?? ?? ?? 89 ?? 04", +0x5);

        protected static readonly PatternInfo SPECTATING_USER_PATTERN = new PatternInfo("8B 0D ?? ?? ?? ?? 85 C0 74 05 8B 50 30", -0x4);

        protected static readonly PatternInfo MODS_POINTER_PATTERN = new PatternInfo("C8 FF ?? ?? ?? ?? ?? 81 0D ?? ?? ?? ?? ?? 08 00 00", 0x9);

        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private Task<bool>? initializeAddressTask;

        public Task<bool> InitializeAddressAsync()
        {
            if (!IsAttached)
                throw new InvalidOperationException("No process attached");

            if (CheckInitialized())
                return Task.FromResult(true);

            if (initializeAddressTask != null && !initializeAddressTask.IsCompleted)
                return initializeAddressTask;

            return initializeAddressTask = Task.Run(() =>
            {
                try
                {
                    Status = AttachStatus.Initializing;

                    var regions = QueryMemoryRegions(ProcessHandle);

                    InitializeAddressInternal(regions);

                    Status = AttachStatus.Attached;
                    return true;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[InitializeAsync] Failed: {ex.Message}");

                    Status = AttachStatus.UnAttached;
                    return false;
                }
            }, cts.Token);
        }

        protected virtual void InitializeAddressInternal(List<MemoryRegion> regions)
        {
            GameBaseAddress = ResolveFromPatternInfo(GAME_BASE_PATTERN, regions) ?? throw new InvalidOperationException("GameBase address not found");
            RulesetsAddress = ResolveFromPatternInfo(RULESETS_PATTERN, regions) ?? throw new InvalidOperationException("Ruleset address not found");
            PlayTimeAddress = ResolveFromPatternInfo(PLAY_TIME_PATTERN, regions) ?? throw new InvalidOperationException("PlayTime address not found");
            SpectatingUser = ResolveFromPatternInfo(SPECTATING_USER_PATTERN, regions) ?? throw new InvalidOperationException("Spectating user address not found");
            ModsPointerAddress = ResolveFromPatternInfo(MODS_POINTER_PATTERN, regions) ?? throw new InvalidOperationException("Mods pointer address not found");
        }

        #endregion

        public GameplayData? GetGameplayData()
        {
            if (!CheckInitialized())
                return null;

            // Ruleset = [[Rulesets - 0xB] + 0x4]
            IntPtr rulesetAddr = ReadInt32(ReadInt32(RulesetsAddress - 0xb) + 0x4);
            if (rulesetAddr == IntPtr.Zero)
                return null;

            IntPtr gameplayBaseAddr = ReadInt32(rulesetAddr + 0x68);
            if (gameplayBaseAddr == IntPtr.Zero)
                return null;

            IntPtr scoreAddr = ReadInt32(gameplayBaseAddr + 0x38);
            if (scoreAddr == IntPtr.Zero)
                return null;

            IntPtr hpBarAddr = ReadInt32(gameplayBaseAddr + 0x40);
            if (hpBarAddr == IntPtr.Zero)
                return null;

            // [[[Ruleset + 0x68] + 0x38] + 0x28]
            string? playerName = ReadSharpString(ReadInt32(scoreAddr + 0x28));

            LegacyMods mods = (LegacyMods)
                (ReadInt32(ReadInt32(scoreAddr + 0x1c) + 0xc) ^
                 ReadInt32(ReadInt32(scoreAddr + 0x1c) + 0x8));

            int modeId = ReadInt32(scoreAddr + 0x64);

            int score = ReadInt32(rulesetAddr + 0x100);
            double hpSmooth = ReadDouble(hpBarAddr + 0x14);
            double hp = ReadDouble(hpBarAddr + 0x1c);
            double acc = ReadDouble(ReadInt32(gameplayBaseAddr + 0x48) + 0xc);

            short hit100 = 0;
            short hit300 = 0;
            short hit50 = 0;
            short hitGeki = 0;
            short hitKatu = 0;
            short hitMiss = 0;
            short combo = 0;
            short maxCombo = 0;

            UpdatePlayTime();

            if (PlayTime > 1000)
            {
                hit100 = ReadShort(scoreAddr + 0x88);
                hit300 = ReadShort(scoreAddr + 0x8A);
                hit50 = ReadShort(scoreAddr + 0x8C);
                hitGeki = ReadShort(scoreAddr + 0x8E);
                hitKatu = ReadShort(scoreAddr + 0x90);
                hitMiss = ReadShort(scoreAddr + 0x92);
                combo = ReadShort(scoreAddr + 0x94);
                maxCombo = ReadShort(scoreAddr + 0x68);
            }

            if (playerName == null)
                return null;

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

        public TournamentUser? GetTournamentUser()
        {
            if (!CheckInitialized())
                return null;

            IntPtr userAddr = ReadInt32(ReadInt32(SpectatingUser));

            if (userAddr == IntPtr.Zero)
                return null;

            TournamentUser tournamentUser = new TournamentUser
            {
                Username = ReadSharpString(ReadInt32(userAddr + 0x30)) ?? string.Empty,
                OnlineID = ReadInt32(userAddr + 0x70)
            };

            return tournamentUser;
        }

        public void UpdatePlayTime()
        {
            if (!CheckInitialized())
                return;

            try
            {
                PlayTime = ReadInt32(ReadInt32(PlayTimeAddress));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to update playtime");
            }
        }

        public int GetBeatmapId()
        {
            if (!CheckInitialized())
                return -1;

            IntPtr beatmapAddr = ReadInt32(ReadInt32(GameBaseAddress - 0xc));

            if (beatmapAddr == IntPtr.Zero)
                return -1;

            return ReadInt32(beatmapAddr + 0xc8);
        }

        public LegacyMods GetMods()
        {
            if (!CheckInitialized())
                return LegacyMods.None;

            return (LegacyMods)ReadInt32(ReadInt32(ModsPointerAddress));
        }

        public string? ReadSharpString(IntPtr objectAddress)
        {
            if (objectAddress == IntPtr.Zero)
                return null;

            const int offset = 4;
            int length = ReadInt32(objectAddress + offset);
            if (length <= 0 || length > 4096)
                return null;

            byte[] buffer = ReadBytes(objectAddress + offset + 4, length * 2);
            return Encoding.Unicode.GetString(buffer).TrimEnd('\0');
        }

        protected override void Dispose(bool disposing)
        {
            cts.Cancel();

            base.Dispose(disposing);
        }
    }

    public enum AttachStatus
    {
        UnAttached,
        Initializing,
        Attached,
    }
}
