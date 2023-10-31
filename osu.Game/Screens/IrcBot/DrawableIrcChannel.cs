// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using BanchoSharp;
using BanchoSharp.Interfaces;
using BanchoSharp.Messaging.ChatMessages;
using BanchoSharp.Multiplayer;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.Cursor;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays.Chat;

namespace osu.Game.Screens.IrcBot
{
    public partial class DrawableIrcChannel : Container
    {
        public IChatChannel Channel;

        public bool isMatchChannel => Channel is MultiplayerLobby;
        private FillFlowContainer chatFlow = null!;

        private ChatTextBox input = null!;

        [Resolved]
        private BanchoClient bancho { get; set; } = null!;

        public DrawableIrcChannel(IChatChannel channel)
        {
            Channel = channel;
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Children = new Drawable[]
            {
                new OsuContextMenuContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    Child = new ChannelScrollContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Bottom = 30f },
                        Child = chatFlow = new FillFlowContainer
                        {
                            Padding = new MarginPadding { Horizontal = 10f },
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                        }
                    },
                },
                input = new ChatTextBox
                {
                    Padding = new MarginPadding { Left = 150 },
                    RelativeSizeAxes = Axes.X,
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                }
            };

            input.OnCommit += (textBox, isNew) =>
            {
                SendMssage(textBox.Text);
                textBox.Text = string.Empty;
            };
        }

        public void SendMssage(string message)
        {
            bancho.SendPrivateMessageAsync(Channel.ChannelName, message);
            UpdateMessage();
        }

        public void UpdateMessage()
        {
            Scheduler.AddOnce(() =>
            {
                chatFlow.ChildrenEnumerable = Channel.MessageHistory!
                                                     .OfType<IPrivateIrcMessage>()
                                                     .Select(m => new IrcChatLine(m));
            });
        }
    }
}
