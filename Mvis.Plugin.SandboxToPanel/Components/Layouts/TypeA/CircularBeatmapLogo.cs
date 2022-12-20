﻿using Mvis.Plugin.Sandbox.Components.MusicHelpers;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace Mvis.Plugin.Sandbox.Components.Layouts.TypeA
{
    public partial class CircularBeatmapLogo : CurrentBeatmapProvider
    {
        private const int base_size = 350;

        public new Color4 Colour
        {
            get => progress.Colour;
            set => progress.Colour = value;
        }

        public new Bindable<int> Size = new Bindable<int>(base_size);

        private Progress progress;

        [BackgroundDependencyLoader]
        private void load()
        {
            Origin = Anchor.Centre;
            RelativePositionAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                new UpdateableBeatmapBackground
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                },
                progress = new Progress
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    InnerRadius = 0.03f,
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Size.BindValueChanged(s =>
            {
                base.Size = new Vector2(s.NewValue);
                progress.UpdateSize(s.NewValue);
            }, true);
        }

        protected override void Update()
        {
            base.Update();

            var track = Beatmap.Value?.Track;
            progress.Current.Value = (track == null || track.Length == 0) ? 0 : (track.CurrentTime / track.Length);
        }

        private partial class Progress : CircularProgress
        {
            private static readonly float sigma = 5;

            private BufferedContainer bufferedContainer => (BufferedContainer)InternalChild;

            public new Color4 Colour
            {
                get => bufferedContainer.Colour;
                set => bufferedContainer.Colour = bufferedContainer.EffectColour = value;
            }

            public Progress()
            {
                bufferedContainer.DrawOriginal = true;
            }

            public void UpdateSize(float size)
            {
                var newSigma = sigma * size / base_size;
                var padding = Blur.KernelSize(newSigma);

                bufferedContainer.BlurSigma = new Vector2(newSigma);
                bufferedContainer.Padding = new MarginPadding(padding);
            }
        }
    }
}
