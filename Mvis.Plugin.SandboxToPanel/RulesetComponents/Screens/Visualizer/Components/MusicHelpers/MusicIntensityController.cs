﻿using osu.Framework.Bindables;
using osu.Framework.Extensions.IEnumerableExtensions;

namespace Mvis.Plugin.SandboxToPanel.RulesetComponents.Screens.Visualizer.Components.MusicHelpers
{
    public partial class MusicIntensityController : MusicAmplitudesProvider
    {
        public readonly BindableFloat Intensity = new BindableFloat();

        protected override void OnAmplitudesUpdate(float[] amplitudes)
        {
            float sum = 0;
            amplitudes.ForEach(amp => sum += amp);

            if (IsKiai.Value)
                sum *= 1.2f;

            Intensity.Value = sum;
        }
    }
}
