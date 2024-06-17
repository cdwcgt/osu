// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Screens;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Scoring;
using osu.Game.Screens.OnlinePlay.Multiplayer.Spectate;
using osu.Game.Screens.Play;
using osu.Game.Screens.Play.HUD;

namespace osu.Game.Screens.ReplayVs
{
    public partial class ReplayVsScreen : OsuScreen
    {
        // Isolates beatmap/ruleset to this screen.
        public override bool DisallowExternalBeatmapRulesetChanges => true;

        public override bool HideOverlaysOnEnter => true;

        // We are managing our own adjustments. For now, this happens inside the Player instances themselves.
        public override bool? ApplyModTrackAdjustments => false;
        public override bool AllowBackButton => false;

        /// <summary>
        /// Whether all spectating players have finished loading.
        /// </summary>
        public bool AllPlayersLoaded => instances.All(p => p.PlayerLoaded);

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        private readonly WorkingBeatmap beatmap;
        private readonly PlayerArea[] instances;
        private MasterGameplayClockContainer masterClockContainer = null!;
        private SpectatorSyncManager syncManager = null!;
        private PlayerGrid grid = null!;
        private PlayerArea? currentAudioSource;
        private readonly int replayCount;
        private readonly Score[] teamRedScores;
        private readonly Score[] teamBlueScores;
        private readonly BindableLong teamRedScore = new BindableLong();
        private readonly BindableLong teamBlueScore = new BindableLong();
        private IAggregateAudioAdjustment? boundAdjustments;

        public ReplayVsScreen(Score[] teamRedScores, Score[] teamBlueScores, WorkingBeatmap beatmap)
        {
            replayCount = teamRedScores.Length + teamBlueScores.Length;
            instances = new PlayerArea[replayCount];
            this.teamRedScores = teamRedScores;
            this.teamBlueScores = teamBlueScores;
            this.beatmap = beatmap;
        }

        protected override void LoadComplete()
        {
            Container scoreDisplayContainer;
            Beatmap.Value = beatmap;
            masterClockContainer = new MasterGameplayClockContainer(Beatmap.Value, 0);

            InternalChildren = new[]
            {
                (Drawable)(syncManager = new SpectatorSyncManager(masterClockContainer)
                {
                    ReadyToStart = performInitialSeek,
                }),
                masterClockContainer.WithChild(new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    RowDimensions = new[]
                    {
                        new Dimension(), new Dimension(GridSizeMode.AutoSize)
                    },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            grid = new PlayerGrid { RelativeSizeAxes = Axes.Both }
                        },
                        new Drawable[]
                        {
                            scoreDisplayContainer = new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y
                            },
                        },
                    }
                }),
                new HoldForMenuButton
                {
                    Action = this.Exit,
                    Padding = new MarginPadding
                    {
                        Bottom = 20
                    },
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                }
            };

            bool singleTeam = teamBlueScores.Length == 0 || teamRedScores.Length == 0;

            for (int i = 0; i < teamRedScores.Length; i++)
            {
                grid.Add(instances[i] = new PlayerArea(-1, syncManager.CreateManagedClock(), true, singleTeam ? Colour4.White : colours.Red));
            }

            for (int i = teamRedScores.Length; i < replayCount; i++)
            {
                grid.Add(instances[i] = new PlayerArea(-1, syncManager.CreateManagedClock(), true, singleTeam ? Colour4.White : colours.Blue));
            }

            if (!singleTeam)
            {
                LoadComponentAsync(new MatchScoreDisplay
                {
                    Team1Score =
                    {
                        BindTarget = teamRedScore
                    },
                    Team2Score =
                    {
                        BindTarget = teamBlueScore
                    },
                }, scoreDisplayContainer.Add);
            }

            base.LoadComplete();

            masterClockContainer.Reset();

            for (int i = 0; i < teamRedScores.Length; i++)
            {
                instances[i].LoadScore(teamRedScores[i]);
            }

            for (int i = 0; i < teamBlueScores.Length; i++)
            {
                instances[i + teamRedScores.Length].LoadScore(teamBlueScores[i]);
            }

            bindAudioAdjustments(instances.First());
        }

        private void bindAudioAdjustments(PlayerArea first)
        {
            if (boundAdjustments != null)
                masterClockContainer.AdjustmentsFromMods.UnbindAdjustments(boundAdjustments);

            boundAdjustments = first.ClockAdjustmentsFromMods;
            masterClockContainer.AdjustmentsFromMods.BindAdjustments(boundAdjustments);
        }

        protected override void Update()
        {
            base.Update();

            if (!isCandidateAudioSource(currentAudioSource?.SpectatorPlayerClock))
            {
                currentAudioSource = instances.Where(i => isCandidateAudioSource(i.SpectatorPlayerClock)).MinBy(i => Math.Abs(i.SpectatorPlayerClock.CurrentTime - syncManager.CurrentMasterTime));

                // Only bind adjustments if there's actually a valid source, else just use the previous ones to ensure no sudden changes to audio.
                if (currentAudioSource != null)
                    bindAudioAdjustments(currentAudioSource);

                foreach (var instance in instances)
                    instance.Mute = instance != currentAudioSource;
            }

            if (!AllPlayersLoaded) return;

            long team1Score = 0;
            long team2Score = 0;

            for (int i = 0; i < teamRedScores.Length; i++)
            {
                if (instances[i].Player != null)
                {
                    team1Score += instances[i].Player!.ScoreProcessor.TotalScore.Value;
                }
            }

            for (int i = teamRedScores.Length; i < replayCount; i++)
            {
                if (instances[i].Player != null)
                {
                    team2Score += instances[i].Player!.ScoreProcessor.TotalScore.Value;
                }
            }

            teamRedScore.Value = team1Score;
            teamBlueScore.Value = team2Score;
        }

        private bool isCandidateAudioSource(SpectatorPlayerClock? clock)
            => clock?.IsRunning == true && !clock.IsCatchingUp && !clock.WaitingOnFrames;

        private void performInitialSeek()
        {
            // We want to start showing gameplay as soon as possible.
            // Each client may be in a different place in the beatmap, so we need to do our best to find a common
            // starting point.
            //
            // Preferring a lower value ensures that we don't have some clients stuttering to keep up.
            List<double> minFrameTimes = new List<double>();

            foreach (var instance in instances)
            {
                if (instance.Score == null)
                    continue;

                minFrameTimes.Add(instance.Score.Replay.Frames.MinBy(f => f.Time)?.Time ?? 0);
            }

            // Remove any outliers (only need to worry about removing those lower than the mean since we will take a Min() after).
            double mean = minFrameTimes.Average();
            minFrameTimes.RemoveAll(t => mean - t > 1000);

            double startTime = minFrameTimes.Min() - 1000;

            masterClockContainer.Reset(startTime, true);
        }
    }
}
