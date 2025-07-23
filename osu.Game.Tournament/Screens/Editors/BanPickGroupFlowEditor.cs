// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays.Settings;
using osu.Game.Tournament.Models;
using osuTK;

namespace osu.Game.Tournament.Screens.Editors
{
    public partial class BanPickGroupFlowEditor : TournamentEditorScreen<BanPickGroupFlowEditor.BanPickGroupRow, BanPickFlowGroup>
    {
        private readonly TournamentRound round;

        public BanPickGroupFlowEditor(TournamentRound round, TournamentScreen parentScreen)
            : base(parentScreen)
        {
            this.round = round;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            ControlPanel.Add(new TourneyButton
            {
                Text = "Apply to all rounds",
                Action = applyToAllRounds
            });
        }

        private void applyToAllRounds()
        {
            foreach (var otherRound in LadderInfo.Rounds.Except(new[] { round }))
            {
                otherRound.BanPickFlowGroups.Clear();

                otherRound.BanPickFlowGroups.AddRange(round.BanPickFlowGroups);
            }
        }

        protected override BindableList<BanPickFlowGroup> Storage => round.BanPickFlowGroups;

        protected override BanPickGroupRow CreateDrawable(BanPickFlowGroup model)
        {
            return new BanPickGroupRow(round, model);
        }

        public partial class BanPickGroupRow : CompositeDrawable, IModelBacked<BanPickFlowGroup>
        {
            private readonly TournamentRound round;
            public BanPickFlowGroup Model { get; }

            private FillFlowContainer stepsContainer = null!;

            private readonly Bindable<int?> repeatCount = new Bindable<int?>();
            private SettingsNumberBox repeatCountNumberBox = null!;

            public BanPickGroupRow(TournamentRound round, BanPickFlowGroup model)
            {
                this.round = round;
                Model = model;
                AutoSizeAxes = Axes.Y;
                RelativeSizeAxes = Axes.X;
                Masking = true;
                CornerRadius = 10;

                repeatCount.Value = model.RepeatCount.Value;
                repeatCount.BindValueChanged(r => model.RepeatCount.Value = r.NewValue ?? 0);
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        Colour = OsuColour.Gray(0.1f),
                        RelativeSizeAxes = Axes.Both,
                    },
                    new FillFlowContainer
                    {
                        Margin = new MarginPadding(5),
                        Padding = new MarginPadding { Right = 160 },
                        Spacing = new Vector2(5),
                        Direction = FillDirection.Full,
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Children = new Drawable[]
                        {
                            new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Spacing = new Vector2(10),
                                Direction = FillDirection.Horizontal,
                                Children = new Drawable[]
                                {
                                    new SettingsTextBox
                                    {
                                        LabelText = "轮换名",
                                        Width = 0.33f,
                                        Current = Model.Name
                                    },
                                    new TourneyButton
                                    {
                                        Margin = new MarginPadding(10),
                                        Width = 0.1f,
                                        Text = "添加 Step",
                                        Action = () =>
                                        {
                                            var step = new BanPickFlowStep();
                                            Model.Steps.Add(step);
                                            stepsContainer.Add(new BanPickStepRow(step, Model));
                                        }
                                    },
                                    repeatCountNumberBox = new SettingsNumberBox
                                    {
                                        Width = 0.1f,
                                        Current = repeatCount,
                                        LabelText = "重复次数",
                                    },
                                    new DangerousSettingsButton
                                    {
                                        Margin = new MarginPadding(10),
                                        Width = 0.1f,
                                        Text = "删除组",
                                        Action = () =>
                                        {
                                            Expire();
                                            round.BanPickFlowGroups.Remove(Model);
                                        }
                                    }
                                }
                            },
                            stepsContainer = new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Margin = new MarginPadding { Top = 5 },
                                Direction = FillDirection.Vertical
                            }
                        }
                    }
                };

                foreach (var step in Model.Steps)
                    stepsContainer.Add(new BanPickStepRow(step, Model));
            }

            private partial class BanPickStepRow : CompositeDrawable
            {
                private readonly BanPickFlowStep step;
                private readonly BanPickFlowGroup group;

                public BanPickStepRow(BanPickFlowStep step, BanPickFlowGroup group)
                {
                    this.step = step;
                    this.group = group;
                    AutoSizeAxes = Axes.Y;
                    RelativeSizeAxes = Axes.X;
                    Margin = new MarginPadding(10);
                    Masking = true;
                    CornerRadius = 5;
                }

                [BackgroundDependencyLoader]
                private void load()
                {
                    InternalChildren = new Drawable[]
                    {
                        new Box
                        {
                            Colour = OsuColour.Gray(0.2f),
                            RelativeSizeAxes = Axes.Both,
                        },
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Horizontal,
                            Margin = new MarginPadding(5),
                            Padding = new MarginPadding { Right = 160 },
                            Children = new Drawable[]
                            {
                                new LabelledEnumDropdown<ChoiceType>
                                {
                                    Width = 0.3f,
                                    Label = "选图模式",
                                    Current = step.CurrentAction
                                },
                                new SettingsCheckbox
                                {
                                    Width = 0.15f,
                                    LabelText = "交换队伍",
                                    TooltipText = "使用与上次不同的队伍",
                                    Current = step.SwapFromLastColor
                                },
                                new DangerousSettingsButton
                                {
                                    Margin = new MarginPadding(10),
                                    Width = 0.3f,
                                    Text = "删除",
                                    Action = () =>
                                    {
                                        Expire();
                                        group.Steps.Remove(step);
                                    }
                                }
                            }
                        }
                    };
                }
            }
        }
    }
}
