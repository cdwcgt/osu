// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using BanchoSharp;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;

namespace osu.Game.Screens.IrcBot
{
    public partial class AddChannelButton : IconButton, IHasPopover
    {
        [Resolved]
        private BanchoClient bancho { get; set; } = null!;

        public AddChannelButton()
        {
            Icon = FontAwesome.Solid.Plus;
            TooltipText = "Add channel";
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            Colour = colours.Gray4;
            IconHoverColour = colours.Green;

            Action = () =>
            {
                if (!bancho.IsAuthenticated)
                    return;

                this.ShowPopover();
            };
        }

        public Popover? GetPopover()
        {
            if (!bancho.IsAuthenticated)
                return null;

            return getPopover();
        }

        protected virtual Popover getPopover() => new AddChannelPopover();
    }
}
