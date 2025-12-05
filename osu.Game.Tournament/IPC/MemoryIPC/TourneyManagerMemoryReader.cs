// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using osu.Framework.Logging;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Chat;

namespace osu.Game.Tournament.IPC.MemoryIPC
{
    // Memory Pattern and Address are borrowed from tosu
    // https://github.com/tosuapp/tosu
    public class TourneyManagerMemoryReader : StableMemoryReader
    {
        // found with PercyDan54
        private static readonly PatternInfo channel_id_pattern = new PatternInfo("8B CE BA 07 00 00 00 E8 ?? ?? ?? ?? A3 ?? ?? ?? ?? 89 15 ?? ?? ?? ?? E8", 0xd);

        private static readonly PatternInfo chat_area_pattern = new PatternInfo("A1 ?? ?? ?? ?? 89 45 F0 8B D1 85 C9 75");

        private IntPtr? channelAddress;

        private IntPtr? chatAreaAddress;

        protected override void InitializeAddressInternal(List<MemoryRegion> regions)
        {
            base.InitializeAddressInternal(regions);

            Task.Factory.StartNew(async () =>
            {
                while (channelAddress == null)
                {
                    channelAddress = ResolveFromPatternInfo(channel_id_pattern, regions);
                    await Task.Delay(1000).ConfigureAwait(false);
                }
            }, TaskCreationOptions.LongRunning);

            Task.Factory.StartNew(async () =>
            {
                while (chatAreaAddress == null)
                {
                    chatAreaAddress = ResolveFromPatternInfo(chat_area_pattern, regions);
                    await Task.Delay(1000).ConfigureAwait(false);
                }
            }, TaskCreationOptions.LongRunning);
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
            if (!CheckInitialized() || channelAddress == null)
                return -1;

            IntPtr channelIdAddress = ReadInt32(channelAddress.Value);

            return ReadInt64(channelIdAddress);
        }

        public List<Message>? GetTourneyChat(int messageSize = -1)
        {
            if (!CheckInitialized() || chatAreaAddress == null)
                return null;

            IntPtr channelsList = ReadInt32(ReadInt32(chatAreaAddress.Value + 0x1));
            IntPtr channelsItems = ReadInt32(channelsList + 0x4);
            int channelsLength = ReadInt32(channelsItems + 0x4);

            if (channelsLength == 0)
            {
                return null;
            }

            try
            {
                for (int i = channelsLength - 1; i >= 0; i--)
                {
                    IntPtr currentChannelPointer = channelsItems + 0x8 + 0x4 * i;

                    IntPtr currentChannel = ReadInt32(currentChannelPointer);

                    if (currentChannel == IntPtr.Zero)
                        continue;

                    string chatTag = ReadSharpString(ReadInt32(currentChannel + 0x4)) ?? string.Empty;

                    if (chatTag != "#multiplayer")
                        continue;

                    var result = new List<Message>();

                    IntPtr messagesAddr = ReadInt32(currentChannel + 0x10);
                    IntPtr messagesItems = ReadInt32(messagesAddr + 0x4);

                    int messageLength = ReadInt32(messagesAddr + 0xc);

                    if (messageSize == messageLength)
                    {
                        continue;
                    }

                    for (int m = 0; m < messageLength; m++)
                    {
                        IntPtr currentMessagePointer = messagesItems + 0x8 + 0x4 * m;
                        IntPtr currentMessage = ReadInt32(currentMessagePointer);

                        string content = ReadSharpString(ReadInt32(currentMessage + 0x4)) ?? string.Empty;

                        if (content == string.Empty)
                            continue;

                        string[] timeAndName = (ReadSharpString(ReadInt32(currentMessage + 0x8)) ?? string.Empty).Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

                        if (timeAndName.Length != 2)
                            continue;

                        string timePart = timeAndName[0].Trim();
                        string namePart = timeAndName[1].Trim();

                        if (!DateTimeOffset.TryParse(timePart, out var time))
                            continue;

                        if (namePart.EndsWith(':'))
                            namePart = namePart[..^1];

                        bool banchoBot = namePart.Equals("Banchobot", StringComparison.OrdinalIgnoreCase);

                        result.Add(new TourneyMessage
                        {
                            Timestamp = time,
                            Sender = new APIUser
                            {
                                IsBot = banchoBot,
                                Username = namePart,
                                Colour = banchoBot ? "#e45678" : string.Empty
                            },
                            Content = content,
                        });
                    }

                    return result;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to get message");
            }

            return null;
        }

        private class TourneyMessage : Message
        {
            public override bool Equals(Message? other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;

                return Timestamp == other.Timestamp && Sender.Username == other.Sender.Username && Content == other.Content;
            }
        }
    }
}
