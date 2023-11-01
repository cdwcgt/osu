// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BanchoSharp;
using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Screens;
using osu.Game.Graphics;
using osu.Game.Graphics.Cursor;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osu.Game.Overlays.Chat;

namespace osu.Game.Screens.IrcBot
{
    [Cached]
    public partial class IrcBotScreen : OsuScreen
    {
        [Cached]
        private readonly BanchoClient banchoClient = new BanchoClient();

        private IrcLogin loginPanel = null!;
        private IrcChannelList roomTab = null!;
        private Container<DrawableIrcChannel> currentChannelContainer = null!;

        public BindableBool IsLogin = new BindableBool();
        private readonly BindableDictionary<IChatChannel, DrawableIrcChannel> loadedIrcChannel = new BindableDictionary<IChatChannel, DrawableIrcChannel>();

        [Cached]
        private readonly Bindable<IChatChannel?> currentIrcChannel = new Bindable<IChatChannel?>();

        [Cached]
        private readonly OverlayColourProvider overlayColourProvider = new OverlayColourProvider(OverlayColourScheme.Orange);

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                new PopoverContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new OsuContextMenuContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                Colour = overlayColourProvider.Background6,
                                RelativeSizeAxes = Axes.Both,
                            },
                            new FillFlowContainer
                            {
                                Origin = Anchor.TopLeft,
                                Anchor = Anchor.TopLeft,
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                                Children = new Drawable[]
                                {
                                    new AddChannelButton
                                    {
                                        Origin = Anchor.TopLeft,
                                        Anchor = Anchor.TopLeft,
                                    },
                                    new AddMatchButton
                                    {
                                        Origin = Anchor.TopLeft,
                                        Anchor = Anchor.TopLeft,
                                    },
                                    new IconButton
                                    {
                                        Origin = Anchor.TopLeft,
                                        Anchor = Anchor.TopLeft,
                                        Icon = FontAwesome.Solid.Retweet,
                                        IconHoverColour = colours.Green,
                                        TooltipText = "Refresh Channel List",
                                        Action = freshChannel,
                                    }
                                }
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

            banchoClient.OnUserQueried += _ => freshChannel();
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

                if (loadedIrcChannel.TryGetValue(c.NewValue, out var drawableChannel))
                {
                    currentChannelContainer.Clear(false);
                    currentChannelContainer.Add(drawableChannel);
                    return;
                }

                loadNewChannel(c.NewValue);
            });
        }

        private void freshChannel()
        {
            if (!banchoClient.IsAuthenticated)
                return;

            foreach (var channel in banchoClient.Channels.Where(c => !loadedIrcChannel.ContainsKey(c)))
            {
                loadNewChannel(channel);
            }
        }

        private void loadNewChannel(IChatChannel channel)
        {
            if (loadedIrcChannel.ContainsKey(channel))
                return;

            var drawableChannel = channel.IsMatchChannel()
                ? new DrawableIrcMatchChannel(channel)
                : new DrawableIrcChannel(channel);

            LoadComponentAsync(drawableChannel, ircChannel =>
            {
                loadedIrcChannel.Add(channel, drawableChannel);
                currentChannelContainer.Clear(false);
                currentChannelContainer.Add(ircChannel);
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

            loadNewChannel(channel);
        }

        private void onLeaveChannel(IChatChannel channel)
        {
            if (!loadedIrcChannel.TryGetValue(channel, out var drawableChannel))
                return;

            if (currentChannelContainer.Child.Channel == channel)
            {
                currentChannelContainer.Clear();
                return;
            }

            loadedIrcChannel.Remove(channel);
            drawableChannel.Expire();
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
