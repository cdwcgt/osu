// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays.Settings;
using osuTK;

namespace osu.Game.Screens.IrcBot
{
    public partial class AddChannelPopover : OsuPopover
    {
        private OsuTextBox channelName;

        public AddChannelPopover(Action<string> joinChannel)
        {
            Child = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Y,
                Width = 300,
                Padding = new MarginPadding { Horizontal = 20 },
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0f, 14f),
                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Text = "Add Channel",
                        Font = OsuFont.GetFont(weight: FontWeight.Bold),
                    },
                    channelName = new OsuTextBox
                    {
                        RelativeSizeAxes = Axes.X,
                        PlaceholderText = "Channel Name",
                        TabbableContentContainer = this
                    },
                    new ShearedButton
                    {
                        Text = "Join",
                        Action = () => joinChannel.Invoke(channelName.Text)
                    }
                }
            };
        }
    }
}
