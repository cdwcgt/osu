// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Logging;
using osu.Game.Online.API;
using osu.Game.Online.Chat;
using osu.Game.Overlays.Chat;
using osu.Game.Tournament.IPC;
using osu.Game.Tournament.IPC.MemoryIPC;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.Components
{
    public partial class TournamentMatchChatDisplay : StandAloneChatDisplay
    {
        private readonly Bindable<int> chatChannel = new Bindable<int>();
        private readonly BindableBool useAlternateChat = new BindableBool();

        private ChannelManager? manager;
        private int channelId;

        [Resolved]
        private LadderInfo ladderInfo { get; set; } = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private MatchIPCInfo? ipc { get; set; }

        private IProvideAdditionalData? additionalData => ipc as MemoryBasedIPC;

        public TournamentMatchChatDisplay()
        {
            RelativeSizeAxes = Axes.Both;

            CornerRadius = 0;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            if (ipc != null)
            {
                chatChannel.BindTo(ipc.ChatChannel);
                chatChannel.BindValueChanged(c =>
                {
                    channelId = c.NewValue;

                    if (channelId <= 0) return;

                    UpdateChat(false);
                    Logger.Log($"Switch channel to {channelId}");
                }, true);
            }

            useAlternateChat.BindTo(ladderInfo.UseAlternateChatSource);
            useAlternateChat.BindValueChanged(_ => UpdateChat(true), true);
        }

        public void UpdateChat(bool sourceChanged)
        {
            if (manager == null)
            {
                AddInternal(manager = new ChannelManager(api));
            }

            if (!ladderInfo.UseAlternateChatSource.Value)
            {
                foreach (var ch in manager.JoinedChannels.ToList())
                    manager.LeaveChannel(ch);

                var channel = new Channel
                {
                    Id = channelId,
                    Type = ChannelType.Public
                };

                manager.JoinChannel(channel);

                if (sourceChanged && additionalData != null)
                {
                    Channel.UnbindFrom(additionalData.TourneyChatChannel);
                    Channel.BindTo(manager.CurrentChannel);
                }

                manager.CurrentChannel.Value = channel;
                return;
            }

            if (sourceChanged && additionalData != null)
            {
                Channel.UnbindFrom(manager.CurrentChannel);
                Channel.BindTo(additionalData.TourneyChatChannel!);
            }
        }

        public void Expand()
        {
            this.FadeIn(300);
            OnExpand?.Invoke();
        }

        public void Contract()
        {
            this.FadeOut(200);
            OnContract?.Invoke();
        }

        public Action? OnExpand;
        public Action? OnContract;

        protected override ChatLine? CreateMessage(Message message)
        {
            if (message.Content.StartsWith("!mp", StringComparison.Ordinal))
                return null;

            return new MatchMessage(message, ladderInfo);
        }

        protected override StandAloneDrawableChannel CreateDrawableChannel(Channel channel) => new MatchChannel(channel);

        public partial class MatchChannel : StandAloneDrawableChannel
        {
            public MatchChannel(Channel channel)
                : base(channel)
            {
                ScrollbarVisible = false;
            }
        }

        protected partial class MatchMessage : StandAloneMessage
        {
            public MatchMessage(Message message, LadderInfo info)
                : base(message)
            {
                if (info.CurrentMatch.Value is TournamentMatch match)
                {
                    if (match.Team1.Value?.Players.Any(u => u.OnlineID == Message.Sender.OnlineID || u.Username == Message.Sender.Username) == true)
                        UsernameColour = TournamentGame.COLOUR_RED;
                    else if (match.Team2.Value?.Players.Any(u => u.OnlineID == Message.Sender.OnlineID || u.Username == Message.Sender.Username) == true)
                        UsernameColour = TournamentGame.COLOUR_BLUE;
                }
            }
        }
    }
}
