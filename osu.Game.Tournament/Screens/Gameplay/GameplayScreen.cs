// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Threading;
using osu.Game.Graphics;
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

        [Resolved]
        private MatchListener listener { get; set; } = null!;

        [Resolved]
        private RoundInfo roundInfo { get; set; } = null!;

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
                drawTextContainer = new Container
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Margin = new MarginPadding { Top = 5f },
                    Size = new Vector2(352, 100)
                    //Alpha = 0,
                },
                scoreWarningContainer = new Container
                {
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Margin = new MarginPadding { Bottom = 5f },
                    AutoSizeAxes = Axes.Both,
                    Alpha = 0f,
                    Child = new TournamentSpriteText
                    {
                        Text = "回合进行中获取的分数在提现模式中无法参考，结束后将会自动获取分数。",
                        Font = OsuFont.Torus.With(size: 17f)
                    }
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
                //new TourneyButton
                //{
                //    RelativeSizeAxes = Axes.X,
                //    Text = "Toggle chat",
                //    Action = () => { State.Value = State.Value == TourneyState.Idle ? TourneyState.Playing : TourneyState.Idle; }
                //},
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
                },
                matchID = new SettingsNumberBox
                {
                    LabelText = "Match ID",
                },
                listeningButton = new TourneyButton
                {
                    Text = "Start Listening",
                }
            });

            LadderInfo.ChromaKeyWidth.BindValueChanged(width => chroma.Width = width.NewValue, true);

            warmup.BindValueChanged(w =>
            {
                warmupButton.Alpha = !w.NewValue ? 0.5f : 1;
                header.ShowScores = !w.NewValue;
            }, true);

            sceneManager?.CurrentScreen.BindValueChanged(s =>
            {
                if (s.OldValue == typeof(MapPoolScreen) && s.NewValue == typeof(GameplayScreen))
                    switchFromMappool = true;
            });

            listener.CurrentlyListening.BindValueChanged(s =>
            {
                if (s.NewValue)
                {
                    listeningButton.Text = "Stop Listening";
                    listeningButton.Action = listener.StopListening;
                }
                else
                {
                    listeningButton.Text = "Start Listening";
                    listeningButton.Action = () => listener.StartListening(matchID.Current.Value);
                }
            }, true);

            roundInfo.ConfirmedByApi.BindValueChanged(c =>
            {
                if (c.NewValue)
                {
                    getResult();
                }
            });

            ((GameplaySongBar)SongBar).WaitForResult.BindTo(waitForResult);

            warmup.BindValueChanged(w =>
            {
                warmupButton.BackgroundColour = w.NewValue ? Color4.Red : Color4Extensions.FromHex(@"44aadd");
            });

            listener.CurrentlyPlaying.BindValueChanged(p =>
            {
                updateState();
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

            team1Coin.UnbindAll();
            team2Coin.UnbindAll();

            team1Coin = match.NewValue.Team1Coin.GetBoundCopy();
            team2Coin = match.NewValue.Team2Coin.GetBoundCopy();

            team1Coin.BindValueChanged(c =>
            {
                team1CoinText.Current.Value = c.NewValue;
            }, true);

            team2Coin.BindValueChanged(c =>
            {
                team2CoinText.Current.Value = c.NewValue;
            }, true);
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
        private Container drawTextContainer = null!;
        private SettingsNumberBox matchID = null!;
        private TourneyButton listeningButton = null!;
        private Container scoreWarningContainer = null!;

        private void contract()
        {
            if (!IsLoaded)
                return;

            HideRoundPreview();

            scheduledContract?.Cancel();

            SongBar.Expanded = false;
            scoreDisplay.FadeOut(100);
            scoreWarningContainer.FadeOut(100);
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
                if (!warmup.Value)
                    scoreWarningContainer.FadeIn(100);
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

        public const double WINNER_BONUS = 110;
        public const double EXTRA_WINNER_BONUS_TB = 40;

        private void showDraw(TeamColour colour)
        {
            drawTextContainer.Clear();
            drawTextContainer.Width = 352;
            drawTextContainer.Add(new Sprite
            {
                RelativeSizeAxes = Axes.Both,
                Texture = colour == TeamColour.Red ? textures.Get("RCB") : textures.Get("BCB")
            });

            drawTextContainer.FadeIn(100);
            drawTextContainer.ResizeWidthTo(200, 150, Easing.Out);

            using (BeginDelayedSequence(12000))
            {
                drawTextContainer.FadeOut(100);
            }
        }

        private readonly BindableBool waitForResult = new BindableBool();

        private void getResult()
        {
            if (!waitForResult.Value || !roundInfo.ConfirmedByApi.Value) return;

            if (CurrentMatch.Value == null)
                return;

            waitForResult.Value = false;

            scoreWarningContainer.FadeOut(100);

            if (roundInfo.Score1.Value > roundInfo.Score2.Value)
            {
                // 黄金加成
                CurrentMatch.Value.Team1Coin.Value += WINNER_BONUS + (isTB ? EXTRA_WINNER_BONUS_TB : 0);
                CurrentMatch.Value.Team2Coin.Value += Math.Round((double)roundInfo.Score2.Value / Math.Max(roundInfo.Score1.Value, 1) * 100, 2, MidpointRounding.AwayFromZero);
                showDraw(TeamColour.Red);
            }
            else
            {
                CurrentMatch.Value.Team2Coin.Value += WINNER_BONUS + (isTB ? EXTRA_WINNER_BONUS_TB : 0);
                CurrentMatch.Value.Team1Coin.Value += Math.Round((double)roundInfo.Score1.Value / Math.Max(roundInfo.Score2.Value, 1) * 100, 2, MidpointRounding.AwayFromZero);;
                showDraw(TeamColour.Blue);
            }

            scoreDisplay.ShowSuccess.Value = true;
            var lastPick = CurrentMatch.Value.PicksBans.LastOrDefault(p => p.Type == ChoiceType.Pick);

            if (lastPick != null)
            {
                lastPick.CalculatedByApi = true;
            }
        }

        private void attemptGetResult()
        {
            var lastPick = CurrentMatch.Value?.PicksBans.LastOrDefault(p => p.Type == ChoiceType.Pick);
            if (warmup.Value || CurrentMatch.Value == null || lastPick?.CalculatedByApi != false) return;

            waitForResult.Value = true;
            listener.FetchMatch();
            roundInfo.CanShowResult.Value = true;
            getResult();
        }

        private void updateState()
        {
            try
            {
                scheduledScreenChange?.Cancel();

                if (State.Value == TourneyState.Ranking && lastState == TourneyState.Playing)
                {
                    attemptGetResult();
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
                        listener.FetchMatch();
                        if (listener.CurrentlyPlaying.Value)
                            MatchStarted();
                        break;
                }
            }
            finally
            {
                lastState = State.Value;
            }
        }

        public void MatchStarted()
        {
            HideRoundPreview();
            expand();
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
