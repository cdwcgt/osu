// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Resources.Localisation.Web;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens.Play.HUD;
using osuTK;
using osuTK.Graphics;
using osu.Game.Rulesets;
using osu.Game.Graphics.Containers;

namespace osu.Game.Screens.Play
{
    /// <summary>
    /// Displays beatmap metadata inside <see cref="PlayerLoader"/>
    /// </summary>
    public partial class BeatmapMetadataDisplay : Container
    {
        private readonly IWorkingBeatmap beatmap;
        private readonly Bindable<IReadOnlyList<Mod>> mods;
        private readonly Drawable logoFacade;
        private LoadingSpinner loading;
        private SelectedRulesetIcon ruleseticon;

        public IBindable<IReadOnlyList<Mod>> Mods => mods;

        private bool isLoading;

        public bool Loading
        {
            get => isLoading;
            set
            {
                if (value)
                {
                    loading.Show();
                    ruleseticon.Hide();
                }
                else
                {
                    loading.Hide();

                    if (Optui.Value)
                        ruleseticon.Show();
                    else
                        ruleseticon.Hide();
                }

                isLoading = value;
            }
        }

        public BeatmapMetadataDisplay(IWorkingBeatmap beatmap, Bindable<IReadOnlyList<Mod>> mods, Drawable logoFacade)
        {
            this.beatmap = beatmap;
            this.logoFacade = logoFacade;

            this.mods = new Bindable<IReadOnlyList<Mod>>();
            this.mods.BindTo(mods);
        }

        private Container bg;
        public readonly Bindable<bool> Optui = new Bindable<bool>();

        private IBindable<StarDifficulty?> starDifficulty;

        private FillFlowContainer versionFlow;
        private StarRatingDisplay starRatingDisplay;

        [BackgroundDependencyLoader]
        private void load(BeatmapDifficultyCache difficultyCache, MConfigManager config)
        {
            var metadata = beatmap.BeatmapInfo.Metadata;

            AutoSizeAxes = Axes.Both;
            Children = new Drawable[]
            {
                bg = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                    CornerRadius = 20,
                    CornerExponent = 2.5f,
                    Masking = true,
                    BorderColour = Color4.Black,
                    BorderThickness = 4.5f,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Alpha = 0.5f,
                            Colour = Color4.Black
                        }
                    }
                },
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                    Direction = FillDirection.Vertical,
                    Margin = new MarginPadding(40),
                    Children = new[]
                    {
                        logoFacade.With(d =>
                        {
                            d.Anchor = Anchor.TopCentre;
                            d.Origin = Anchor.TopCentre;
                        }),
                        new OsuSpriteText
                        {
                            Text = new RomanisableString(metadata.TitleUnicode, metadata.Title),
                            Font = OsuFont.GetFont(size: 36, italics: true),
                            Origin = Anchor.TopCentre,
                            Anchor = Anchor.TopCentre,
                            Margin = new MarginPadding { Top = 15 }
                        },
                        new OsuSpriteText
                        {
                            Text = new RomanisableString(metadata.ArtistUnicode, metadata.Artist),
                            Font = OsuFont.GetFont(size: 26, italics: true),
                            Origin = Anchor.TopCentre,
                            Anchor = Anchor.TopCentre
                        },
                        new Container
                        {
                            Size = new Vector2(300, 60),
                            Margin = new MarginPadding(10),
                            Origin = Anchor.TopCentre,
                            Anchor = Anchor.TopCentre,
                            CornerRadius = 10,
                            Masking = true,
                            Children = new Drawable[]
                            {
                                new Sprite
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Texture = beatmap.Background,
                                    Origin = Anchor.Centre,
                                    Anchor = Anchor.Centre,
                                    FillMode = FillMode.Fill,
                                },
                                loading = new LoadingLayer(true),
                                ruleseticon = new SelectedRulesetIcon()
                            }
                        },
                        versionFlow = new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(5f),
                            Margin = new MarginPadding { Bottom = 40 },
                            Children = new Drawable[]
                            {
                                new OsuSpriteText
                                {
                                    Text = beatmap.BeatmapInfo.DifficultyName,
                                    Font = OsuFont.GetFont(size: 26, italics: true),
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                },
                                starRatingDisplay = new StarRatingDisplay(default)
                                {
                                    Alpha = 0f,
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                }
                            }
                        },
                        new GridContainer
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            AutoSizeAxes = Axes.Both,
                            RowDimensions = new[]
                            {
                                new Dimension(GridSizeMode.AutoSize),
                                new Dimension(GridSizeMode.AutoSize),
                            },
                            ColumnDimensions = new[]
                            {
                                new Dimension(GridSizeMode.AutoSize),
                                new Dimension(GridSizeMode.AutoSize),
                            },
                            Content = new[]
                            {
                                new Drawable[]
                                {
                                    new MetadataLineLabel(BeatmapsetsStrings.ShowInfoSource),
                                    new MetadataLineInfo(metadata.Source)
                                },
                                new Drawable[]
                                {
                                    new MetadataLineLabel("谱师"),
                                    new MetadataLineInfo(metadata.Author.Username)
                                }
                            }
                        },
                        new ModDisplay
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Margin = new MarginPadding { Top = 20 },
                            Current = mods
                        }
                    }
                }
            };

            starDifficulty = difficultyCache.GetBindableDifficulty(beatmap.BeatmapInfo);

            Loading = true;

            config.BindWith(MSetting.OptUI, Optui);
            Optui.BindValueChanged(updateVisualEffects);

            entryAnimation();
        }

        private void updateVisualEffects(ValueChangedEvent<bool> v)
        {
            switch (v.NewValue)
            {
                case true:
                    if (!isLoading)
                        ruleseticon.Show();

                    bg.ResizeHeightTo(1, 300, Easing.OutCubic).FadeIn(300, Easing.OutQuint);
                    return;

                case false:
                    ruleseticon.Hide();
                    bg.ResizeHeightTo(0.6f, 200, Easing.OutCubic).FadeOut(200, Easing.OutQuint);
                    return;
            }
        }

        private void entryAnimation()
        {
            bg.ScaleTo(1).FadeOut().ResizeHeightTo(0);

            switch (Optui.Value)
            {
                case true:
                    bg.Delay(750).ResizeHeightTo(1, 500, Easing.OutQuint).FadeIn(500, Easing.OutQuint);
                    return;

                case false:
                    ruleseticon.Hide();
                    return;
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (starDifficulty.Value != null)
            {
                starRatingDisplay.Current.Value = starDifficulty.Value.Value;
                starRatingDisplay.Show();
            }
            else
                starRatingDisplay.Hide();

            starDifficulty.ValueChanged += d =>
            {
                Debug.Assert(d.NewValue != null);

                starRatingDisplay.Current.Value = d.NewValue.Value;

                versionFlow.AutoSizeDuration = 300;
                versionFlow.AutoSizeEasing = Easing.OutQuint;

                starRatingDisplay.FadeIn(300, Easing.InQuint);
            };
        }

        private partial class MetadataLineLabel : OsuSpriteText
        {
            public MetadataLineLabel(LocalisableString text)
            {
                Anchor = Anchor.TopRight;
                Origin = Anchor.TopRight;
                Margin = new MarginPadding { Right = 5 };
                Colour = OsuColour.Gray(0.8f);
                Text = text;
            }
        }

        private partial class MetadataLineInfo : OsuSpriteText
        {
            public MetadataLineInfo(string text)
            {
                Margin = new MarginPadding { Left = 5 };
                Text = string.IsNullOrEmpty(text) ? @"-" : text;
            }
        }

        private class SelectedRulesetIcon : Container
        {
            private Container rulesetTextContainer;
            private ConstrainedIconContainer icon;

            private const float target_width = 200;

            [Resolved]
            private IBindable<RulesetInfo> currentRuleset { get; set; }

            [BackgroundDependencyLoader]
            private void load()
            {
                var ruleset = currentRuleset?.Value?.CreateInstance();

                Size = new Vector2(0, 40);
                Alpha = 0;
                Origin = Anchor.Centre;
                Anchor = Anchor.Centre;
                CornerRadius = 10;
                Masking = true;
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0.8f,
                        Colour = Color4.Black,
                    },
                    new FillFlowContainer
                    {
                        Direction = FillDirection.Horizontal,
                        RelativeSizeAxes = Axes.Both,
                        Spacing = new Vector2(10),
                        LayoutDuration = 500,
                        LayoutEasing = Easing.OutQuint,
                        Children = new Drawable[]
                        {
                            icon = new ConstrainedIconContainer
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Icon = ruleset?.CreateIcon() ?? new SpriteIcon { Icon = FontAwesome.Regular.QuestionCircle },
                                Colour = Color4.White,
                                Size = new Vector2(20),
                                Scale = new Vector2(2),
                                Alpha = 0
                            },
                            rulesetTextContainer = new Container
                            {
                                AutoSizeAxes = Axes.Both,
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Scale = new Vector2(0.9f),
                                Alpha = 0,
                                Child = new OsuSpriteText
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Text = ruleset?.Description ?? "missingno"
                                }
                            }
                        }
                    }
                };
            }

            private bool isFirstShow = true;

            public override void Show()
            {
                this.ResizeWidthTo(target_width, 300, Easing.OutQuint).FadeIn(300, Easing.OutQuint);

                if (isFirstShow)
                {
                    rulesetTextContainer.ScaleTo(1, 500)
                                        .OnComplete(_ => rulesetTextContainer.FadeIn(500, Easing.OutQuint));

                    icon.ScaleTo(1, 500, Easing.OutQuint).FadeIn(500, Easing.OutQuint);
                }

                isFirstShow = false;
            }

            public override void Hide()
            {
                this.ResizeWidthTo(target_width * 0.6f, 200, Easing.OutCubic).FadeOut(200, Easing.OutQuint);
            }
        }
    }
}
