// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using BanchoSharp.Interfaces;
using BanchoSharp.Messaging;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics.Backgrounds;
using osu.Game.Graphics.Containers;
using osu.Game.Overlays;
using osu.Game.Overlays.Mods;

namespace osu.Game.Screens.IrcBot
{
    public partial class IrcChannelList : Container
    {
        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private FillFlowContainer<IrcChannelListingItem> flow = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.Both;
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background4,
                },
                new OsuScrollContainer(Direction.Horizontal)
                {
                    RelativeSizeAxes = Axes.Both,
                    ScrollbarAnchor = Anchor.TopRight,
                    Children = new Drawable[]
                    {
                        flow = new FillFlowContainer<IrcChannelListingItem>
                        {
                            RelativeSizeAxes = Axes.Y,
                            Direction = FillDirection.Horizontal,
                            AutoSizeAxes = Axes.X,
                            Padding = new MarginPadding
                            {
                                Vertical = 13,
                                Horizontal = 15,
                            },
                        },
                        new AddRoomButton()
                        {
                            Origin = Anchor.TopLeft,
                            Anchor = Anchor.TopLeft,
                        }
                    },
                },
            };
        }

        public void UpdateAvailableChannels(IEnumerable<IChatChannel> newChannels)
        {
            flow.ChildrenEnumerable = new IrcChannelListingItem[]
            {
                new IrcChannelListingItem(new Channel("test1", false)),
                new IrcChannelListingItem(new Channel("test1", false)),
            };
        }
    }
}
