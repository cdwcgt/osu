// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class FirstRunOverlayImportFromStableScreenStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.ScreenImportFromStable";

        /// <summary>
        /// "导入"
        /// </summary>
        public static LocalisableString Header => new TranslatableString(getKey(@"llin_header"), @"导入");

        /// <summary>
        /// "如果你之前装有stable，你可以选择在这里导入所有内容。 这将会复制一遍所有内容，并不影响现有的stable文件。"
        /// </summary>
        public static LocalisableString Description => new TranslatableString(getKey(@"llin_description"),
            @"如果你之前装有stable，你可以选择在这里导入所有内容。 这将会复制一遍所有内容，并不影响现有的stable文件。");

        /// <summary>
        /// "stable安装地址"
        /// </summary>
        public static LocalisableString LocateDirectoryLabel => new TranslatableString(getKey(@"llin_locate_directory_label"), @"stable安装地址");

        /// <summary>
        /// "点击定位stable安装地址"
        /// </summary>
        public static LocalisableString LocateDirectoryPlaceholder => new TranslatableString(getKey(@"llin_locate_directory_placeholder"), @"点击定位stable安装地址");

        /// <summary>
        /// "从stable导入内容"
        /// </summary>
        public static LocalisableString ImportButton => new TranslatableString(getKey(@"llin_import_button"), @"从stable导入内容");

        /// <summary>
        /// "导入将在后台继续进行, 打开通知可以随时查看进度！"
        /// </summary>
        public static LocalisableString ImportInProgress =>
            new TranslatableString(getKey(@"llin_import_in_progress"), @"导入将在后台继续进行, 打开通知可以随时查看进度！");

        /// <summary>
        /// "正在计算..."
        /// </summary>
        public static LocalisableString Calculating => new TranslatableString(getKey(@"llin_calculating"), @"正在计算...");

        /// <summary>
        /// "{0} 个物品"
        /// </summary>
        public static LocalisableString Items(int arg0) => new TranslatableString(getKey(@"llin_items"), @"{0} 个物品", arg0);

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
