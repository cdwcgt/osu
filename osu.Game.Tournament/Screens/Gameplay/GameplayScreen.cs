// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
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
        private Sprite slotSprite = null!;

        [Resolved]
        private TournamentSceneManager? sceneManager { get; set; }

        [Resolved]
        private TextureStore textures { get; set; } = null!;

        [Resolved]
        private TournamentMatchChatDisplay chat { get; set; } = null!;

        private Drawable chroma = null!;

        private Bindable<double?> team1Coin = new Bindable<double?>();
        private Bindable<double?> team2Coin = new Bindable<double?>();

        protected override SongBar CreateSongBar() => new GameplaySongBar
        {
            Depth = float.MinValue,
        };

        private bool switchFromMappool;

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
                new Container
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    Size = new Vector2(100, 50),
                    Margin = new MarginPadding { Left = 10f, Bottom = 7f },
                    Child = slotSprite = new Sprite
                    {
                        Alpha = 0,
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        FillMode = FillMode.Fit,
                        RelativeSizeAxes = Axes.Both
                    }
                },
                roundPreview = new RoundInformationPreview
                {
                    Alpha = 0f,
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Margin = new MarginPadding(13)
                }
            });

            ControlPanel.AddRange(new Drawable[]
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
                new ControlPanel.Spacer(),
                new MatchRoundNameTextBox
                {
                    RelativeSizeAxes = Axes.X,
                },
                new TourneyButton
                {
                    RelativeSizeAxes = Axes.X,
                    Text = "Toggle map detail",
                    Action = () =>
                    {
                        if (roundPreviewShow)
                        {
                            HideRoundPreview();
                        }
                        else
                        {
                            ShowRoundPreview();
                        }
                    }
                },
                team1CoinText = new TourneyNumberBox
                {
                    LabelText = "Team1 Coin"
                },
                team2CoinText = new TourneyNumberBox
                {
                    LabelText = "Team2 Coin"
                },
                new TourneyButton
                {
                    Text = "Apply coin",
                    Action = () =>
                    {
                        team1Coin.Value = team1CoinText.Current.Value;
                        team2Coin.Value = team2CoinText.Current.Value;
                    }
                }
            });

            LadderInfo.ChromaKeyWidth.BindValueChanged(width => chroma.Width = width.NewValue, true);

            warmup.BindValueChanged(w =>
            {
                warmupButton.Alpha = !w.NewValue ? 0.5f : 1;
                header.ShowScores = !w.NewValue;
            }, true);

            CurrentMatch.BindValueChanged(m =>
            {
                team1Coin.UnbindBindings();
                team2Coin.UnbindBindings();

                if (m.NewValue == null)
                    return;

                Scheduler.AddOnce(() =>
                {
                    team1Coin.BindTo(m.NewValue.Team1Coin);
                    team2Coin.BindTo(m.NewValue.Team2Coin);
                });
            }, true);

            team1Coin.BindValueChanged(c =>
            {
                team1CoinText.Current.Value = c.NewValue;
            }, true);

            team2Coin.BindValueChanged(c =>
            {
                team2CoinText.Current.Value = c.NewValue;
            }, true);

            sceneManager?.CurrentScreen.BindValueChanged(s =>
            {
                if (s.OldValue == typeof(MapPoolScreen) && s.NewValue == typeof(GameplayScreen))
                    switchFromMappool = true;
            });
        }

        private bool roundPreviewShow;

        public bool ShowRoundPreview()
        {
            if (!IsLoaded)
                return false;

            scheduledShowRoundPreview?.Cancel();

            if (roundPreviewShow)
                return false;

            if (!IsPresent)
                return false;

            if (State.Value != TourneyState.Idle && State.Value != TourneyState.Ranking)
                return false;

            SongBar.FadeOut(100);
            chat.Contract();

            using (roundPreview.BeginDelayedSequence(200))
                roundPreview.FadeIn(200);

            roundPreviewShow = true;
            return true;
        }

        public void HideRoundPreview()
        {
            scheduledHideRoundPreview?.Cancel();

            if (!roundPreviewShow)
                return;

            roundPreview.FadeOut(100);

            using (SongBar.BeginDelayedSequence(200))
                SongBar.FadeIn(200);

            roundPreviewShow = false;

            updateState();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            State.BindTo(ipc.State);
            State.BindValueChanged(_ => updateState(), true);
            LadderInfo.InvertScoreColour.BindValueChanged(v => scoreDisplay.InvertTextColor = v.NewValue, true);
        }

        protected override void SetModAcronym(string acronym)
        {
            var texture = textures.Get($"Slots/{acronym}");

            if (texture == null)
                slotSprite.FadeOut(500, Easing.Out);
            else
            {
                slotSprite.Texture = texture;
                slotSprite.FadeInFromZero(500, Easing.Out);
            }
        }

        protected override void CurrentMatchChanged(ValueChangedEvent<TournamentMatch?> match)
        {
            base.CurrentMatchChanged(match);

            if (match.NewValue == null)
                return;

            warmup.Value = match.NewValue.Team1Score.Value + match.NewValue.Team2Score.Value == 0;
            scheduledScreenChange?.Cancel();
        }

        private ScheduledDelegate? scheduledScreenChange;
        private ScheduledDelegate? scheduledContract;
        private ScheduledDelegate? scheduledShowRoundPreview;
        private ScheduledDelegate? scheduledHideRoundPreview;

        private TournamentMatchScoreDisplay scoreDisplay = null!;

        private TourneyState lastState;
        private MatchHeader header = null!;
        private RoundInformationPreview roundPreview = null!;
        private TourneyNumberBox team1CoinText = null!;
        private TourneyNumberBox team2CoinText = null!;

        private void contract()
        {
            if (!IsLoaded)
                return;

            HideRoundPreview();

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
            SongBar.Expanded = true;

            using (BeginDelayedSequence(300))
            {
                scoreDisplay.FadeIn(100);
            }
        }

        private bool isTB
        {
            get
            {
                var lastPick = CurrentMatch.Value?.PicksBans.LastOrDefault();
                var tbMap = CurrentMatch.Value?.Round.Value?.Beatmaps.FirstOrDefault(map => map.Mods == "TB");

                if (lastPick == null || tbMap == null)
                    return false;

                return lastPick.Type == ChoiceType.Pick && lastPick.BeatmapID == tbMap.ID;
            }
        }

        private const double winner_bonus = 110;
        private const double extra_winner_bonus_tb = 40;

        private void updateState()
        {
            try
            {
                scheduledScreenChange?.Cancel();

                if (State.Value == TourneyState.Ranking)
                {
                    if (warmup.Value || CurrentMatch.Value == null) return;

                    if (ipc.Score1.Value > ipc.Score2.Value)
                    {
                        // 黄金加成
                        CurrentMatch.Value.Team1Coin.Value += winner_bonus + (isTB ? extra_winner_bonus_tb : 0);
                        CurrentMatch.Value.Team2Coin.Value += (double)ipc.Score2.Value / ipc.Score1.Value * 100;
                    }
                    else
                    {
                        CurrentMatch.Value.Team2Coin.Value += winner_bonus + (isTB ? extra_winner_bonus_tb : 0);
                        CurrentMatch.Value.Team1Coin.Value += (double)ipc.Score1.Value / ipc.Score2.Value * 100;
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
                        HideRoundPreview();
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
            if (roundPreviewShow)
            {
                HideRoundPreview();
            }

            scheduledScreenChange?.Cancel();
            scheduledShowRoundPreview?.Cancel();
            base.Hide();
        }

        public override void Show()
        {
            updateState();

            if (switchFromMappool)
            {
                scheduledShowRoundPreview = Scheduler.AddDelayed(() =>
                {
                    if (ShowRoundPreview())
                        scheduledHideRoundPreview = Scheduler.AddDelayed(HideRoundPreview, 5000);
                }, 5000);
            }

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
