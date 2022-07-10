﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Bindables;
using osu.Framework.Graphics.Sprites;
using osu.Game.Configuration;

namespace osu.Game.Rulesets.Osu.Mods
{
    public class OsuModDeflate : OsuModObjectScaleTween
    {
        public override string Name => "收缩";

        public override string Acronym => "DF";

        public override IconUsage? Icon => FontAwesome.Solid.CompressArrowsAlt;

        public override string Description => "在正确的大小击打物件!";

        [SettingSource("Starting Size", "The initial size multiplier applied to all objects.")]
        public override BindableNumber<float> StartScale { get; } = new BindableFloat
        {
            MinValue = 1f,
            MaxValue = 25f,
            Default = 2f,
            Value = 2f,
            Precision = 0.1f,
        };
    }
}
