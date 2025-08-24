// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Threading;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Online.API;
using osu.Game.Tournament.Models;
using osu.Game.Tournament.Online.Requests;
using osu.Game.Tournament.Online.Requests.Responses;

namespace osu.Game.Tournament.IPC.MemoryIPC
{
    [SupportedOSPlatform("windows")]
    public partial class MemoryBasedIPCWithMatchListener : MemoryBasedIPC
    {
        private int currentMatch = -1;
        private long abortedEventId = 0;
        private long currentGameID = -1;

        private double waitTime;

        public event Action<bool>? MatchFinished;
        public event Action? FetchFailed;
        public event Action? MatchAborted;

        private BeatmapChoice? pendingBindChoice;

        // we just need fetch when game finished.
        private const int refresh_interval = 3000;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        public IBindableList<APIMatchEvent> Events => events;

        private readonly BindableList<APIMatchEvent> events = new BindableList<APIMatchEvent>();

        public IBindable<bool> CurrentlyListening => currentlyListening;

        private readonly BindableBool currentlyListening = new BindableBool();

        public IBindable<bool> CurrentlyPlaying => currentlyPlaying;

        private readonly BindableBool currentlyPlaying = new BindableBool();

        public bool Aborted => abortedEventId == currentGameID;
        private APIMatchEvent? currentMatchEvent => Events.LastOrDefault(e => e.Id == currentMatch);

        public APIMatchEvent? LatestMatchEvent => Events.MaxBy(e => e.Id);
        public long LatestMatchEventID => LatestMatchEvent?.Id ?? 0;

        public MemoryBasedIPCWithMatchListener()
        {
            State.BindValueChanged(s =>
            {
                if (s.NewValue != TourneyState.Ranking || s.OldValue != TourneyState.Playing)
                    return;

                RequestCurrentRoundResultFromApi();
            });
        }

        public void StartListening()
        {
            if (currentMatch == -1) return;

            currentlyListening.Value = true;
            FetchMatch();
        }

        public void StartListening(int? matchID)
        {
            if (!matchID.HasValue)
                return;

            StopListening();
            currentMatch = matchID.Value;
            StartListening();
        }

        public void StopListening()
        {
            events.Clear();
            currentMatch = -1;
            abortedEventId = 0;
            currentGameID = -1;
            pendingBindChoice = null;
            currentMatchFinished = false;
            currentlyPlaying.Value = false;
            currentlyListening.Value = false;
        }

        public void CurrentRoundAborted()
        {
            if (!currentlyPlaying.Value
                || LatestMatchEvent?.Game == null
                || State.Value != TourneyState.Idle
                || abortedEventId == currentGameID)
                return;

            currentlyPlaying.Value = false;
            abortedEventId = currentGameID;
            MatchAborted?.Invoke();
        }

        private ScheduledDelegate? fetchTimeOutScheduleDelegate;

        /// <summary>
        /// set timeout to fetch result
        /// </summary>
        public void RequestCurrentRoundResultFromApi()
        {
            if (!currentlyListening.Value)
            {
                if (pendingBindChoice != null)
                {
                    pendingBindChoice.Scores[TeamColour.Red] = getTeamScore(TeamColour.Red, true).Sum(CalculateModMultiplier);
                    pendingBindChoice.Scores[TeamColour.Blue] = getTeamScore(TeamColour.Blue, true).Sum(CalculateModMultiplier);
                    pendingBindChoice = null;
                }

                currentlyPlaying.Value = false;
                MatchFinished?.Invoke(false);
            }

            if (!currentlyPlaying.Value || currentMatchFinished)
                return;

            if (fetchTimeOutScheduleDelegate?.Completed == false)
                return;

            fetchTimeOutScheduleDelegate?.Cancel();

            fetchTimeOutScheduleDelegate = Scheduler.AddDelayed(() =>
            {
                if (pendingBindChoice != null)
                {
                    pendingBindChoice.Scores[TeamColour.Red] = getTeamScore(TeamColour.Red, true).Sum(CalculateModMultiplier);
                    pendingBindChoice.Scores[TeamColour.Blue] = getTeamScore(TeamColour.Blue, true).Sum(CalculateModMultiplier);
                    pendingBindChoice = null;
                }

                currentlyPlaying.Value = false;
                MatchFinished?.Invoke(false);
            }, 10_000);

            FetchMatch();
        }

        public void BindChoiceToNextOrCurrentMatch(BeatmapChoice? choice)
        {
            pendingBindChoice = choice;
        }

        protected override void Update()
        {
            base.Update();

            if (!api.IsLoggedIn)
                return;

            if (!currentlyListening.Value)
                return;

            updateStatue();

            waitTime += Time.Elapsed;

            if (waitTime >= refresh_interval)
            {
                FetchMatch();
            }
        }

        private void updateStatue()
        {
            if (!CurrentlyListening.Value || !CurrentlyPlaying.Value || Aborted || State.Value == TourneyState.Playing || currentGameID == -1)
                return;

            if (currentMatchFinished)
            {
                if (pendingBindChoice != null)
                {
                    pendingBindChoice.Scores[TeamColour.Red] = getTeamScore(TeamColour.Red, true).Sum(CalculateModMultiplier);
                    pendingBindChoice.Scores[TeamColour.Blue] = getTeamScore(TeamColour.Blue, true).Sum(CalculateModMultiplier);
                    pendingBindChoice = null;
                }

                currentlyPlaying.Value = false;

                fetchTimeOutScheduleDelegate?.Cancel();
                fetchTimeOutScheduleDelegate = null;
                MatchFinished?.Invoke(true);
            }
        }

        private IEnumerable<PlayerScore> getTeamScore(TeamColour colour, bool forceApi = false)
        {
            if (!forceApi && (!currentMatchFinished || Aborted || currentlyPlaying.Value || State.Value == TourneyState.Playing))
                return base.GetTeamScore(colour);

            var gameResult = Events.LastOrDefault(e => e.Game?.Id == currentGameID)?.Game;

            if (gameResult == null)
                return base.GetTeamScore(colour);

            LegacyMods mods = LegacyMods.None;

            if (gameResult.Mods != null)
            {
                foreach (string mod in gameResult.Mods)
                {
                    mods |= GetLegacyModFromString(mod);
                }
            }

            int[] teamIds = Ladder.CurrentMatch.Value?.GetTeamByColor(colour)?.Players.Select(p => p.OnlineID).ToArray() ??
                            Array.Empty<int>();

            return gameResult.Scores.Where(s => teamIds.Any(t => t == s.UserID)).Select(s =>
            {
                LegacyMods playerMods = mods;

                foreach (APIMod mod in s.Mods)
                {
                    playerMods |= GetLegacyModFromString(mod.Acronym);
                }

                return new PlayerScore
                {
                    OnlineId = s.UserID,
                    Score = s.Score,
                    Mods = playerMods
                };
            });
        }

        public void AddFakeEvent(long redScore, long blueScore)
        {
            if (!CurrentlyListening.Value)
                return;

            if (!currentlyPlaying.Value || currentMatchFinished || Aborted)
                return;

            if (currentGameID == -1)
                return;

            int redOnlineId = Ladder.CurrentMatch.Value?.GetTeamByColor(TeamColour.Red)?.Players.FirstOrDefault()?.OnlineID ?? -1;
            int blueOnlineId = Ladder.CurrentMatch.Value?.GetTeamByColor(TeamColour.Red)?.Players.FirstOrDefault()?.OnlineID ?? -1;

            events.Add(new APIMatchEvent
            {
                Id = currentGameID,
                Timestamp = DateTime.Now,
                Game = new APIMatchGame
                {
                    BeatmapId = -1,
                    Id = (int)currentGameID,
                    Scores = new List<MatchScore>
                    {
                        new MatchScore
                        {
                            Score = redScore,
                            UserID = redOnlineId,
                        },
                        new MatchScore
                        {
                            Score = blueScore,
                            UserID = blueOnlineId,
                        }
                    }
                }
            });

            currentMatchFinished = true;
        }

        protected override IEnumerable<PlayerScore> GetTeamScore(TeamColour colour) => getTeamScore(colour);

        // ture meanwhile current match id is null from api
        private bool currentMatchFinished;

        public void FetchMatch()
        {
            waitTime = 0;

            var req = new GetAPIMatchInfo(currentMatch)
            {
                AfterEvent = LatestMatchEventID
            };

            req.Success += content =>
            {
                var newEvents = content.Events.Where(e => e.Game == null || e.Game?.Scores.Count != 0).ExceptBy(Events.Select(e => e.Id), e => e.Id);

                if (content.CurrentGameID == null)
                {
                    // 理论上永远为true
                    currentMatchFinished = Events.Concat(newEvents).Any(e => e.Game?.Id == currentGameID && e.Game?.Scores.Count != 0);
                }
                else if (content.CurrentGameID != currentGameID)
                {
                    currentMatchFinished = false;
                    currentlyPlaying.Value = true;
                    currentGameID = content.CurrentGameID.Value;
                }

                events.AddRange(newEvents);

                if (Events.Any(e => e.Detail.Type == MatchEventType.MatchDisbanded))
                    StopListening();
            };

            req.Failure += _ => FetchFailed?.Invoke();

            api.Queue(req);
        }

        public static LegacyMods GetLegacyModFromString(string modString)
        {
            switch (modString)
            {
                case "EZ":
                    return LegacyMods.Easy;

                case "NF":
                    return LegacyMods.NoFail;

                case "HT":
                    return LegacyMods.HalfTime;

                case "HR":
                    return LegacyMods.HardRock;

                case "SD":
                    return LegacyMods.SuddenDeath;

                case "PF":
                    return LegacyMods.Perfect;

                case "DT":
                    return LegacyMods.DoubleTime;

                case "NC":
                    return LegacyMods.Nightcore;

                case "FI":
                    return LegacyMods.FadeIn;

                case "HD":
                    return LegacyMods.Hidden;

                case "FL":
                    return LegacyMods.Flashlight;

                case "RX":
                    return LegacyMods.Relax;

                default:
                    throw new ArgumentOutOfRangeException(nameof(modString), modString, $"Cannot prase to {nameof(LegacyMods)}.");
            }
        }
    }
}
