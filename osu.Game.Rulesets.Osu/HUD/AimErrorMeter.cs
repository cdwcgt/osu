﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Pooling;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Rulesets.Osu.Judgements;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Statistics;
using osu.Game.Rulesets.Scoring;
using osu.Game.Screens.Play.HUD.HitErrorMeters;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Osu.HUD
{
    [Cached]
    public partial class AimErrorMeter : HitErrorMeter
    {
        [SettingSource("Judgement position size", "How big of hit point should be.")]
        public BindableNumber<float> JudgementSize { get; } = new BindableNumber<float>(7f)
        {
            MinValue = 0f,
            MaxValue = 12f,
            Precision = 1f
        };

        [SettingSource("Judgement position style", "The style of hit point.")]
        public Bindable<HitStyle> JudgementStyle { get; } = new Bindable<HitStyle>();

        [SettingSource("Average position size", "How big of average position should be.")]
        public BindableNumber<float> AverageSize { get; } = new BindableNumber<float>(12f)
        {
            MinValue = 7f,
            MaxValue = 25f,
            Precision = 1f
        };

        [SettingSource("Average position style", "The style of average position.")]
        public Bindable<HitStyle> AverageStyle { get; } = new Bindable<HitStyle>(HitStyle.Plus);

        [SettingSource("Position Style", "")]
        public Bindable<PositionStyle> HitPositionStyle { get; } = new Bindable<PositionStyle>();

        [Resolved]
        private ScoreProcessor scoreProcessor { get; set; } = null!;

        private Container averagePositionContainer = null!;
        private Container averagePositionRotateContainer = null!;
        private Vector2 averagePosition;

        private readonly DrawablePool<HitPosition> hitPositionPool = new DrawablePool<HitPosition>(20);
        private Container hitPositionsContainer = null!;

        private Container arrowBackgroundContainer = null!;
        private UprightAspectMaintainingContainer rotateFixedContainer = null!;
        private Container mainContainer = null!;

        private float objectRadius;

        private const int max_concurrent_judgements = 30;

        private const float line_thickness = 2;
        private const float inner_portion = 0.85f;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        public AimErrorMeter()
        {
            AutoSizeAxes = Axes.Both;
            AlwaysPresent = true;
        }

        [BackgroundDependencyLoader]
        private void load(IBindable<WorkingBeatmap> beatmap)
        {
            InternalChild = new Container
            {
                Height = 100,
                Width = 100,
                Children = new Drawable[]
                {
                    hitPositionPool,
                    rotateFixedContainer = new UprightAspectMaintainingContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    },
                }
            };

            mainContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new CircularContainer
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        BorderColour = Colour4.White,
                        Masking = true,
                        BorderThickness = 2,
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(inner_portion),
                        Child = new Box
                        {
                            Colour = Colour4.Gray,
                            Alpha = 0.3f,
                            RelativeSizeAxes = Axes.Both
                        },
                    },
                    arrowBackgroundContainer = new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Name = "Arrow Background",
                        RelativeSizeAxes = Axes.Both,
                        Rotation = 45,
                        Alpha = 0f,
                        Children = new Drawable[]
                        {
                            new Circle
                            {
                                RelativeSizeAxes = Axes.Y,
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Height = inner_portion + 0.2f,
                                Width = line_thickness / 2,
                            },
                            new Circle
                            {
                                Height = 5f,
                                Width = line_thickness / 2,
                                Anchor = Anchor.Centre,
                                Origin = Anchor.TopCentre,
                                Margin = new MarginPadding(-line_thickness / 4),
                                RelativePositionAxes = Axes.Both,
                                Y = -(inner_portion + 0.2f) / 2,
                                Rotation = -45
                            },
                            new Circle
                            {
                                Height = 5f,
                                Width = line_thickness / 2,
                                Anchor = Anchor.Centre,
                                Origin = Anchor.TopCentre,
                                Margin = new MarginPadding(-line_thickness / 4),
                                RelativePositionAxes = Axes.Both,
                                Y = -(inner_portion + 0.2f) / 2,
                                Rotation = 45
                            }
                        }
                    },
                    new Container
                    {
                        Name = "Cross Background",
                        RelativeSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            new Circle
                            {
                                RelativeSizeAxes = Axes.Y,
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Alpha = 0.5f,
                                Width = line_thickness,
                                Height = inner_portion * 0.9f
                            },
                            new Circle
                            {
                                RelativeSizeAxes = Axes.Y,
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Alpha = 0.5f,
                                Width = line_thickness,
                                Height = inner_portion * 0.9f,
                                Rotation = 90
                            },
                            new Circle
                            {
                                RelativeSizeAxes = Axes.Y,
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Alpha = 0.2f,
                                Width = line_thickness / 2,
                                Height = inner_portion * 0.9f,
                                Rotation = 45
                            },
                            new Circle
                            {
                                RelativeSizeAxes = Axes.Y,
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Alpha = 0.2f,
                                Width = line_thickness / 2,
                                Height = inner_portion * 0.9f,
                                Rotation = 135
                            },
                        }
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Children = new Drawable[]
                        {
                            hitPositionsContainer = new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre
                            },
                            averagePositionContainer = new UprightAspectMaintainingContainer
                            {
                                RelativePositionAxes = Axes.Both,
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Child = averagePositionRotateContainer = new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Children = new Drawable[]
                                    {
                                        new Circle
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Width = 0.25f,
                                        },
                                        new Circle
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Width = 0.25f,
                                            Rotation = 90
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            objectRadius = OsuHitObject.OBJECT_RADIUS * LegacyRulesetExtensions.CalculateScaleFromCircleSize(beatmap.Value.Beatmap.Difficulty.CircleSize, true);

            AverageSize.BindValueChanged(size => averagePositionContainer.Size = new Vector2(size.NewValue), true);
            AverageStyle.BindValueChanged(style => averagePositionRotateContainer.Rotation = style.NewValue == HitStyle.Plus ? 0 : 45, true);

            HitPositionStyle.BindValueChanged(s =>
            {
                foreach (var hit in hitPositionsContainer)
                {
                    hit.FadeOut(300).Expire();
                    averagePositionContainer.MoveTo(averagePosition = Vector2.Zero, 800, Easing.OutQuint);
                }

                if (s.NewValue == PositionStyle.Relative)
                {
                    arrowBackgroundContainer.FadeIn(100);
                    rotateFixedContainer.Remove(mainContainer, false);
                    AddInternal(mainContainer);
                }
                else
                {
                    arrowBackgroundContainer.FadeOut(100);
                    RemoveInternal(mainContainer, false);
                    rotateFixedContainer.Add(mainContainer);
                }
            }, true);
        }

        protected override void OnNewJudgement(JudgementResult judgement)
        {
            if (judgement is not OsuHitCircleJudgementResult circleJudgement) return;

            if (circleJudgement.CursorPositionAtHit == null) return;

            if (hitPositionsContainer.Count > max_concurrent_judgements)
            {
                const double quick_fade_time = 300;

                // check with a bit of lenience to avoid precision error in comparison.
                var old = hitPositionsContainer.FirstOrDefault(j => j.LifetimeEnd > Clock.CurrentTime + quick_fade_time * 1.1);

                if (old != null)
                {
                    old.ClearTransforms();
                    old.FadeOut(quick_fade_time).Expire();
                }
            }

            Vector2 hitPosition;

            if (HitPositionStyle.Value == PositionStyle.Relative && scoreProcessor.HitEvents.LastOrDefault().LastHitObject != null)
            {
                var currentHitEvent = scoreProcessor.HitEvents.Last();

                hitPosition = AccuracyHeatmap.FindRelativeHitPosition(((OsuHitObject)currentHitEvent.LastHitObject).StackedEndPosition, ((OsuHitObject)currentHitEvent.HitObject).StackedEndPosition,
                    circleJudgement.CursorPositionAtHit.Value, objectRadius, new Vector2(0.5f), inner_portion, 45) - new Vector2(0.5f);
            }
            else
            {
                hitPosition = roundPosition((circleJudgement.CursorPositionAtHit.Value - ((OsuHitObject)circleJudgement.HitObject).StackedPosition) / objectRadius / 2 * inner_portion);
            }

            hitPositionPool.Get(drawableHit =>
            {
                drawableHit.X = hitPosition.X;
                drawableHit.Y = hitPosition.Y;
                drawableHit.Colour = getColourForPosition(hitPosition);

                hitPositionsContainer.Add(drawableHit);
            });

            averagePositionContainer.MoveTo(averagePosition = (hitPosition + averagePosition) / 2, 800, Easing.OutQuint);
        }

        private static Vector2 roundPosition(Vector2 position)
        {
            if (position.X > 0.5f)
            {
                position.X = 0.5f;
            }
            else if (position.X < -0.5f)
            {
                position.X = -0.5f;
            }

            if (position.Y > 0.5f)
            {
                position.Y = 0.5f;
            }
            else if (position.Y < -0.5f)
            {
                position.Y = -0.5f;
            }

            return position;
        }

        private Color4 getColourForPosition(Vector2 position)
        {
            switch (Vector2.Distance(position, Vector2.Zero))
            {
                case >= 0.5f * inner_portion:
                    return colours.Red;

                case >= 0.35f * inner_portion:
                    return colours.Yellow;

                case >= 0.2f * inner_portion:
                    return colours.Green;

                default:
                    return colours.Blue;
            }
        }

        public override void Clear()
        {
            averagePositionContainer.MoveTo(averagePosition = Vector2.Zero, 800, Easing.OutQuint);

            foreach (var h in hitPositionsContainer)
            {
                h.ClearTransforms();
                h.Expire();
            }
        }

        private partial class HitPosition : PoolableDrawable
        {
            [Resolved]
            private AimErrorMeter aimErrorMeter { get; set; } = null!;

            public readonly BindableNumber<float> JudgementSize = new BindableFloat();

            public readonly Bindable<HitStyle> JudgementStyle = new Bindable<HitStyle>();

            private readonly Container content;

            public HitPosition()
            {
                RelativePositionAxes = Axes.Both;

                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;

                InternalChild = new UprightAspectMaintainingContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Child = content = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Children = new Drawable[]
                        {
                            new Circle
                            {
                                RelativeSizeAxes = Axes.Both,
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Width = 0.25f,
                                Rotation = -45
                            },
                            new Circle
                            {
                                RelativeSizeAxes = Axes.Both,
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Width = 0.25f,
                                Rotation = 45
                            }
                        }
                    }
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                JudgementSize.BindTo(aimErrorMeter.JudgementSize);
                JudgementSize.BindValueChanged(size => Size = new Vector2(size.NewValue), true);
                JudgementStyle.BindTo(aimErrorMeter.JudgementStyle);
                JudgementStyle.BindValueChanged(style => content.Rotation = style.NewValue == HitStyle.X ? 0 : 45, true);
            }

            protected override void PrepareForUse()
            {
                base.PrepareForUse();

                const int judgement_fade_in_duration = 100;
                const int judgement_fade_out_duration = 5000;

                this
                    .ResizeTo(new Vector2(0))
                    .FadeInFromZero(judgement_fade_in_duration, Easing.OutQuint)
                    .ResizeTo(new Vector2(JudgementSize.Value), judgement_fade_in_duration, Easing.OutQuint)
                    .Then()
                    .FadeOut(judgement_fade_out_duration)
                    .Expire();
            }
        }

        public enum HitStyle
        {
            X,
            Plus,
        }

        public enum PositionStyle
        {
            Absolute,
            Relative,
        }
    }
}
