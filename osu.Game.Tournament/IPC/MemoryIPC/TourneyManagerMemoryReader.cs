// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;

namespace osu.Game.Tournament.IPC.MemoryIPC
{
    // Memory Pattern and Address are borrowed from tosu
    // https://github.com/tosuapp/tosu
    public class TourneyManagerMemoryReader : StableMemoryReader
    {
        // found with PercuDan54
        private static readonly PatternInfo channel_id_pattern = new PatternInfo("8B CE BA 07 00 00 00 E8 ?? ?? ?? ?? A3 ?? ?? ?? ?? 89 15 ?? ?? ?? ?? E8", 0xd);

        private IntPtr channelAddress;

        protected override void InitializeAddressInternal(List<MemoryRegion> regions)
        {
            base.InitializeAddressInternal(regions);

            channelAddress = ResolveFromPatternInfo(channel_id_pattern, regions) ?? throw new InvalidOperationException("Channel ID Address not found");
        }

        public TourneyState GetTourneyState()
        {
            if (!CheckInitialized())
                return TourneyState.Initialising;

            IntPtr rulesetAddr = ReadInt32(ReadInt32(RulesetsAddress - 0xb) + 0x4);
            if (rulesetAddr == IntPtr.Zero)
                return TourneyState.Initialising;

            return (TourneyState)ReadInt32(rulesetAddr + 0x54);
        }

        public long GetChannelId()
        {
            if (!CheckInitialized())
                return -1;

            IntPtr channelIdAddress = ReadInt32(channelAddress);

            return ReadInt64(channelIdAddress);
        }
    }
}
