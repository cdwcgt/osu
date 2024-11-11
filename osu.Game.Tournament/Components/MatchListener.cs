// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Online.API;
using osu.Game.Tournament.Online.Requests;
using osu.Game.Tournament.Online.Requests.Responses;

namespace osu.Game.Tournament.Components
{
    public partial class MatchListener : Component
    {
        private int currentMatch;

        // we just need fetch when game finished.
        private const int refresh_interval = 100000;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        public IBindableList<APIMatchEvent> Events => events;

        private readonly BindableList<APIMatchEvent> events = new BindableList<APIMatchEvent>();

        public IBindable<bool> CurrentlyListening => currentlyListening;

        private readonly BindableBool currentlyListening = new BindableBool();

        private long latestMatchEventID => events.Count != 0 ? events.Max(e => e.Id) : 0;

        private double waitTime;

        public void StartListening()
        {
            currentlyListening.Value = true;
        }

        public void StartListening(int? matchID)
        {
            if (!matchID.HasValue)
                return;

            StartListening();
            events.Clear();
            currentMatch = matchID.Value;
            FetchMatch();
        }

        public void StopListening()
        {
            currentlyListening.Value = false;
        }

        protected override void Update()
        {
            base.Update();

            if (!api.IsLoggedIn)
                return;

            if (!currentlyListening.Value)
                return;

            waitTime += Time.Elapsed;

            if (waitTime >= refresh_interval)
            {
                FetchMatch();
            }
        }

        public void FetchMatch()
        {
            waitTime = 0;

            var req = new GetAPIMatchInfo(currentMatch)
            {
                AfterEvent = latestMatchEventID
            };

            req.Success += content =>
            {
                var newEvent = content.Events.ExceptBy(events.Select(e => e.Id), e => e.Id);

                events.AddRange(newEvent);

                if (events.Any(e => e.Detail.Type == MatchEventType.MatchDisbanded))
                    StopListening();
            };

            api.Queue(req);
        }
    }
}
