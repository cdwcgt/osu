// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;

namespace osu.Game.Screens.IrcBot
{
    public partial class AddRoomButton : IconButton
    {
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
        }
    }
}
