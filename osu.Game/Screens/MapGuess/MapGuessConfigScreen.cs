// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Screens;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Overlays.Settings;
using osuTK;

namespace osu.Game.Screens.MapGuess
{
    public partial class MapGuessConfigScreen : OsuScreen
    {
        private readonly MapGuessConfig config = new MapGuessConfig();
        private readonly BindableBool osu = new BindableBool(true);
        private readonly BindableBool taiko = new BindableBool();
        private readonly BindableBool osuCatch = new BindableBool();
        private readonly BindableBool mania = new BindableBool();
        private readonly OsuSpriteText errorText;

        [Resolved]
        private BeatmapManager beatmaps { get; set; } = null!;

        protected override BackgroundScreen CreateBackground() => new SolidBackgroundScreen();

        public MapGuessConfigScreen()
        {
            Padding = new MarginPadding
            {
                Horizontal = 35,
                Vertical = 40,
            };
            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(10),
                Children =
                [
                    new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Height = 0.2f,
                        Content = new Drawable[][]
                        {
                            [
                                new SettingsEnumDropdown<AutoSkipMode>
                                {
                                    LabelText = "Auto Skip Mode",
                                    Current = config.AutoSkipMode
                                },
                                new SettingsSlider<int>
                                {
                                    LabelText = "Auto Skip",
                                    Current = config.AutoSkip
                                },
                                new SettingsSlider<int>
                                {
                                    LabelText = "Answer show time",
                                    Current = config.ShowAnswerLength
                                },
                            ]
                        }
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Height = 0.2f,
                        Spacing = new Vector2(20),
                        Direction = FillDirection.Vertical,
                        Children =
                        [
                            new OsuSpriteText
                            {
                                Text = RulesetSettingsStrings.Rulesets,
                            },
                            new GridContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Content = new Drawable[][]
                                {
                                    [
                                        new SettingsCheckbox
                                        {
                                            LabelText = "osu!",
                                            Current = osu,
                                        },
                                        new SettingsCheckbox
                                        {
                                            LabelText = "osu!taiko",
                                            Current = taiko,
                                        },
                                        new SettingsCheckbox
                                        {
                                            LabelText = "osu!catch",
                                            Current = osuCatch,
                                        },
                                        new SettingsCheckbox
                                        {
                                            LabelText = "osu!mania",
                                            Current = mania,
                                        },
                                    ]
                                }
                            }
                        ]
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Height = 0.2f,
                        Spacing = new Vector2(20),
                        Direction = FillDirection.Vertical,
                        Children =
                        [
                            new OsuSpriteText
                            {
                                Text = "Initial hints",
                            },
                            new GridContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Content = new Drawable[][]
                                {
                                    [
                                        new SettingsCheckbox
                                        {
                                            LabelText = "Show background",
                                            Current = config.ShowBackground,
                                        },
                                        new SettingsCheckbox
                                        {
                                            LabelText = "Show hitobjects",
                                            Current = config.ShowHitobjects,
                                        },
                                        new SettingsCheckbox
                                        {
                                            LabelText = "Music",
                                            Current = config.Music,
                                        },
                                        new SettingsSlider<float>
                                        {
                                            LabelText = GameplaySettingsStrings.BackgroundBlur,
                                            Current = config.BackgroundBlur
                                        },
                                        new SettingsSlider<int>
                                        {
                                            LabelText = "Preview length",
                                            Current = config.PreviewLength
                                        },
                                    ]
                                }
                            }
                        ]
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(10),
                        Children =
                        [
                            new SettingsButton
                            {
                                Text = "Start",
                                RelativeSizeAxes = Axes.X,
                                Action = startGame
                            },
                            errorText = new OsuSpriteText
                            {
                                Colour = Colour4.Red,
                                Font = OsuFont.Default.With(size: 40)
                            }
                        ]
                    }
                ]
            };

            osu.BindValueChanged(_ => updateRulesets());
            taiko.BindValueChanged(_ => updateRulesets());
            osuCatch.BindValueChanged(_ => updateRulesets());
            mania.BindValueChanged(_ => updateRulesets());
        }

        private void updateRulesets()
        {
            config.Rulesets.Clear();

            if (osu.Value)
                config.Rulesets.Add(0);
            if (taiko.Value)
                config.Rulesets.Add(1);
            if (osuCatch.Value)
                config.Rulesets.Add(2);
            if (mania.Value)
                config.Rulesets.Add(3);
        }

        private void startGame()
        {
            var allBeatmapSets = beatmaps.GetAllUsableBeatmapSets();
            var filteredBeatmaps = allBeatmapSets.Select(bs => bs.Beatmaps.MaxBy(b => b.StarRating)).Where(b => config.Rulesets.Contains(b.Ruleset.OnlineID)).ToArray();
            var filteredBeatmapSets = filteredBeatmaps.Select(b => b.BeatmapSet).ToArray();

            if (filteredBeatmapSets.Length == 0)
            {
                errorText.Text = "No beatmap selected";
                return;
            }

            this.Push(new MapGuessGameScreen(config, filteredBeatmapSets));
        }

        private partial class SolidBackgroundScreen : BackgroundScreen
        {
            private readonly Box background;

            public SolidBackgroundScreen()
            {
                InternalChild = background = new Box
                {
                    Colour = Colour4.Black,
                    RelativeSizeAxes = Axes.Both,
                };
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                background.Colour = colours.B5;
            }

            public override void OnEntering(ScreenTransitionEvent e)
            {
                Show();
            }
        }
    }
}
