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

        protected override IHasText CreateText() => new SignNumberContainer();

        public partial class SignNumberContainer : CompositeDrawable, IHasText
        {
            private readonly OsuSpriteText text;

            public LocalisableString Text
            {
                get => text.Text;
                set
                {
                    if (!int.TryParse(value.ToString().Replace(",", ""), out int result))
                    {
                        text.Text = "+0";
                    }

                    char sign = Math.Sign(result) == -1 ? '-' : '+';

                    text.Text = $"{sign}{Math.Abs(result)}";
                }
            }

            public SignNumberContainer()
            {
                RelativeSizeAxes = Axes.Both;

                InternalChild = text = new OsuSpriteText
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Font = OsuFont.Torus.With(size: 20),
                };
            }
        }
    }
}
