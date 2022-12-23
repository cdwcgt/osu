﻿using System;
using Mvis.Plugin.SandboxToPanel.RulesetComponents.Configuration;
using Mvis.Plugin.SandboxToPanel.RulesetComponents.Screens.Visualizer.Components.MusicHelpers;
using Mvis.Plugin.SandboxToPanel.RulesetComponents.Screens.Visualizer.Components.Visualizers;
using Mvis.Plugin.SandboxToPanel.RulesetComponents.Screens.Visualizer.Components.Visualizers.Circular;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osuTK;

#nullable disable

namespace Mvis.Plugin.SandboxToPanel.RulesetComponents.Screens.Visualizer.Components.Layouts.TypeA
{
    public partial class TypeAVisualizerController : MusicAmplitudesProvider
    {
        [Resolved(canBeNull: true)]
        private SandboxRulesetConfigManager config { get; set; }

        private readonly Bindable<int> visuals = new Bindable<int>(3);
        private readonly Bindable<double> barWidth = new Bindable<double>(1.0);
        private readonly Bindable<int> totalBarCount = new Bindable<int>(3500);
        private readonly Bindable<int> rotation = new Bindable<int>(0);
        private readonly Bindable<int> decay = new Bindable<int>(200);
        private readonly Bindable<int> multiplier = new Bindable<int>(400);
        private readonly Bindable<CircularBarType> type = new Bindable<CircularBarType>(CircularBarType.Basic);
        private readonly Bindable<bool> symmetry = new Bindable<bool>(true);
        private readonly Bindable<int> smoothness = new Bindable<int>();

        [BackgroundDependencyLoader]
        private void load()
        {
            Origin = Anchor.Centre;
            Size = new Vector2(348);
            RelativePositionAxes = Axes.Both;

            config?.BindWith(SandboxRulesetSetting.VisualizerAmount, visuals);
            config?.BindWith(SandboxRulesetSetting.BarWidthA, barWidth);
            config?.BindWith(SandboxRulesetSetting.BarsPerVisual, totalBarCount);
            config?.BindWith(SandboxRulesetSetting.Rotation, rotation);
            config?.BindWith(SandboxRulesetSetting.CircularBarType, type);
            config?.BindWith(SandboxRulesetSetting.DecayA, decay);
            config?.BindWith(SandboxRulesetSetting.MultiplierA, multiplier);
            config?.BindWith(SandboxRulesetSetting.Symmetry, symmetry);
            config?.BindWith(SandboxRulesetSetting.SmoothnessA, smoothness);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            rotation.BindValueChanged(e => Rotation = e.NewValue + (symmetry.Value ? 180f / visuals.Value : 0));
            totalBarCount.BindValueChanged(_ => updateBarCount());
            visuals.BindValueChanged(_ => updateVisuals());
            symmetry.BindValueChanged(_ => updateVisuals());
            type.BindValueChanged(_ => updateVisuals(), true);
        }

        private void updateVisuals()
        {
            Clear();

            var degree = 360f / trueVisualsCount;

            for (int i = 0; i < trueVisualsCount; i++)
            {
                Add(createVisualizer().With(v =>
                {
                    v.Anchor = Anchor.Centre;
                    v.Origin = Anchor.Centre;
                    v.RelativeSizeAxes = Axes.Both;
                    v.Rotation = i * degree;
                    v.DegreeValue.Value = degree;
                    v.BarWidth.BindTo(barWidth);
                    v.Decay.BindTo(decay);
                    v.HeightMultiplier.BindTo(multiplier);
                    v.Smoothness.BindTo(smoothness);

                    if (symmetry.Value)
                        v.Reversed.Value = i % 2 == 0;
                }));
            }

            updateBarCount();
            rotation.TriggerChange();
        }

        private CircularMusicVisualizerDrawable createVisualizer()
        {
            switch (type.Value)
            {
                default:
                case CircularBarType.Basic:
                    return new BasicMusicVisualizerDrawable();

                case CircularBarType.Rounded:
                    return new RoundedMusicVisualizerDrawable();

                case CircularBarType.Fall:
                    return new FallMusicVisualizerDrawable();

                case CircularBarType.Dots:
                    return new DotsMusicVisualizerDrawable();
            }
        }

        private void updateBarCount()
        {
            var barsPerVis = (int)Math.Round((float)totalBarCount.Value / trueVisualsCount);

            foreach (var c in Children)
                ((MusicVisualizerDrawable)c).BarCount.Value = barsPerVis;
        }

        protected override void OnAmplitudesUpdate(float[] amplitudes)
        {
            foreach (var c in Children)
                ((MusicVisualizerDrawable)c).SetAmplitudes(amplitudes);
        }

        private int trueVisualsCount => visuals.Value * (symmetry.Value ? 2 : 1);
    }
}
