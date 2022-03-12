// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class GraphicsSettingsStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.GraphicsSettings";

        /// <summary>
        /// "Graphics"
        /// </summary>
        public static LocalisableString GraphicsSectionHeader => new TranslatableString(getKey(@"graphics_section_header"), @"Graphics");

        /// <summary>
        /// "Renderer"
        /// </summary>
        public static LocalisableString RendererHeader => new TranslatableString(getKey(@"renderer_header"), @"Renderer");

        /// <summary>
        /// "Frame limiter"
        /// </summary>
        public static LocalisableString FrameLimiter => new TranslatableString(getKey(@"frame_limiter"), @"Frame limiter");

        /// <summary>
        /// "Threading mode"
        /// </summary>
        public static LocalisableString ThreadingMode => new TranslatableString(getKey(@"threading_mode"), @"Threading mode");

        /// <summary>
        /// "Show FPS"
        /// </summary>
        public static LocalisableString ShowFPS => new TranslatableString(getKey(@"show_fps"), @"显示帧率");

        /// <summary>
        /// "Using unlimited frame limiter can lead to stutters, bad performance and overheating. It will not improve perceived latency. "2x refresh rate" is recommended."
        /// </summary>
        public static LocalisableString UnlimitedFramesNote => new TranslatableString(getKey(@"unlimited_frames_note"), @"启用无限制帧数选项可能导致游戏卡顿、性能下降和电脑过热。这并不会降低体感延迟。推荐使用“2倍刷新率”选项。");

        /// <summary>
        /// "Layout"
        /// </summary>
        public static LocalisableString LayoutHeader => new TranslatableString(getKey(@"layout_header"), @"Layout");

        /// <summary>
        /// "Screen mode"
        /// </summary>
        public static LocalisableString ScreenMode => new TranslatableString(getKey(@"screen_mode"), @"Screen mode");

        /// <summary>
        /// "Resolution"
        /// </summary>
        public static LocalisableString Resolution => new TranslatableString(getKey(@"resolution"), @"Resolution");

        /// <summary>
        /// "Display"
        /// </summary>
        public static LocalisableString Display => new TranslatableString(getKey(@"display"), @"Display");

        /// <summary>
        /// "UI scaling"
        /// </summary>
        public static LocalisableString UIScaling => new TranslatableString(getKey(@"ui_scaling"), @"UI scaling");

        /// <summary>
        /// "Screen scaling"
        /// </summary>
        public static LocalisableString ScreenScaling => new TranslatableString(getKey(@"screen_scaling"), @"Screen scaling");

        /// <summary>
        /// "Horizontal position"
        /// </summary>
        public static LocalisableString HorizontalPosition => new TranslatableString(getKey(@"horizontal_position"), @"Horizontal position");

        /// <summary>
        /// "Vertical position"
        /// </summary>
        public static LocalisableString VerticalPosition => new TranslatableString(getKey(@"vertical_position"), @"Vertical position");

        /// <summary>
        /// "Horizontal scale"
        /// </summary>
        public static LocalisableString HorizontalScale => new TranslatableString(getKey(@"horizontal_scale"), @"Horizontal scale");

        /// <summary>
        /// "Vertical scale"
        /// </summary>
        public static LocalisableString VerticalScale => new TranslatableString(getKey(@"vertical_scale"), @"Vertical scale");

        /// <summary>
        /// "Running without fullscreen mode will increase your input latency!"
        /// </summary>
        public static LocalisableString NotFullscreenNote => new TranslatableString(getKey(@"not_fullscreen_note"), @"不启用全屏模式会增加你的输入延迟！");

        /// <summary>
        /// "Detail Settings"
        /// </summary>
        public static LocalisableString DetailSettingsHeader => new TranslatableString(getKey(@"detail_settings_header"), @"Detail Settings");

        /// <summary>
        /// "Storyboard / video"
        /// </summary>
        public static LocalisableString StoryboardVideo => new TranslatableString(getKey(@"storyboard_video"), @"故事板 / 视频");

        /// <summary>
        /// "Hit lighting"
        /// </summary>
        public static LocalisableString HitLighting => new TranslatableString(getKey(@"hit_lighting"), @"击打闪光");

        /// <summary>
        /// "Screenshots"
        /// </summary>
        public static LocalisableString Screenshots => new TranslatableString(getKey(@"screenshots"), @"Screenshots");

        /// <summary>
        /// "Screenshot format"
        /// </summary>
        public static LocalisableString ScreenshotFormat => new TranslatableString(getKey(@"screenshot_format"), @"Screenshot format");

        /// <summary>
        /// "Show menu cursor in screenshots"
        /// </summary>
        public static LocalisableString ShowCursorInScreenshots => new TranslatableString(getKey(@"show_cursor_in_screenshots"), @"在截图中显示光标");

        /// <summary>
        /// "Video"
        /// </summary>
        public static LocalisableString VideoHeader => new TranslatableString(getKey(@"video_header"), @"视频");

        /// <summary>
        /// "Use hardware acceleration"
        /// </summary>
        public static LocalisableString UseHardwareAcceleration => new TranslatableString(getKey(@"use_hardware_acceleration"), @"使用硬件加速");

        private static string getKey(string key) => $"{prefix}:{key}";
    }
}
