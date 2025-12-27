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
using osu.Game.Online.Rooms;
using osu.Game.Overlays.Chat;
using osu.Game.Tournament.IPC;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.Components
{
    public partial class TournamentMatchChatDisplay : StandAloneChatDisplay
    {
        private readonly IBindable<Room?> currentRoom = new Bindable<Room?>();

        private ChannelManager manager = null!;

        [Resolved]
        private LadderInfo ladderInfo { get; set; } = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private LazerRoomMatchInfo ipc { get; set; } = null!;

        public TournamentMatchChatDisplay()
        {
            RelativeSizeAxes = Axes.Both;

            CornerRadius = 0;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AddInternal(manager = new ChannelManager(api));

            currentRoom.BindTo(ipc.CurrentRoom);
            currentRoom.BindValueChanged(c =>
            {
                if (c.OldValue != null)
                {
                    Logger.Log($"Leave Channel {Channel.Value}");
                    manager.LeaveChannel(Channel.Value);
                }

                Scheduler.AddOnce(UpdateChat);
            }, true);
        }

        public void UpdateChat()
        {
            if (currentRoom.Value?.RoomID == null || currentRoom.Value?.ChannelId == null)
                return;

            Channel.Value = manager.JoinChannel(new Channel { Id = currentRoom.Value.ChannelId, Type = ChannelType.Multiplayer, Name = $"#lazermp_{currentRoom.Value.RoomID.Value}" });
            Logger.Log($"Join Channel {Channel.Value}");
        }

        public void Expand() => this.FadeIn(300);

        public void Contract() => this.FadeOut(200);

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
