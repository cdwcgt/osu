// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
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

        // TODO: 使用事件而不是BindableBool
        public BindableBool ConfirmedByApi { get; } = new BindableBool();

        [Resolved]
        private LadderInfo ladder { get; set; } = null!;

        private readonly Bindable<TournamentMatch?> currentMatch = new Bindable<TournamentMatch?>();

        private readonly IBindableList<APIMatchEvent> events = new BindableList<APIMatchEvent>();

        [Resolved]
        private MatchListener listener { get; set; } = null!;

        [Resolved]
        private MatchIPCInfo ipc { get; set; } = null!;

        public BindableBool CanShowResult { get; } = new BindableBool();

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

            ipc.Score1.BindValueChanged(_ => updateScore());
            ipc.Score2.BindValueChanged(_ => updateScore());

            ipc.State.BindValueChanged(s =>
            {
                if (s.NewValue == TourneyState.Ranking)
                    return;

                // to reset the status.
                ConfirmedByApi.Value = true;

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

                if (!s.NewValue)
                {
                    score1 = score2 = 0;
                    CanShowResult.Value = false;
                }

                updateScore();
            }, true);

            CanShowResult.BindValueChanged(_ => updateScore());
        }

        private void updateScore()
        {
            if (ConfirmedByApi.Value && listener.CurrentlyListening.Value && !CanShowResult.Value)
            {
                Score1.Value = score1;
                Score2.Value = score2;
            }

            Score1.Value = ipc.Score1.Value;
            Score2.Value = ipc.Score2.Value;
        }

        private long score1;
        private long score2;

        private void processNewMatchEvent(APIMatchEvent newEvent)
        {
            if (currentMatch.Value == null)
                return;

            if (ipc.State.Value != TourneyState.Playing && ipc.State.Value != TourneyState.Ranking)
                return;

            APIMatchGame? matchResult = newEvent.Game;

            if (matchResult == null)
                return;

            // if we actually need to confirm this is a pick?
            int currentBeatmapId = currentMatch.Value.PicksBans.LastOrDefault(s => s.Type == ChoiceType.Pick)?.BeatmapID ?? 0;

            if (matchResult.BeatmapId != currentBeatmapId)
                return;

            List<int> team1Player = currentMatch.Value.Team1.Value?.Players.Select(p => p.OnlineID).ToList() ?? new List<int>();
            List<int> team2Player = currentMatch.Value.Team2.Value?.Players.Select(p => p.OnlineID).ToList() ?? new List<int>();

            ConfirmedByApi.Value = true;
            score1 = matchResult.Scores.Where(s => team1Player.Contains(s.UserID)).Select(s => s.Score).Sum();
            score2 = matchResult.Scores.Where(s => team2Player.Contains(s.UserID)).Select(s => s.Score).Sum();
        }
    }
}
