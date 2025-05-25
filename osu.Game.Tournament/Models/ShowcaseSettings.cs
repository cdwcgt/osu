// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osuTK.Graphics;

namespace osu.Game.Tournament.Models
{
    public class ShowcaseSettings
    {
        public BindableFloat FlashIntensity = new BindableFloat(2)
        {
            MinValue = 0,
            MaxValue = 8,
            Value = 2,
        };

        public BindableBool FlashKiaiOnly = new BindableBool(true);
        public Bindable<Color4> FlashColor = new Bindable<Color4>(Color4.White);
        public BindableBool UseLazer = new BindableBool();
    }
}
