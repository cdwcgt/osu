// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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
    public partial class AddRoomButton : IconButton, IHasPopover
    {
        [Resolved]
        private IrcBotScreen screen { get; set; } = null!;

        public AddRoomButton()
        {
            Icon = FontAwesome.Solid.Plus;
            TooltipText = "Add room";
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            Colour = colours.Gray4;
            IconHoverColour = colours.Green;

            Action = () =>
            {


                this.ShowPopover();
            };
        }

        public Popover? GetPopover()
        {


            return new AddChannelPopover(screen.TryJoinChannel);
        }
    }
}
