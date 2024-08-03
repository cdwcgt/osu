// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.IO.Network;
using osu.Framework.Logging;
using osu.Framework.Threading;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Chat;

namespace osu.Game.Tournament.Chat
{
    public partial class APIChatClient : Component
    {
        private const double update_delay = 800;
        public Bindable<Channel> CurrentChannel { get; private set; } = new Bindable<Channel>();

        public int MatchId
        {
            get => matchId;
            set
            {
                matchId = value;
                CurrentChannel.Value = new Channel
                {
                    Name = "mp",
                    Id = value,
                    Type = ChannelType.Multiplayer
                };
                updateDelegate = Scheduler.Add(loop);
            }
        }

        private double lastTime;
        private int matchId;
        private ScheduledDelegate? updateDelegate;

        private void loop()
        {
            if (matchId <= 0)
                return;

            var req = new JsonWebRequest<APIChatMessage[]>($"https://api.cdwcgt.top/v1/match_chat?id={matchId}&t={lastTime}");
            req.Finished += () =>
            {
                var messages = req.ResponseObject;

                updateDelegate = Scheduler.AddDelayed(loop, update_delay);

                if (messages == null)
                    return;

                var newMessages = messages.Select(m =>
                {
                    bool banchoBot = m.SenderId == 1;
                    var message = new Message
                    {
                        Sender = new APIUser
                        {
                            IsBot = banchoBot,
                            Username = m.SenderName
                        },
                        Content = m.Content,
                        Timestamp = DateTimeOffset.UnixEpoch.AddSeconds(m.Timestamp),
                        ChannelId = matchId,
                    };

                    if (banchoBot)
                        message.Sender.Colour = "#e45678";

                    return message;
                }).ToArray();

                if (newMessages.Length == 0)
                    return;

                CurrentChannel.Value.AddNewMessages(newMessages);
                lastTime = messages.Max(m => m.Timestamp);
            };
            req.Failed += _ => updateDelegate = Scheduler.AddDelayed(loop, update_delay);

            try
            {
                req.Perform();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to get chat from API", LoggingTarget.Network);
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            updateDelegate?.Cancel();
            updateDelegate = null;
            base.Dispose(isDisposing);
        }
    }
}
