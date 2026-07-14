// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Textures;
using osu.Game.Beatmaps;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace osu.Game.Graphics.Backgrounds
{
    public partial class BeatmapBackground : Background
    {
        public readonly WorkingBeatmap? Beatmap;

        private readonly string fallbackTextureName;

        public Bindable<double> PerceivedBrightness { get; } = new Bindable<double>(1);

        public BeatmapBackground(WorkingBeatmap? beatmap, string fallbackTextureName = @"Backgrounds/bg1")
        {
            Beatmap = beatmap;
            this.fallbackTextureName = fallbackTextureName;
        }

        [BackgroundDependencyLoader]
        private void load(LargeTextureStore textures)
        {
            Sprite.Texture = Beatmap?.GetBackground() ?? textures.Get(fallbackTextureName);
            loadBrightnessAsync();
        }

        private CancellationTokenSource? brightnessCancellation;

        private void loadBrightnessAsync()
        {
            if (Beatmap == null || string.IsNullOrEmpty(Beatmap.Metadata?.BackgroundFile))
                return;

            string? path = Beatmap.BeatmapSetInfo.GetPathForFile(Beatmap.Metadata.BackgroundFile);

            brightnessCancellation?.Cancel();
            var cancellation = brightnessCancellation = new CancellationTokenSource();

            Task.Run(() =>
            {
                using var stream = Beatmap.GetStream(path);
                if (stream == null)
                    return 1;

                using var image = Image.Load<Rgba32>(stream);

                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(64, 64),
                    Mode = ResizeMode.Max
                }));

                double[] brightnessValues = new double[4096];
                int brightnessCount = 0;

                double sum = 0;
                double weight = 0;

                image.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        Span<Rgba32> row = accessor.GetRowSpan(y);

                        foreach (Rgba32 p in row)
                        {
                            double a = p.A / 255.0;

                            if (a <= 0)
                                continue;

                            double r = p.R / 255.0;
                            double g = p.G / 255.0;
                            double b = p.B / 255.0;

                            double brightness = Math.Sqrt(
                                r * r * 0.299 +
                                g * g * 0.587 +
                                b * b * 0.114);

                            sum += brightness * a;
                            weight += a;

                            brightnessValues[brightnessCount++] = brightness;
                        }
                    }
                });

                if (weight <= 0)
                    return 1;

                double mean = sum / weight;

                Array.Sort(brightnessValues, 0, brightnessCount);

                double p90 =
                    brightnessValues[(int)(brightnessCount * 0.9)];

                double p99 =
                    brightnessValues[Math.Min(
                        brightnessCount - 1,
                        (int)(brightnessCount * 0.99))];

                return
                    mean * 0.3 +
                    p90 * 0.5 +
                    p99 * 0.2;
            }, cancellation.Token).ContinueWith(t =>
            {
                if (!t.IsCompletedSuccessfully)
                    return;

                double result = t.GetResultSafely();

                Schedule(() => PerceivedBrightness.Value = result);
            }, cancellation.Token);
        }

        public override bool Equals(Background? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return other.GetType() == GetType()
                   && ((BeatmapBackground)other).Beatmap == Beatmap;
        }
    }
}
