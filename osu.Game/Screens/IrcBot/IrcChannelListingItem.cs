// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BanchoSharp.Interfaces;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;

namespace osu.Game.Screens.IrcBot
{
    public partial class IrcChannelListingItem : OsuClickableContainer
    {
        public IChatChannel Channel;
        private Box background = null!;
        private OsuSpriteText channelText = null!;
        private const float text_size = 18;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [Resolved]
        private Bindable<IChatChannel?> currentIrcChannel { get; set; } = null!;

        public IrcChannelListingItem(IChatChannel channel)
        {
            Channel = channel;

            Action = () =>
            {
                currentIrcChannel.Value = Channel;
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Width = 200;
            RelativeSizeAxes = Axes.Y;

            Children = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Dark3,
                },
                channelText = new OsuSpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Text = Channel.ChannelName,
                    Font = OsuFont.Torus.With(size: text_size, weight: FontWeight.SemiBold),
                    Padding = new MarginPadding { Left = 10 }
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            currentIrcChannel.BindValueChanged(s =>
            {
                background.Colour = s.NewValue == Channel ? colourProvider.Foreground1 : colourProvider.Dark3;
            });
        }
    }
}
