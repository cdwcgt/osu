// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BanchoSharp.Interfaces;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;

namespace osu.Game.Screens.IrcBot
{
    public partial class IrcChannelListingItem : OsuClickableContainer
    {
        public IChatChannel Channel;
        private OsuSpriteText channelText;
        private const float text_size = 18;

        [Resolved]
        private Bindable<IChatChannel?> currentIrcChannel { get; set; } = null!;

        public IrcChannelListingItem(IChatChannel channel)
        {
            Channel = channel;
            Width = 300;
            Height = 80;

            Children = new Drawable[]
            {
                channelText = new OsuSpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Text = Channel.ChannelName,
                    Font = OsuFont.Torus.With(size: text_size, weight: FontWeight.SemiBold),
                    Margin = new MarginPadding { Bottom = 2 },
                },
            };

            Action = () =>
            {
                currentIrcChannel.Value = Channel;
            };
        }
    }
}
