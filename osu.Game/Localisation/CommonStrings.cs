// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class CommonStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.Common";

        /// <summary>
        /// "Back"
        /// </summary>
        public static LocalisableString Back => new TranslatableString(getKey(@"back"), @"Back");

        /// <summary>
        /// "Next"
        /// </summary>
        public static LocalisableString Next => new TranslatableString(getKey(@"next"), @"Next");

        /// <summary>
        /// "Finish"
        /// </summary>
        public static LocalisableString Finish => new TranslatableString(getKey(@"finish"), @"Finish");

        /// <summary>
        /// "Enabled"
        /// </summary>
        public static LocalisableString Enabled => new TranslatableString(getKey(@"enabled"), @"启用");

        /// <summary>
        /// "Disabled"
        /// </summary>
        public static LocalisableString Disabled => new TranslatableString(getKey(@"disabled"), @"Disabled");

        /// <summary>
        /// "Default"
        /// </summary>
        public static LocalisableString Default => new TranslatableString(getKey(@"default"), @"默认");

        /// <summary>
        /// "Width"
        /// </summary>
        public static LocalisableString Width => new TranslatableString(getKey(@"width"), @"宽度");

        /// <summary>
        /// "Height"
        /// </summary>
        public static LocalisableString Height => new TranslatableString(getKey(@"height"), @"高度");

        /// <summary>
        /// "Downloading..."
        /// </summary>
        public static LocalisableString Downloading => new TranslatableString(getKey(@"downloading"), @"下载中...");

        /// <summary>
        /// "Importing..."
        /// </summary>
        public static LocalisableString Importing => new TranslatableString(getKey(@"importing"), @"导入中...");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
