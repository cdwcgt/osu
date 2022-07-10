// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.ComponentModel;

namespace osu.Game.Configuration
{
    public enum BackgroundSource
    {
        [Description("皮肤")]
        Skin,

        [Description("谱面")]
        Beatmap,

        [Description("谱面 (带故事版)")]
        BeatmapWithStoryboard,

        [Description("加载页纯色背景")]
        LoaderBackground
    }
}
