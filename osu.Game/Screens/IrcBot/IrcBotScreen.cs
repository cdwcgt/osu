// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Threading.Tasks;
using BanchoSharp;
using BanchoSharp.Interfaces;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Screens;
using osu.Game.Overlays;

namespace osu.Game.Screens.IrcBot
{
    [Cached]
    public partial class IrcBotScreen : OsuScreen
    {
        [Cached]
        private readonly BanchoClient banchoClient = new BanchoClient();

        private IrcLogin loginPanel;
        private IrcChannelList roomTab;
        private readonly Container<DrawableIrcChannel> currentChannelContainer;

        public BindableBool IsLogin = new BindableBool();
        private readonly BindableDictionary<IChatChannel, DrawableIrcChannel> loadedIrcChannel = new BindableDictionary<IChatChannel, DrawableIrcChannel>();

        [Cached]
        private readonly Bindable<IChatChannel?> currentIrcChannel = new Bindable<IChatChannel?>();

        [Cached]
        private readonly OverlayColourProvider overlayColourProvider = new OverlayColourProvider(OverlayColourScheme.Orange);

        public IrcBotScreen()
        {
            InternalChildren = new Drawable[]
            {
                new PopoverContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            Colour = overlayColourProvider.Background6,
                            RelativeSizeAxes = Axes.Both,
                        },
                        new AddRoomButton
                        {
                            Origin = Anchor.TopLeft,
                            Anchor = Anchor.TopLeft,
                        },
                        new FillFlowContainer
                        {
                            Origin = Anchor.TopLeft,
                            Anchor = Anchor.TopLeft,
                            Name = "Tab",
                            Height = 80f,
                            RelativeSizeAxes = Axes.X,
                            Direction = FillDirection.Horizontal,
                            Padding = new MarginPadding { Top = 30f },
                            Children = new Drawable[]
                            {
                                roomTab = new IrcChannelList
                                {
                                    Origin = Anchor.TopLeft,
                                    Anchor = Anchor.TopLeft,
                                    Name = "Room Tab",
                                    Padding = new MarginPadding { Right = 35 }
                                },
                            }
                        },
                        currentChannelContainer = new Container<DrawableIrcChannel>
                        {
                            Name = "Main",
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Top = 80f },
                        },
                        loginPanel = new IrcLogin
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            FireLogin = loginToBancho
                        }
                    }
                }
            };

            IsLogin.BindValueChanged(v =>
            {
                if (v.NewValue)
                {
                    Schedule(() => loginPanel.Hide());
                }
                else
                {
                    Schedule(() => loginPanel.Show());
                }
            });

            banchoClient.OnAuthenticated += () =>
            {
                IsLogin.Value = true;
            };

            banchoClient.OnDisconnected += () =>
            {
                IsLogin.Value = false;
            };

            banchoClient.OnChannelParted += onLeaveChannel;

            loadedIrcChannel.BindCollectionChanged((_, _) => roomTab.UpdateAvailableChannels(banchoClient.Channels));

            banchoClient.OnChannelJoined += onNewChannel;

            banchoClient.OnMessageReceived += onNewMessage;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            roomTab.UpdateAvailableChannels(banchoClient.Channels);

            currentIrcChannel.BindValueChanged(c =>
            {
                if (c.NewValue == null)
                {
                    currentChannelContainer.Clear(false);
                    return;
                }

                if (currentChannelContainer.Child.Channel == c.NewValue)
                    return;

                if (loadedIrcChannel.TryGetValue(c.NewValue, out var drawablechannel))
                {
                    currentChannelContainer.Clear(false);
                    currentChannelContainer.Add(drawablechannel);
                }
            });
        }

        private void loginToBancho(string username, string password)
        {
            if (banchoClient.IsConnected)
                banchoClient.DisconnectAsync();

            banchoClient.ClientConfig = new BanchoClientConfig(
                new IrcCredentials(username, password));

            banchoClient.ConnectAsync();
        }

        private void onNewChannel(IChatChannel channel)
        {
            if (loadedIrcChannel.ContainsKey(channel))
                return;

            var drawableChannel = new DrawableIrcChannel(channel);
            loadedIrcChannel.Add(channel, drawableChannel);

            LoadComponentAsync(drawableChannel, ircChannel =>
            {
                currentChannelContainer.Clear(false);
                currentChannelContainer.Add(ircChannel);
                currentIrcChannel.Value = channel;
            });
        }

        private void onLeaveChannel(IChatChannel channel)
        {
            loadedIrcChannel.Remove(channel);
        }

        private void onNewMessage(IIrcMessage message)
        {
            if (message is not IPrivateIrcMessage) return;

            foreach (var ch in loadedIrcChannel.Values)
            {
                ch.UpdateMessage();
            }
        }

        public void TryJoinChannel(string channelName)
        {
            if (!IsLogin.Value)
                return;

            banchoClient.JoinChannelAsync(channelName);
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            banchoClient.DisconnectAsync();

            return base.OnExiting(e);
        }
    }
}
