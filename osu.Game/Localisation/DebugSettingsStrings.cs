// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class DebugSettingsStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.DebugSettings";

        /// <summary>
        /// "Debug"
        /// </summary>
        public static LocalisableString DebugSectionHeader => new TranslatableString(getKey(@"debug_section_header"), @"Debug");

        /// <summary>
        /// "常规"
        /// </summary>
        public static LocalisableString GeneralHeader => new TranslatableString(getKey(@"general_header"), @"常规");

        /// <summary>
        /// "显示日志叠加层"
        /// </summary>
        public static LocalisableString ShowLogOverlay => new TranslatableString(getKey(@"show_log_overlay"), @"显示日志叠加层");

        /// <summary>
        /// "总是渲染被遮挡的窗口"
        /// </summary>
        public static LocalisableString BypassFrontToBackPass => new TranslatableString(getKey(@"bypass_front_to_back_pass"), @"总是渲染被遮挡的窗口");

        /// <summary>
        /// "导入文件"
        /// </summary>
        public static LocalisableString ImportFiles => new TranslatableString(getKey(@"import_files"), @"导入文件");

        /// <summary>
        /// "Memory"
        /// </summary>
        public static LocalisableString MemoryHeader => new TranslatableString(getKey(@"memory_header"), @"Memory");

        /// <summary>
        /// "Clear all caches"
        /// </summary>
        public static LocalisableString ClearAllCaches => new TranslatableString(getKey(@"clear_all_caches"), @"Clear all caches");

        /// <summary>
        /// "压缩realm存储"
        /// </summary>
        public static LocalisableString CompactRealm => new TranslatableString(getKey(@"compact_realm"), @"压缩realm存储");

        private static string getKey(string key) => $"{prefix}:{key}";
    }
}
