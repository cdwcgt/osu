// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Bindables;

namespace osu.Game.Screens.MapGuess
{
    public class MapGuessConfig
    {
        public Bindable<AutoSkipMode> AutoSkipMode { get; set; } = new Bindable<AutoSkipMode>();

        public BindableBool Music { get; private set; } = new BindableBool(true);
        public BindableBool ShowBackground { get; private set; } = new BindableBool(true);
        public BindableBool ShowHitobjects { get; private set; } = new BindableBool();
        public List<int> Rulesets { get; private set; } = [0];

        public BindableFloat BackgroundBlur { get; private set; } = new BindableFloat
        {
            Value = 0.1f,
            MinValue = 0,
            MaxValue = 1,
            Default = 0.1f,
            Precision = 0.05f,
        };

        public BindableInt AutoSkip { get; private set; } = new BindableInt
        {
            Value = 15,
            Default = 15,
            MinValue = 0,
            MaxValue = 60,
        };

        public BindableInt PreviewLength { get; private set; } = new BindableInt
        {
            Value = 10000,
            Default = 10000,
            MinValue = 0,
            MaxValue = 60000,
            Precision = 500,
        };
    }

    public enum AutoSkipMode
    {
        TryCount,
        Countdown,
        None
    }
}
