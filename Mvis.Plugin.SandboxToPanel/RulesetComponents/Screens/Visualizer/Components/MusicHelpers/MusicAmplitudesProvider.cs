﻿using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Game.Beatmaps;

#nullable disable

namespace Mvis.Plugin.SandboxToPanel.RulesetComponents.Screens.Visualizer.Components.MusicHelpers
{
    public abstract partial class MusicAmplitudesProvider : CurrentBeatmapProvider
    {
        public readonly BindableBool IsKiai = new BindableBool();

        private Track track;

        protected override void OnBeatmapChanged(ValueChangedEvent<WorkingBeatmap> beatmap)
        {
            base.OnBeatmapChanged(beatmap);
            track = beatmap.NewValue?.Track;
        }

        protected override void Update()
        {
            base.Update();

            OnAmplitudesUpdate(track?.CurrentAmplitudes.FrequencyAmplitudes.Span.ToArray() ?? new float[256]);
            IsKiai.Value = Beatmap.Value?.Beatmap.ControlPointInfo.EffectPointAt(track?.CurrentTime ?? 0).KiaiMode ?? false;
        }

        protected abstract void OnAmplitudesUpdate(float[] amplitudes);
    }
}
