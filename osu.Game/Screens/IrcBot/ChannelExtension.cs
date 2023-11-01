// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;

namespace osu.Game.Screens.IrcBot
{
    public static class ChannelExtension
    {
        public static bool IsMatchChannel(this IChatChannel channel) => channel is MultiplayerLobby;
    }
}
