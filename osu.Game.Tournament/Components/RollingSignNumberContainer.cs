// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;

namespace osu.Game.Tournament.Components
{
    public partial class RollingSignNumberContainer : RollingCounter<double>
    {
        protected override double RollingDuration => 500;

        protected override Easing RollingEasing => Easing.Out;

        protected override double GetProportionalDuration(double currentValue, double newValue) =>
            currentValue > newValue ? currentValue - newValue : newValue - currentValue;

        protected override OsuSpriteText CreateSpriteText() => new OsuSpriteText
        {
            Font = OsuFont.Torus.With(size: 20),
        };

        protected override LocalisableString FormatCount(double count)
        {
            char sign = Math.Sign(count) == -1 ? '-' : '+';

            return $"{sign}{Math.Abs(count):N2}";
        }
    }
}
