// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;

namespace osu.Game.Screens.IrcBot
{
    public partial class AddMatchButton : AddChannelButton
    {
        public AddMatchButton()
        {
            Icon = FontAwesome.Solid.Chess;
            TooltipText = "Create Match";
        }

        protected override Popover getPopover()
        {
            return new AddMatchPopover();
        }
    }
}
