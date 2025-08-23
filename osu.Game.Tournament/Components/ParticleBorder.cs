// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Components
{
    public partial class ParticleBorder : CompositeDrawable
    {
        private readonly Container particleContainer;

        public ParticleBorder()
        {
            RelativeSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(15, 15, 15, 255),
                },
                particleContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Scheduler.AddDelayed(generateParticle, 100, true);
        }

        private void generateParticle()
        {
            var rectSize = DrawSize;
            int side = RNG.Next(4);

            float startX = 0, startY = 0;
            Vector2 offset = Vector2.Zero;

            switch (side)
            {
                case 0: // 上
                    startX = (float)RNG.NextDouble() * rectSize.X;
                    startY = 0;
                    offset = new Vector2((float)(RNG.NextDouble() - 0.5) * 25, -(float)RNG.NextDouble() * 30 - 10);
                    break;

                case 1: // 右
                    startX = rectSize.X;
                    startY = (float)RNG.NextDouble() * rectSize.Y;
                    offset = new Vector2((float)RNG.NextDouble() * 30 + 10, (float)(RNG.NextDouble() - 0.5) * 20);
                    break;

                case 2: // 下
                    startX = (float)RNG.NextDouble() * rectSize.X;
                    startY = rectSize.Y;
                    offset = new Vector2((float)(RNG.NextDouble() - 0.5) * 25, (float)RNG.NextDouble() * 30 + 10);
                    break;

                case 3: // 左
                    startX = 0;
                    startY = (float)RNG.NextDouble() * rectSize.Y;
                    offset = new Vector2(-(float)RNG.NextDouble() * 30 - 10, (float)(RNG.NextDouble() - 0.5) * 20);
                    break;
            }

            float size = (float)(RNG.NextDouble() * 1.5 + 1);
            float opacity = (float)(RNG.NextDouble() * 0.5 + 0.3);
            double duration = RNG.NextDouble() * 3_000 + 4_000; // 4s-7s

            var particle = new Circle
            {
                Size = new Vector2(size),
                Colour = Color4.Gray,
                Alpha = opacity,
                Position = new Vector2(startX, startY),
            };

            particleContainer.Add(particle);

            particle.MoveToOffset(offset, duration, Easing.OutQuad)
                    .FadeOut(duration)
                    .Expire();
        }
    }
}
