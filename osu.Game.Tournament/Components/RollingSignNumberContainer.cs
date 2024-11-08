// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;

namespace osu.Game.Tournament.Components
{
    public partial class RollingSignNumberContainer : CommaSeparatedScoreCounter
    {
        protected override double RollingDuration => 500;

        protected override IHasText CreateText() => new SignNumberContainer(CreateSpriteText);

        protected override OsuSpriteText CreateSpriteText() => new OsuSpriteText
        {
            Font = OsuFont.Torus.With(size: 20),
        };

        protected override LocalisableString FormatCount(double count) => count.ToString("N1");

        public partial class SignNumberContainer : CompositeDrawable, IHasText
        {
            private readonly OsuSpriteText text;

            public LocalisableString Text
            {
                get => text.Text;
                set
                {
                    if (!double.TryParse(value.ToString().Replace(",", ""), out double result))
                    {
                        text.Text = "+0";
                    }

                    char sign = Math.Sign(result) == -1 ? '-' : '+';

                    text.Text = $"{sign}{Math.Abs(result)}";
                }
            }

            public SignNumberContainer(Func<OsuSpriteText> createSpriteText)
            {
                AutoSizeAxes = Axes.Both;

                InternalChild = text = createSpriteText().With(t =>
                {
                    t.Anchor = Anchor.Centre;
                    t.Origin = Anchor.Centre;
                });
            }
        }
    }
}
