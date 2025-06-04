// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays.Settings;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.IPC;
using osu.Game.Tournament.Models;
using osu.Game.Tournament.Screens.Gameplay;
using osu.Game.Tournament.Screens.Gameplay.Components.MatchHeader;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Game.Tournament.Screens.MapPool
{
    public partial class MapPoolScreen : BeatmapInfoScreen
    {
        private FillFlowContainer<FillFlowContainer<TournamentBeatmapPanel>> mapFlows = null!;

        [Resolved]
        private TournamentSceneManager? sceneManager { get; set; }

        private TeamColour pickColour;
        private ChoiceType pickType;
        private readonly BindableBool pendingSelectMapModifyWinner = new BindableBool();
        private readonly Bindable<IBeatmapInfo?> modifyWinnerMap = new Bindable<IBeatmapInfo?>();
        private TournamentSpriteText modifyWinnerMapText = null!;
        private SettingsWinnerDropdown winnerColorDropDown = null!;

        private OsuButton buttonRedProtected = null!;
        private OsuButton buttonBlueProtected = null!;
        private OsuButton buttonRedBan = null!;
        private OsuButton buttonBlueBan = null!;
        private OsuButton buttonRedPick = null!;
        private OsuButton buttonBluePick = null!;
        private ControlPanel controlPanel = null!;

        private AutoAdvancePrompt? scheduledScreenChange;

        protected override bool ShowLogo => true;

        [BackgroundDependencyLoader]
        private void load(MatchIPCInfo ipc, TextureStore store)
        {
            InternalChildren = new Drawable[]
            {
                new TourneyVideo("mappool")
                {
                    Loop = true,
                    RelativeSizeAxes = Axes.Both,
                },
                new Sprite
                {
                    RelativeSizeAxes = Axes.Both,
                    Texture = store.Get("Videos/mappool"),
                    FillMode = FillMode.Fit,
                },
                new MatchHeader
                {
                    ShowScores = true,
                },
                mapFlows = new FillFlowContainer<FillFlowContainer<TournamentBeatmapPanel>>
                {
                    Y = 160,
                    Spacing = new Vector2(10, 10),
                    Direction = FillDirection.Vertical,
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                },
                controlPanel = new ControlPanel
                {
                    Children = new Drawable[]
                    {
                        new TournamentSpriteText
                        {
                            Text = "Current Mode"
                        },
                        buttonRedProtected = new TourneyButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Text = "Red Protect",
                            Action = () => setMode(TeamColour.Red, ChoiceType.Protected)
                        },
                        buttonBlueProtected = new TourneyButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Text = "Blue Protect",
                            Action = () => setMode(TeamColour.Blue, ChoiceType.Protected)
                        },
                        buttonRedBan = new TourneyButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Text = "Red Ban",
                            Action = () => setMode(TeamColour.Red, ChoiceType.Ban)
                        },
                        buttonBlueBan = new TourneyButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Text = "Blue Ban",
                            Action = () => setMode(TeamColour.Blue, ChoiceType.Ban)
                        },
                        buttonRedPick = new TourneyButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Text = "Red Pick",
                            Action = () => setMode(TeamColour.Red, ChoiceType.Pick)
                        },
                        buttonBluePick = new TourneyButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Text = "Blue Pick",
                            Action = () => setMode(TeamColour.Blue, ChoiceType.Pick)
                        },
                        new ControlPanel.Spacer(),
                        new SettingsCheckbox
                        {
                            RelativeSizeAxes = Axes.X,
                            LabelText = "修改地图胜者",
                            Current = { BindTarget = pendingSelectMapModifyWinner }
                        },
                        new TextFlowContainer(f => f.Font = OsuFont.Default.With(size: 10))
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Text = "打开上方开关后选择要修改的地图\n别忘记关了"
                        },
                        modifyWinnerMapText = new TournamentSpriteText
                        {
                            RelativeSizeAxes = Axes.X,
                            AllowMultiline = true,
                            Font = OsuFont.Default.With(size: 10),
                            Text = "当前选择的地图为: 没选吧"
                        },
                        winnerColorDropDown = new SettingsWinnerDropdown
                        {
                            RelativeSizeAxes = Axes.X,
                            LabelText = "胜者队伍"
                        },
                        new ControlPanel.Spacer(),
                        new TourneyButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Text = "Reset",
                            Action = reset
                        },
                        new ControlPanel.Spacer(),
                        new OsuCheckbox
                        {
                            LabelText = "Split display by mods",
                            Current = LadderInfo.SplitMapPoolByMods,
                        },
                        new ControlPanel.Spacer(),
                        new MatchRoundNameTextBox
                        {
                            RelativeSizeAxes = Axes.X,
                        },
                    },
                }
            };

            ipc.Beatmap.BindValueChanged(beatmapChanged);

            modifyWinnerMap.BindValueChanged(m =>
            {
                winnerColorDropDown.Current = new Bindable<TeamColour?>();

                if (m.NewValue == null)
                {
                    modifyWinnerMapText.Text = "当前选择的地图为: 没选吧";
                }
                else
                {
                    Debug.Assert(CurrentMatch.Value != null);

                    var pickChoice = CurrentMatch.Value.PicksBans.FirstOrDefault(p => p.BeatmapID == m.NewValue.OnlineID && p.Type == ChoiceType.Pick);

                    if (pickChoice == null)
                    {
                        modifyWinnerMap.Value = null;
                        winnerColorDropDown.SetNoticeText("选择的图没有被pick过");
                        return;
                    }

                    winnerColorDropDown.Current = pickChoice.Winner;
                    winnerColorDropDown.ClearNoticeText();

                    string beatmapInformation = m.NewValue.Metadata.TitleUnicode;
                    modifyWinnerMapText.Text = $"当前选择的地图为: {beatmapInformation}";
                }
            });
        }

        private Bindable<bool>? splitMapPoolByMods;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            splitMapPoolByMods = LadderInfo.SplitMapPoolByMods.GetBoundCopy();
            splitMapPoolByMods.BindValueChanged(_ => updateDisplay());
        }

        private void beatmapChanged(ValueChangedEvent<TournamentBeatmap?> beatmap)
        {
            if (CurrentMatch.Value?.Round.Value == null)
                return;

            if (pickType != ChoiceType.Pick)
                return;

            // if bans have already been placed, beatmap changes result in a selection being made automatically
            if (beatmap.NewValue?.OnlineID > 0)
                addForBeatmap(beatmap.NewValue.OnlineID);
        }

        private void setMode(TeamColour colour, ChoiceType choiceType)
        {
            pickColour = colour;
            pickType = choiceType;

            buttonRedProtected.Colour = setColour(pickColour == TeamColour.Red && pickType == ChoiceType.Protected);
            buttonBlueProtected.Colour = setColour(pickColour == TeamColour.Blue && pickType == ChoiceType.Protected);
            buttonRedBan.Colour = setColour(pickColour == TeamColour.Red && pickType == ChoiceType.Ban);
            buttonBlueBan.Colour = setColour(pickColour == TeamColour.Blue && pickType == ChoiceType.Ban);
            buttonRedPick.Colour = setColour(pickColour == TeamColour.Red && pickType == ChoiceType.Pick);
            buttonBluePick.Colour = setColour(pickColour == TeamColour.Blue && pickType == ChoiceType.Pick);

            static Color4 setColour(bool active) => active ? Color4.White : Color4.Gray;
        }

        private void setNextMode()
        {
            if (CurrentMatch.Value?.Round.Value == null)
                return;

            int banPickCount = CurrentMatch.Value.PicksBans.Count;
            var groups = CurrentMatch.Value.Round.Value.BanPickFlowGroups;

            int accumulatedSteps = 0;

            BanPickFlowGroup? currentGroup = null;

            foreach (BanPickFlowGroup g in groups)
            {
                accumulatedSteps += g.Steps.Count * (g.RepeatCount.Value + 1);

                if (accumulatedSteps <= banPickCount) continue;

                currentGroup = g;
                break;
            }

            if (currentGroup == null)
            {
                // we've exhausted all defined steps; loop the last group
                currentGroup = groups.LastOrDefault();
                if (currentGroup == null)
                    return;

                int loopIndex = (banPickCount - (accumulatedSteps - currentGroup.Steps.Count)) % currentGroup.Steps.Count;
                BanPickFlowStep loopStep = currentGroup.Steps[loopIndex];

                TeamColour lastPickColour = CurrentMatch.Value.PicksBans.LastOrDefault()?.Team ?? TeamColour.Red;
                TeamColour loopColour = !loopStep.SwapFromLastColor.Value
                    ? lastPickColour
                    : getOppositeTeamColour(lastPickColour);

                setMode(loopColour, loopStep.CurrentAction.Value);
                return;
            }

            int stepsBeforeGroup = accumulatedSteps - currentGroup.Steps.Count;
            int currentIndex = banPickCount - stepsBeforeGroup;

            if (currentIndex < 0 || currentIndex >= currentGroup.Steps.Count)
                return;

            BanPickFlowStep currentStep = currentGroup.Steps[currentIndex];

            TeamColour lastTeam = CurrentMatch.Value.PicksBans.LastOrDefault()?.Team ?? TeamColour.Red;
            TeamColour nextColor = !currentStep.SwapFromLastColor.Value
                ? lastTeam
                : getOppositeTeamColour(lastTeam);

            setMode(nextColor, currentStep.CurrentAction.Value);

            TeamColour getOppositeTeamColour(TeamColour colour) => colour == TeamColour.Red ? TeamColour.Blue : TeamColour.Red;
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            var maps = mapFlows.Select(f => f.FirstOrDefault(m => m.ReceivePositionalInputAt(e.ScreenSpaceMousePosition)));
            var map = maps.FirstOrDefault(m => m != null);

            if (map != null)
            {
                if (e.Button == MouseButton.Left && map.Beatmap?.OnlineID > 0)
                {
                    if (pendingSelectMapModifyWinner.Value)
                    {
                        modifyWinnerMap.Value = map.Beatmap;
                    }
                    else
                    {
                        addForBeatmap(map.Beatmap.OnlineID);
                    }
                }
                else
                {
                    var existing = CurrentMatch.Value?.PicksBans.FirstOrDefault(p => p.BeatmapID == map.Beatmap?.OnlineID);

                    if (existing != null)
                    {
                        CurrentMatch.Value?.PicksBans.Remove(existing);
                        setNextMode();
                    }
                }

                return true;
            }

            return base.OnMouseDown(e);
        }

        private void reset()
        {
            CurrentMatch.Value?.PicksBans.Clear();
            setNextMode();
        }

        private void addForBeatmap(int beatmapId)
        {
            if (CurrentMatch.Value?.Round.Value == null)
                return;

            if (CurrentMatch.Value.Round.Value.Beatmaps.All(b => b.Beatmap?.OnlineID != beatmapId))
                // don't attempt to add if the beatmap isn't in our pool
                return;

            if (pickType == ChoiceType.Protected && CurrentMatch.Value.PicksBans.Any(p => p.BeatmapID == beatmapId))
                return;

            if (pickType == ChoiceType.Ban && CurrentMatch.Value.PicksBans.Any(p => p.BeatmapID == beatmapId))
                // don't ban if already protected.
                return;

            if (pickType == ChoiceType.Pick && CurrentMatch.Value.PicksBans.Any(p => p.BeatmapID == beatmapId && p.Type != ChoiceType.Protected))
                // don't pick if map already in pickbans unless is protected.
                return;

            CurrentMatch.Value.PicksBans.Add(new BeatmapChoice
            {
                Team = pickColour,
                Type = pickType,
                BeatmapID = beatmapId
            });

            if (LadderInfo.AutoProgressScreens.Value)
            {
                if (pickType == ChoiceType.Pick && CurrentMatch.Value.PicksBans.Any(i => i.Type == ChoiceType.Pick))
                {
                    scheduledScreenChange?.Cancel();
                    controlPanel.Add(scheduledScreenChange = new AutoAdvancePrompt(() => { sceneManager?.SetScreen(typeof(GameplayScreen)); }, 10000));
                }
            }

            setNextMode();
        }

        public override void Hide()
        {
            scheduledScreenChange?.Cancel();
            base.Hide();
        }

        protected override void CurrentMatchChanged(ValueChangedEvent<TournamentMatch?> match)
        {
            base.CurrentMatchChanged(match);
            setNextMode();
            updateDisplay();
        }

        private void updateDisplay()
        {
            mapFlows.Clear();

            if (CurrentMatch.Value == null)
                return;

            int totalRows = 0;

            if (CurrentMatch.Value.Round.Value != null)
            {
                FillFlowContainer<TournamentBeatmapPanel>? currentFlow = null;
                string? currentMods = null;
                int flowCount = 0;
                int currentModCount = 1;

                var g = CurrentMatch.Value.Round.Value.Beatmaps.GroupBy(b => b.Mods).ToDictionary(f => f.Key, f => f.Count());

                foreach (var b in CurrentMatch.Value.Round.Value.Beatmaps)
                {
                    if (currentFlow == null || (LadderInfo.SplitMapPoolByMods.Value && currentMods != b.Mods))
                    {
                        mapFlows.Add(currentFlow = new FillFlowContainer<TournamentBeatmapPanel>
                        {
                            Spacing = new Vector2(10, 5),
                            Direction = FillDirection.Full,
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y
                        });

                        currentMods = b.Mods;

                        totalRows++;
                        flowCount = 0;
                        currentModCount = 0;
                    }

                    if (++flowCount > 2)
                    {
                        totalRows++;
                        flowCount = 1;

                        if (g[b.Mods] % 3 == 1)
                        {
                            mapFlows.Add(currentFlow = new FillFlowContainer<TournamentBeatmapPanel>
                            {
                                Spacing = new Vector2(10, 5),
                                Direction = FillDirection.Full,
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Padding = new MarginPadding
                                {
                                    // remove horizontal padding to increase flow width to 3 panels
                                    Horizontal = 100
                                }
                            });
                        }
                    }

                    currentFlow.Add(new TournamentBeatmapPanel(b, g[b.Mods] > 1 ? ++currentModCount : null, isMappool: true)
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Height = 42,
                    });
                }
            }

            mapFlows.Padding = new MarginPadding(5)
            {
                // remove horizontal padding to increase flow width to 3 panels
                Horizontal = totalRows > 9 ? 0 : 100
            };
        }
    }
}
