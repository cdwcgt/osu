// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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
using osu.Game.Localisation.HUD;
using osu.Game.Rulesets.Catch.Judgements;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch.UI;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Screens.Play.HUD.HitErrorMeters;
using osuTK;

namespace osu.Game.Rulesets.Catch.HUD
{
    public partial class CatchErrorMeter : HitErrorMeter
    {
        [SettingSource(typeof(AimErrorMeterStrings), nameof(AimErrorMeterStrings.PositionDisplayStyle), nameof(AimErrorMeterStrings.PositionDisplayStyleDescription))]
        public Bindable<PositionDisplay> PositionDisplayStyle { get; } = new Bindable<PositionDisplay>();

        private float catchWidth;
        private readonly Container catchErrorMarkerContainer;
        private readonly Container arrowContainer;

        private const float inner_portion = 0.7f;

        public CatchErrorMeter()
        {
            Masking = true;
            Width = 200;
            AutoSizeAxes = Axes.Y;
            AlwaysPresent = true;
            Padding = new MarginPadding { Vertical = 6 };

            const float line_thickness = 2f;

            InternalChildren = new Drawable[]
            {
                hitPositionPool,
                new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Name = "background",
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Width = inner_portion,
                    Children = new Drawable[]
                    {
                        new Circle
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            RelativeSizeAxes = Axes.X,
                            Height = line_thickness,
                        },
                        arrowContainer = new Container
                        {
                            Name = "Arrow",
                            RelativeSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                new Circle
                                {
                                    Anchor = Anchor.CentreRight,
                                    Origin = Anchor.TopCentre,
                                    Margin = new MarginPadding(-line_thickness / 2),
                                    Rotation = 45,
                                    Width = line_thickness,
                                    Height = 10f,
                                },
                                new Circle
                                {
                                    Anchor = Anchor.CentreRight,
                                    Origin = Anchor.TopCentre,
                                    Margin = new MarginPadding(-line_thickness / 2),
                                    Rotation = 135,
                                    Width = line_thickness,
                                    Height = 10f,
                                }
                            }
                        },
                    },
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Children = new Drawable[]
                    {
                        catchErrorMarkerContainer = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre
                        },
                    }
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(IBindable<WorkingBeatmap> beatmap, ScoreProcessor processor)
        {
            // handle IApplicableToDifficulty for CS change.
            BeatmapDifficulty newDifficulty = new BeatmapDifficulty();
            beatmap.Value.Beatmap.Difficulty.CopyTo(newDifficulty);

            var mods = processor.Mods.Value;

            foreach (var mod in mods.OfType<IApplicableToDifficulty>())
                mod.ApplyToDifficulty(newDifficulty);

            catchWidth = Catcher.CalculateCatchWidth(newDifficulty);

            PositionDisplayStyle.BindValueChanged(s =>
            {
                Clear();

                if (s.NewValue == PositionDisplay.Normalised)
                {
                    arrowContainer.FadeIn(100);
                }
                else
                {
                    arrowContainer.FadeOut(100);
                }
            }, true);
        }

        private readonly DrawablePool<CatchPositionMarker> hitPositionPool = new DrawablePool<CatchPositionMarker>(10);
        private float? lastObjectPosition;

        protected override void OnNewJudgement(JudgementResult judgement)
        {
            if (!judgement.Type.IsScorable() || judgement.Type.IsBonus())
                return;

            var catchResult = (CatchJudgementResult)judgement;

            float catcherPosition = catchResult.CatcherPosition;
            float objectPosition = ((CatchHitObject)catchResult.HitObject).EffectiveX;

            float offset;

            if (PositionDisplayStyle.Value == PositionDisplay.Absolute ||
                lastObjectPosition == null || lastObjectPosition < objectPosition)
            {
                offset = objectPosition - catcherPosition;
            }
            else
            {
                offset = catcherPosition - objectPosition;
            }

            offset = offset / catchWidth * inner_portion;

            offset = Math.Clamp(offset, -0.5f, 0.5f);

            hitPositionPool.Get(drawableHit =>
            {
                drawableHit.X = offset;
                drawableHit.Colour = GetColourForHitResult(judgement.Type);

                catchErrorMarkerContainer.Add(drawableHit);
            });

            lastObjectPosition = objectPosition;
        }

        public override void Clear()
        {
            lastObjectPosition = null;

            foreach (var marker in catchErrorMarkerContainer)
            {
                marker.FinishTransforms();
                marker.Expire();
            }
        }

        private partial class CatchPositionMarker : PoolableDrawable
        {
            public CatchPositionMarker()
            {
                RelativePositionAxes = Axes.Both;

                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;

                InternalChild = new Circle
                {
                    RelativeSizeAxes = Axes.Both,
                };
            }

            protected override void PrepareForUse()
            {
                base.PrepareForUse();

                const int judgement_fade_in_duration = 100;
                const int judgement_fade_out_duration = 1000;

                this
                    .ResizeTo(new Vector2(0))
                    .FadeInFromZero(judgement_fade_in_duration, Easing.OutQuint)
                    .ResizeTo(new Vector2(5), judgement_fade_in_duration, Easing.OutQuint)
                    .Then()
                    .FadeOut(judgement_fade_out_duration)
                    .Expire();
            }
        }

        public enum PositionDisplay
        {
            [LocalisableDescription(typeof(AimErrorMeterStrings), nameof(AimErrorMeterStrings.Normalised))]
            Normalised,

            [LocalisableDescription(typeof(AimErrorMeterStrings), nameof(AimErrorMeterStrings.Absolute))]
            Absolute,
        }
    }
}
