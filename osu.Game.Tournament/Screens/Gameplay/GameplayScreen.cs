// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Threading;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays.Settings;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.IPC;
using osu.Game.Tournament.Models;
using osu.Game.Tournament.Screens.Gameplay.Components;
using osu.Game.Tournament.Screens.MapPool;
using osu.Game.Tournament.Screens.TeamWin;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Gameplay
{
    public partial class GameplayScreen : BeatmapInfoScreen
    {
        private readonly BindableBool warmup = new BindableBool();

        public readonly Bindable<TourneyState> State = new Bindable<TourneyState>();
        private OsuButton warmupButton = null!;
        private MatchIPCInfo ipc = null!;

        private readonly BindableBool scoreMode = new BindableBool();
        private readonly BindableBool autoScoreMode = new BindableBool();
        private readonly Bindable<int?> team1Score = new Bindable<int?>();
        private readonly Bindable<int?> team2Score = new Bindable<int?>();

        private FillFlowContainer scoreModeControl = null!;
        private SettingsNumberBox redScoreInput = null!;
        private SettingsNumberBox blueScoreInput = null!;

        [Resolved]
        private TournamentSceneManager? sceneManager { get; set; }

        [Resolved]
        private TournamentMatchChatDisplay chat { get; set; } = null!;

        private Drawable chroma = null!;

        [BackgroundDependencyLoader]
        private void load(MatchIPCInfo ipc)
        {
            this.ipc = ipc;

            AddRangeInternal(new Drawable[]
            {
                new TourneyVideo("gameplay")
                {
                    Loop = true,
                    RelativeSizeAxes = Axes.Both,
                },
                header = new MatchHeader
                {
                    ShowLogo = false,
                },
                new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Y = 110,
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Children = new[]
                    {
                        chroma = new Container
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Height = 512,
                            Children = new Drawable[]
                            {
                                new ChromaArea
                                {
                                    Name = "Left chroma",
                                    RelativeSizeAxes = Axes.Both,
                                    Width = 0.5f,
                                },
                                new ChromaArea
                                {
                                    Name = "Right chroma",
                                    RelativeSizeAxes = Axes.Both,
                                    Anchor = Anchor.TopRight,
                                    Origin = Anchor.TopRight,
                                    Width = 0.5f,
                                }
                            }
                        },
                    }
                },
                scoreDisplay = new TournamentMatchScoreDisplay
                {
                    Y = -147,
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.TopCentre,
                },
                new ControlPanel
                {
                    Children = new Drawable[]
                    {
                        warmupButton = new TourneyButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Text = "Toggle warmup",
                            Action = () => warmup.Toggle()
                        },
                        new TourneyButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Text = "Toggle chat",
                            Action = () => { State.Value = State.Value == TourneyState.Idle ? TourneyState.Playing : TourneyState.Idle; }
                        },
                        new SettingsSlider<int>
                        {
                            LabelText = "Chroma width",
                            Current = LadderInfo.ChromaKeyWidth,
                            KeyboardStep = 1,
                        },
                        new SettingsSlider<int>
                        {
                            LabelText = "Players per team",
                            Current = LadderInfo.PlayersPerTeam,
                            KeyboardStep = 1,
                        },
                        new SettingsCheckbox
                        {
                            LabelText = "toggle score Mode",
                            Current = scoreMode
                        },
                        scoreModeControl = new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.None,
                            RelativeSizeAxes = Axes.X,
                            Masking = true,
                            Direction = FillDirection.Vertical,
                            AutoSizeDuration = 200,
                            AutoSizeEasing = Easing.OutQuint,
                            Spacing = new Vector2(0, 10),
                            Children = new Drawable[]
                            {
                                new SettingsCheckbox
                                {
                                    LabelText = "Auto Score Mode",
                                    Current = autoScoreMode
                                },
                                redScoreInput = new SettingsNumberBox
                                {
                                    LabelText = "Red Score",
                                },
                                blueScoreInput = new SettingsNumberBox
                                {
                                    LabelText = "Blue Score",
                                },
                                new SettingsButton
                                {
                                    Text = "Apply",
                                    Action = () =>
                                    {
                                        team1Score.Value = redScoreInput.Current.Value;
                                        team2Score.Value = blueScoreInput.Current.Value;
                                    }
                                }
                            }
                        }
                    }
                }
            });

            LadderInfo.ChromaKeyWidth.BindValueChanged(width => chroma.Width = width.NewValue, true);

            warmup.BindValueChanged(w =>
            {
                warmupButton.Alpha = !w.NewValue ? 0.5f : 1;
                header.ShowScores = !w.NewValue;
            }, true);

            team1Score.BindValueChanged(s =>
                redScoreInput.Current.Value = s.NewValue, true);
            team2Score.BindValueChanged(s =>
                blueScoreInput.Current.Value = s.NewValue, true);
            scoreMode.BindValueChanged(s =>
            {
                scoreModeControl.ClearTransforms();

                scoreModeControl.AutoSizeAxes = s.NewValue ? Axes.Y : Axes.None;

                if (!s.NewValue)
                    scoreModeControl.ResizeHeightTo(0, 200, Easing.OutQuint);
            }, true);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            State.BindTo(ipc.State);
            State.BindValueChanged(_ => updateState(), true);
        }

        protected override void CurrentMatchChanged(ValueChangedEvent<TournamentMatch?> match)
        {
            base.CurrentMatchChanged(match);

            if (match.NewValue == null)
                return;

            if (match.OldValue != null)
            {
                scoreMode.UnbindFrom(match.OldValue.ScoreMode);
                team1Score.UnbindFrom(match.OldValue.Team1Score);
                team2Score.UnbindFrom(match.OldValue.Team2Score);
            }

            scoreMode.BindTo(match.NewValue.ScoreMode);
            team1Score.BindTo(match.NewValue.Team1Score);
            team2Score.BindTo(match.NewValue.Team2Score);

            warmup.Value = match.NewValue.Team1Score.Value + match.NewValue.Team2Score.Value == 0;
            scheduledScreenChange?.Cancel();
        }

        private ScheduledDelegate? scheduledScreenChange;
        private ScheduledDelegate? scheduledContract;

        private TournamentMatchScoreDisplay scoreDisplay = null!;

        private TourneyState lastState;
        private MatchHeader header = null!;

        private void contract()
        {
            if (!IsLoaded)
                return;

            scheduledContract?.Cancel();

            SongBar.Expanded = false;
            scoreDisplay.FadeOut(100);
            using (chat.BeginDelayedSequence(500))
                chat.Expand();
        }

        private void expand()
        {
            if (!IsLoaded)
                return;

            scheduledContract?.Cancel();

            chat.Contract();

            using (BeginDelayedSequence(300))
            {
                scoreDisplay.FadeIn(100);
                SongBar.Expanded = true;
            }
        }

        private void updateState()
        {
            try
            {
                scheduledScreenChange?.Cancel();

                if (State.Value == TourneyState.Ranking)
                {
                    if (warmup.Value || CurrentMatch.Value == null) return;

                    if (CurrentMatch.Value.ScoreMode.Value && autoScoreMode.Value)
                    {
                        if (ipc.Score1.Value > ipc.Score2.Value)
                            CurrentMatch.Value.Team1Score.Value += 3000;
                        else
                            CurrentMatch.Value.Team2Score.Value += 3000;
                    }
                    else if (!CurrentMatch.Value.ScoreMode.Value || !autoScoreMode.Value)
                    {
                        if (ipc.Score1.Value > ipc.Score2.Value)
                            CurrentMatch.Value.Team1Score.Value++;
                        else
                            CurrentMatch.Value.Team2Score.Value++;
                    }
                }

                switch (State.Value)
                {
                    case TourneyState.Idle:
                        contract();

                        if (LadderInfo.AutoProgressScreens.Value)
                        {
                            const float delay_before_progression = 4000;

                            // if we've returned to idle and the last screen was ranking
                            // we should automatically proceed after a short delay
                            if (lastState == TourneyState.Ranking && !warmup.Value)
                            {
                                if (CurrentMatch.Value?.Completed.Value == true)
                                    scheduledScreenChange = Scheduler.AddDelayed(() => { sceneManager?.SetScreen(typeof(TeamWinScreen)); }, delay_before_progression);
                                else if (CurrentMatch.Value?.Completed.Value == false)
                                    scheduledScreenChange = Scheduler.AddDelayed(() => { sceneManager?.SetScreen(typeof(MapPoolScreen)); }, delay_before_progression);
                            }
                        }

                        break;

                    case TourneyState.Ranking:
                        scheduledContract = Scheduler.AddDelayed(contract, 10000);
                        break;

                    default:
                        expand();
                        break;
                }
            }
            finally
            {
                lastState = State.Value;
            }
        }

        public override void Hide()
        {
            scheduledScreenChange?.Cancel();
            base.Hide();
        }

        public override void Show()
        {
            updateState();
            base.Show();
        }

        private partial class ChromaArea : CompositeDrawable
        {
            [Resolved]
            private LadderInfo ladder { get; set; } = null!;

            [BackgroundDependencyLoader]
            private void load()
            {
                // chroma key area for stable gameplay
                Colour = new Color4(0, 255, 0, 255);

                ladder.PlayersPerTeam.BindValueChanged(performLayout, true);
            }

            private void performLayout(ValueChangedEvent<int> playerCount)
            {
                switch (playerCount.NewValue)
                {
                    case 3:
                        InternalChildren = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Width = 0.5f,
                                Height = 0.5f,
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                            },
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Anchor = Anchor.BottomLeft,
                                Origin = Anchor.BottomLeft,
                                Height = 0.5f,
                            },
                        };
                        break;

                    default:
                        InternalChild = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                        };
                        break;
                }
            }
        }
    }
}
