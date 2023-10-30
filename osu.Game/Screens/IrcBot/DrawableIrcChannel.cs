// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BanchoSharp.Interfaces;
using osu.Framework.Graphics.Containers;

namespace osu.Game.Screens.IrcBot
{
    public partial class DrawableIrcChannel : Container
    {
        public IChatChannel Channel;

        public DrawableIrcChannel(IChatChannel channel)
        {
            Channel = channel;
        }
    }
}
