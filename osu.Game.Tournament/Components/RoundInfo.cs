// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Tournament.IPC;
using osu.Game.Tournament.Models;
using osu.Game.Tournament.Online.Requests.Responses;

namespace osu.Game.Tournament.Components
{
    public partial class RoundInfo : Component
    {
        public BindableLong Score1 { get; } = new BindableLong();
        public BindableLong Score2 { get; } = new BindableLong();

        public BindableBool ConfirmedByApi { get; } = new BindableBool();

        [Resolved]
        private LadderInfo ladder { get; set; } = null!;

        private readonly Bindable<TournamentMatch?> currentMatch = new Bindable<TournamentMatch?>();

        private readonly IBindableList<APIMatchEvent> events = new BindableList<APIMatchEvent>();

        [Resolved]
        private MatchListener listener { get; set; } = null!;

        [Resolved]
        private MatchIPCInfo ipc { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            currentMatch.BindTo(ladder.CurrentMatch);

            events.BindTo(listener.Events);

            events.BindCollectionChanged((_, args) =>
            {
                if (args.Action != NotifyCollectionChangedAction.Add || args.NewItems == null)
                    return;

                foreach (APIMatchEvent newEvent in args.NewItems)
                {
                    processNewMatchEvent(newEvent);
                }
            });

            ipc.Score1.BindValueChanged(_ => updateScoreFromIPC());
            ipc.Score2.BindValueChanged(_ => updateScoreFromIPC());

            ipc.State.BindValueChanged(s =>
            {
                if (s.NewValue != TourneyState.Idle)
                    return;

                Score1.Value = 0;
                Score2.Value = 0;
                ConfirmedByApi.Value = false;
            });

            ConfirmedByApi.BindValueChanged(s =>
            {
                if (!listener.CurrentlyListening.Value)
                {
                    // if we are not listening a api, just trust ipc.
                    ConfirmedByApi.Value = true;
                }
            });
        }

        private void updateScoreFromIPC()
        {
            if (ConfirmedByApi.Value)
                return;

            Score1.Value = ipc.Score1.Value;
            Score2.Value = ipc.Score2.Value;
        }

        private void processNewMatchEvent(APIMatchEvent newEvent)
        {
            if (currentMatch.Value == null)
                return;

            APIMatchGame? matchResult = newEvent.Game;

            if (matchResult == null)
                return;

            // if we actually need to confirm this is a pick?
            int currentBeatmapId = currentMatch.Value.PicksBans.Last(s => s.Type == ChoiceType.Pick).BeatmapID;

            if (matchResult.BeatmapId != currentBeatmapId)
                return;

            int[] team1Player = currentMatch.Value.Team1.Value?.Players.Select(p => p.OnlineID).ToArray() ?? Array.Empty<int>();
            int[] team2Player = currentMatch.Value.Team2.Value?.Players.Select(p => p.OnlineID).ToArray() ?? Array.Empty<int>();

            ConfirmedByApi.Value = true;
            Score1.Value = matchResult.Scores.Where(s => team1Player.Any(t => t == s.UserID)).Select(s => s.TotalScore).Sum();
            Score2.Value = matchResult.Scores.Where(s => team2Player.Any(t => t == s.UserID)).Select(s => s.TotalScore).Sum();
        }
    }
}
