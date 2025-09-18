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
using osu.Game.Tournament.IPC.MemoryIPC;
using osu.Game.Tournament.Models;
using osu.Game.Tournament.Screens.Gameplay.Components;
using osu.Game.Tournament.Screens.Gameplay.Components.MatchHeader;
using osu.Game.Tournament.Screens.MapPool;
using osu.Game.Tournament.Screens.TeamWin;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Gameplay
{
    public partial class GameplayScreen : BeatmapInfoScreen, IScreenNeedHideBeforeAction
    {
        private readonly BindableBool warmup = new BindableBool();

        public readonly Bindable<TourneyState> State = new Bindable<TourneyState>();
        private OsuButton warmupButton = null!;
        private MemoryBasedIPCWithMatchListener ipc = null!;
        private Sprite slotSprite = null!;
        private Sprite fetchFailedWarning = null!;

        private PlayerArea redArea = null!;
        private PlayerArea blueArea = null!;

        private Container matchHeaderContainer = null!;
        private RoundInformationPreview roundPreview = null!;

        [Resolved]
        private TournamentSceneManager? sceneManager { get; set; }

        [Resolved]
        private TextureStore textures { get; set; } = null!;

        [Resolved]
        private TournamentMatchChatDisplay chat { get; set; } = null!;

        [Resolved]
        private MatchHeader header { get; set; } = null!;

        private Drawable chroma = null!;

        private Bindable<double?> team1Coin = new Bindable<double?>();
        private Bindable<double?> team2Coin = new Bindable<double?>();

        protected override SongBar CreateSongBar() => new GameplaySongBar
        {
            Depth = float.MinValue,
        };

        protected override bool FetchDataFromMemoryThisScreen => true;

        private GameplaySongBar gameplaySongBar => (GameplaySongBar)SongBar;

        protected override bool ShowLogo => true;

        private bool switchFromMappool;

        [BackgroundDependencyLoader]
        private void load(MatchIPCInfo matchIpc, TextureStore store)
        {
            ipc = (matchIpc as MemoryBasedIPCWithMatchListener)!;

            AddRangeInternal(new Drawable[]
            {
                new TourneyVideo("gameplay")
                {
                    Loop = true,
                    RelativeSizeAxes = Axes.Both,
                },
                new Sprite
                {
                    RelativeSizeAxes = Axes.Both,
                    Texture = store.Get("Videos/gameplay"),
                    FillMode = FillMode.Fit,
                },
                matchHeaderContainer = new Container(),
                withdrawTextContainer = new Container
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
                        Text = "回合进行中获取的分数在提现模式中无法参考，结束后将会自动(?)获取分数。",
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
                            Width = 1366,
                            Children = new Drawable[]
                            {
                                redArea = new PlayerArea(TeamColour.Red)
                                {
                                    Name = "Left PlayerArea",
                                    RelativeSizeAxes = Axes.Both,
                                    Width = 0.5f,
                                },
                                blueArea = new PlayerArea(TeamColour.Blue)
                                {
                                    Name = "Right PlayerArea",
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
                },
                new Container
                {
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Padding = new MarginPadding { Vertical = 10 },
                    Height = 150,
                    Width = 370,
                    Depth = float.NegativeInfinity,
                    Child = fetchFailedWarning = new Sprite
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0f,
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre,
                        Texture = store.Get("fetch-failed"),
                        FillMode = FillMode.Fit,
                    }
                }
            });

            ControlPanel.AddRange(new Drawable[]
            {
                warmupButton = new TourneyButton
                {
                    RelativeSizeAxes = Axes.X,
                    Text = "切换热手",
                    Action = () => warmup.Toggle()
                },
                new TourneyButton
                {
                    RelativeSizeAxes = Axes.X,
                    Text = "切换聊天",
                    Action = () =>
                    {
                        if (!chat.IsPresent)
                        {
                            chat.Expand();
                        }
                        else
                        {
                            chat.Contract();
                        }
                    }
                },
                new SettingsSlider<int>
                {
                    LabelText = $"{(OperatingSystem.IsWindows() ? "玩家区域" : "绿幕")} width",
                    Current = LadderInfo.ChromaKeyWidth,
                    KeyboardStep = 1,
                },
                OperatingSystem.IsWindows()
                    ? new SettingsSlider<int>
                    {
                        LabelText = "抓取帧数限制",
                        Current = LadderInfo.FrameRate,
                        KeyboardStep = 1,
                    }
                    : Empty(),
                new SettingsSlider<int>
                {
                    LabelText = "每队玩家数",
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
                    Text = "切换选图信息",
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
                    LabelText = "Team1 币"
                },
                team2CoinText = new TourneyNumberBox
                {
                    LabelText = "Team2 币"
                },
                new TourneyButton
                {
                    Text = "应用币修改",
                    Action = () =>
                    {
                        team1Coin.Value = team1CoinText.Current.Value;
                        team2Coin.Value = team2CoinText.Current.Value;
                    }
                },
                matchID = new SettingsNumberBox
                {
                    LabelText = "Mplink ID",
                },
                listeningButton = new TourneyButton
                {
                    Text = "开始监听",
                },
                new TourneyButton
                {
                    Text = "红猪",
                    Action = () =>
                    {
                        ipc.CurrentRoundAborted();
                    }
                },
                team1Score = new SettingsNumberBox
                {
                    LabelText = "队伍1分数",
                },
                team2Score = new SettingsNumberBox
                {
                    LabelText = "队伍2分数",
                },
                new TourneyButton
                {
                    Text = "应用分数",
                    Action = () =>
                    {
                        ipc.AddFakeEvent(team1Score.Current.Value ?? 0, team2Score.Current.Value ?? 0);
                    }
                },
                new TourneyButton
                {
                    Text = "红飞",
                    Action = redArea.Launch
                },
                new TourneyButton
                {
                    Text = "蓝飞",
                    Action = blueArea.Launch
                },
                new TourneyButton
                {
                    Text = "飞重置",
                    Action = () =>
                    {
                        redArea.Reset();
                        blueArea.Reset();
                    }
                }
            });

            LadderInfo.ChromaKeyWidth.BindValueChanged(width => chroma.Width = width.NewValue, true);

            warmup.BindValueChanged(w =>
            {
                warmupButton.Alpha = !w.NewValue ? 0.5f : 1;
                warmupButton.BackgroundColour = w.NewValue ? Color4.Red : Color4Extensions.FromHex(@"44aadd");
                header.ShowScores = !w.NewValue;
            }, true);

            sceneManager?.CurrentScreen.BindValueChanged(s =>
            {
                if (s.OldValue == typeof(MapPoolScreen) && s.NewValue == typeof(GameplayScreen))
                    switchFromMappool = true;
            });

            ipc.CurrentlyListening.BindValueChanged(s =>
            {
                if (s.NewValue)
                {
                    listeningButton.Text = "停止监听";
                    listeningButton.Action = ipc.StopListening;
                }
                else
                {
                    listeningButton.Text = "开始监听";
                    listeningButton.Action = () => ipc.StartListening(matchID.Current.Value);
                }
            }, true);

            ((GameplaySongBar)SongBar).WaitForResult.BindTo(waitForResult);

            ipc.CurrentlyPlaying.BindValueChanged(p =>
            {
                updateState();
            });

            ipc.MatchFinished += getResult;
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
            {
                roundPreview.FadeIn(200);
            }

            roundPreviewShow = true;
            return true;
        }

        public void HideRoundPreview()
        {
            scheduledHideRoundPreview?.Cancel();

            if (!roundPreviewShow)
                return;

            scheduledHideRoundPreview?.Cancel();

            roundPreview.FadeOut(100);

            using (SongBar.BeginDelayedSequence(200))
            {
                SongBar.FadeIn(200);
            }

            using (chat.BeginDelayedSequence(200))
            {
                chat.Expand();
            }

            roundPreviewShow = false;
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

        private AutoAdvancePrompt? scheduledScreenChange;
        private ScheduledDelegate? scheduledContract;
        private ScheduledDelegate? scheduledShowRoundPreview;
        private ScheduledDelegate? scheduledHideRoundPreview;

        private TournamentMatchScoreDisplay scoreDisplay = null!;

        private TourneyState lastState;
        private TourneyNumberBox team1CoinText = null!;
        private TourneyNumberBox team2CoinText = null!;
        private Container withdrawTextContainer = null!;
        private SettingsNumberBox matchID = null!;
        private TourneyButton listeningButton = null!;
        private Container scoreWarningContainer = null!;

        private void contract()
        {
            if (!IsLoaded)
                return;

            HideRoundPreview();

            scheduledContract?.Cancel();

            gameplaySongBar.Expanded = false;
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
            gameplaySongBar.Expanded = true;

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

        private void showDraw(TeamColour colour)
        {
            withdrawTextContainer.Clear();
            withdrawTextContainer.Width = 352;
            withdrawTextContainer.Add(new Sprite
            {
                RelativeSizeAxes = Axes.Both,
                Texture = colour == TeamColour.Red ? textures.Get("RCB") : textures.Get("BCB")
            });

            withdrawTextContainer.FadeIn(100);
            withdrawTextContainer.ResizeWidthTo(200, 150, Easing.Out);

            using (BeginDelayedSequence(12000))
            {
                withdrawTextContainer.FadeOut(100);
            }
        }

        private readonly BindableBool waitForResult = new BindableBool();
        private SettingsNumberBox team1Score;
        private SettingsNumberBox team2Score;

        private void getResult(bool fromApi)
        {
            if (warmup.Value || CurrentMatch.Value == null)
                return;

            if (ipc.State.Value == TourneyState.Playing)
                return;

            waitForResult.Value = false;

            scoreWarningContainer.FadeOut(100);

            if (ipc.Score1.Value > ipc.Score2.Value)
            {
                // 黄金加成
                CurrentMatch.Value.Team1Coin.Value += TournamentGame.WINNER_BONUS + (isTB ? TournamentGame.EXTRA_WINNER_BONUS_TB : 0);
                CurrentMatch.Value.Team2Coin.Value +=
                    Math.Min(Math.Round((double)ipc.Score2.Value / Math.Max(ipc.Score1.Value, 1) * 100, 2, MidpointRounding.AwayFromZero),
                        93.5);
                showDraw(TeamColour.Red);
            }
            else
            {
                CurrentMatch.Value.Team2Coin.Value += TournamentGame.WINNER_BONUS + (isTB ? TournamentGame.EXTRA_WINNER_BONUS_TB : 0);
                CurrentMatch.Value.Team1Coin.Value +=
                    Math.Min(Math.Round((double)ipc.Score1.Value / Math.Max(ipc.Score2.Value, 1) * 100, 2, MidpointRounding.AwayFromZero),
                        93.5);
                showDraw(TeamColour.Blue);
            }

            if (!fromApi)
            {
                fetchFailedWarning.ScaleTo(1).FadeIn(100).RotateTo(365, 500, Easing.OutQuint).Then()
                                  .RotateTo(360, 50, Easing.OutQuint).Then(10_000).ScaleTo(0, 100).FadeOut(100);
            }
        }

        private void attemptGetResult()
        {
            if (warmup.Value || CurrentMatch.Value == null || !ipc.CurrentlyPlaying.Value) return;

            waitForResult.Value = true;
            ipc.RequestCurrentRoundResultFromApi();
        }

        private void updateState()
        {
            try
            {
                scheduledScreenChange?.Cancel();

                if (State.Value == TourneyState.Ranking && lastState == TourneyState.Playing)
                {
                    if (warmup.Value || CurrentMatch.Value == null) return;

                    attemptGetResult();

                    var lastPick = CurrentMatch.Value.PicksBans.LastOrDefault(p => p.Type == ChoiceType.Pick && p.BeatmapID == ipc.Beatmap.Value?.OnlineID);

                    if (lastPick?.Winner.Value != null)
                        return;

                    // if (ipc.Score1.Value > ipc.Score2.Value)
                    // {
                    //     CurrentMatch.Value.Team1Score.Value++;
                    //     if (lastPick != null) lastPick.Winner.Value = TeamColour.Red;
                    // }
                    // else
                    // {
                    //     CurrentMatch.Value.Team2Score.Value++;
                    //     if (lastPick != null) lastPick.Winner.Value = TeamColour.Blue;
                    // }
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
                                    scheduledScreenChange = new AutoAdvancePrompt(() => { sceneManager?.SetScreen(typeof(TeamWinScreen)); }, delay_before_progression);
                                else if (CurrentMatch.Value?.Completed.Value == false)
                                    scheduledScreenChange = new AutoAdvancePrompt(() => { sceneManager?.SetScreen(typeof(MapPoolScreen)); }, delay_before_progression);

                                if (scheduledScreenChange != null)
                                    ControlPanel.Add(scheduledScreenChange);
                            }
                        }

                        break;

                    case TourneyState.Ranking:
                        scheduledContract = Scheduler.AddDelayed(contract, 10000);
                        break;

                    case TourneyState.WaitingForClients:
                    case TourneyState.Playing:
                        var lastPick = CurrentMatch.Value?.PicksBans.LastOrDefault(p => p.Type == ChoiceType.Pick);
                        ipc.BindChoiceToNextOrCurrentMatch(lastPick);
                        HideRoundPreview();
                        expand();
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

        public void BeforeHide()
        {
            header.ReturnProxy();
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
                        scheduledHideRoundPreview = Scheduler.AddDelayed(HideRoundPreview, 30000);
                }, 5000);
            }

            header.ProxyToContainer(matchHeaderContainer);

            base.Show();
        }

        private partial class PlayerArea : CompositeDrawable
        {
            [Resolved]
            private LadderInfo ladder { get; set; } = null!;

            private TeamColour teamColour;
            private Container? loseTextContainer;

            public PlayerArea(TeamColour teamColour)
            {
                this.teamColour = teamColour;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                ladder.PlayersPerTeam.BindValueChanged(performLayout, true);
            }

            public void Launch()
            {
                double delayTime = 0;
                bool clockWise = true;

                foreach (var player in InternalChildren.OfType<PlayerWindow>())
                {
                    using (player.BeginDelayedSequence(delayTime))
                    {
                        player.FlyingLaunch(clockWise);
                    }

                    delayTime += 300;
                    clockWise = !clockWise;
                }

                loseTextContainer?.RotateTo(150).Delay(delayTime + 1200).FadeIn(1000).RotateTo(0, 1000, Easing.OutCubic);
            }

            public void Reset()
            {
                double delayTime = 0;

                foreach (var player in InternalChildren.OfType<PlayerWindow>())
                {
                    using (player.BeginDelayedSequence(delayTime))
                    {
                        player.Reset();
                    }

                    delayTime += 300;
                }

                loseTextContainer?.Delay(delayTime + 1200).FadeOut(300);
            }

            private void performLayout(ValueChangedEvent<int> playerCount)
            {
                if (!OperatingSystem.IsWindows())
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

                    AddInternal(loseTextSprite());
                    return;
                }

                int clientIndex = teamColour == TeamColour.Red ? 0 : playerCount.NewValue;

                switch (playerCount.NewValue)
                {
                    case 1:
                        InternalChildren = new Drawable[]
                        {
                            new PlayerWindow($"{TournamentGame.TOURNAMENT_CLIENT_NAME}{clientIndex}")
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                RelativeSizeAxes = Axes.Both,
                            }
                        };
                        break;

                    case 2:
                        InternalChildren = new Drawable[]
                        {
                            new PlayerWindow($"{TournamentGame.TOURNAMENT_CLIENT_NAME}{clientIndex++}")
                            {
                                RelativeSizeAxes = Axes.Both,
                                Height = 0.5f,
                                RelativeAnchorPosition = new Vector2(0.25f, 0.5f),
                                Origin = Anchor.Centre,
                            },
                            new PlayerWindow($"{TournamentGame.TOURNAMENT_CLIENT_NAME}{clientIndex}")
                            {
                                RelativeSizeAxes = Axes.Both,
                                Height = 0.5f,
                                RelativeAnchorPosition = new Vector2(0.75f, 0.5f),
                                Origin = Anchor.Centre,
                            }
                        };
                        break;

                    case 3:
                        InternalChildren = new Drawable[]
                        {
                            new PlayerWindow($"{TournamentGame.TOURNAMENT_CLIENT_NAME}{clientIndex++}")
                            {
                                RelativeSizeAxes = Axes.Both,
                                Width = 0.5f,
                                Height = 0.5f,
                                RelativeAnchorPosition = new Vector2(0.5f, 0.25f),
                                Origin = Anchor.Centre,
                            },
                            new PlayerWindow($"{TournamentGame.TOURNAMENT_CLIENT_NAME}{clientIndex++}")
                            {
                                RelativeSizeAxes = Axes.Both,
                                Width = 0.5f,
                                Height = 0.5f,
                                RelativeAnchorPosition = new Vector2(0.25f, 0.75f),
                                Origin = Anchor.Centre,
                            },
                            new PlayerWindow($"{TournamentGame.TOURNAMENT_CLIENT_NAME}{clientIndex}")
                            {
                                RelativeSizeAxes = Axes.Both,
                                Width = 0.5f,
                                Height = 0.5f,
                                RelativeAnchorPosition = new Vector2(0.75f, 0.75f),
                                Origin = Anchor.Centre,
                            },
                        };
                        break;

                    case 4:
                        InternalChildren = new Drawable[]
                        {
                            new PlayerWindow($"{TournamentGame.TOURNAMENT_CLIENT_NAME}{clientIndex++}")
                            {
                                RelativeSizeAxes = Axes.Both,
                                Width = 0.5f,
                                Height = 0.5f,
                                RelativeAnchorPosition = new Vector2(0.25f, 0.25f),
                                Origin = Anchor.Centre,
                            },
                            new PlayerWindow($"{TournamentGame.TOURNAMENT_CLIENT_NAME}{clientIndex++}")
                            {
                                RelativeSizeAxes = Axes.Both,
                                Width = 0.5f,
                                Height = 0.5f,
                                RelativeAnchorPosition = new Vector2(0.75f, 0.25f),
                                Origin = Anchor.Centre,
                            },
                            new PlayerWindow($"{TournamentGame.TOURNAMENT_CLIENT_NAME}{clientIndex++}")
                            {
                                RelativeSizeAxes = Axes.Both,
                                Width = 0.5f,
                                Height = 0.5f,
                                RelativeAnchorPosition = new Vector2(0.25f, 0.75f),
                                Origin = Anchor.Centre,
                            },
                            new PlayerWindow($"{TournamentGame.TOURNAMENT_CLIENT_NAME}{clientIndex}")
                            {
                                RelativeSizeAxes = Axes.Both,
                                Width = 0.5f,
                                Height = 0.5f,
                                RelativeAnchorPosition = new Vector2(0.75f, 0.75f),
                                Origin = Anchor.Centre,
                            },
                        };
                        break;

                    default:
                        throw new ArgumentException("Not Support this player count");
                }

                AddInternal(loseTextSprite());
            }

            private Container loseTextSprite() => loseTextContainer = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Alpha = 0,
                Children = new Drawable[]
                {
                    new TournamentSpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Font = OsuFont.Torus.With(size: 187, weight: FontWeight.Bold),
                        Text = "输了...",
                        Colour = TournamentGame.GetTeamColour(teamColour)
                    },
                    new TournamentSpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Font = OsuFont.Torus.With(size: 175, weight: FontWeight.Bold),
                        Text = "输了...",
                    },
                }
            };
        }
    }
}
