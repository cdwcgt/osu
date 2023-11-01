// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using BanchoSharp;
using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Screens.IrcBot.MatchChannelItem;

namespace osu.Game.Screens.IrcBot
{
    public partial class DrawableIrcMatchChannel : DrawableIrcChannel
    {
        public new MultiplayerLobby Channel => (MultiplayerLobby)base.Channel;

        private FillFlowContainer playerList = null!;

        [Resolved]
        private OsuColour colour { get; set; } = null!;

        [Resolved]
        private BanchoClient bancho { get; set; } = null!;

        public DrawableIrcMatchChannel(IChatChannel channel)
            : base(channel)
        {
            if (!isMatchChannel)
                throw new ArgumentException($"{nameof(channel)} is not a Match channel");
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            MainContainer.Padding = new MarginPadding { Right = 300f };
            FullContainer.Add(new Container
            {
                Width = 300f,
                RelativeSizeAxes = Axes.Y,
                Anchor = Anchor.CentreRight,
                Origin = Anchor.CentreRight,
                Children = new Drawable[]
                {
                    new Container
                    {
                        Height = 0.5f,
                        RelativeSizeAxes = Axes.Both,
                        CornerRadius = 5f,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = colour.Gray9
                            },
                            playerList = new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Direction = FillDirection.Vertical
                            }
                        }
                    }
                }
            });

            UpdateMatchInformation();
        }

        public void UpdateMatchInformation()
        {
            Channel.RefreshSettingsAsync().WaitSafely();

            playerList.ChildrenEnumerable = Channel.Players.Select(p => new PlayerPanel(p));
        }
    }
}
