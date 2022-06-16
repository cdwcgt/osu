// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class AudioSettingsStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.AudioSettings";

        /// <summary>
        /// "音频"
        /// </summary>
        public static LocalisableString AudioSectionHeader => new TranslatableString(getKey(@"audio_section_header"), @"音频");

        /// <summary>
        /// "输出设备"
        /// </summary>
        public static LocalisableString AudioDevicesHeader => new TranslatableString(getKey(@"audio_devices_header"), @"输出设备");

        /// <summary>
        /// "音量"
        /// </summary>
        public static LocalisableString VolumeHeader => new TranslatableString(getKey(@"volume_header"), @"音量");

        /// <summary>
        /// "Output device"
        /// </summary>
        public static LocalisableString OutputDevice => new TranslatableString(getKey(@"output_device"), @"Output device");

        /// <summary>
        /// "Hitsound stereo separation"
        /// </summary>
        public static LocalisableString PositionalLevel => new TranslatableString(getKey(@"positional_hitsound_audio_level"), @"Hitsound stereo separation");

        /// <summary>
        /// "主音量"
        /// </summary>
        public static LocalisableString MasterVolume => new TranslatableString(getKey(@"master_volume"), @"主音量");

        /// <summary>
        /// "主音量（窗口位于后台时）"
        /// </summary>
        public static LocalisableString MasterVolumeInactive => new TranslatableString(getKey(@"master_volume_inactive"), @"主音量（窗口位于后台时）");

        /// <summary>
        /// "音效"
        /// </summary>
        public static LocalisableString EffectVolume => new TranslatableString(getKey(@"effect_volume"), @"音效");

        /// <summary>
        /// "音乐"
        /// </summary>
        public static LocalisableString MusicVolume => new TranslatableString(getKey(@"music_volume"), @"音乐");

        /// <summary>
        /// "偏移调整"
        /// </summary>
        public static LocalisableString OffsetHeader => new TranslatableString(getKey(@"offset_header"), @"偏移调整");

        /// <summary>
        /// "音频偏移"
        /// </summary>
        public static LocalisableString AudioOffset => new TranslatableString(getKey(@"audio_offset"), @"音频偏移");

        /// <summary>
        /// "偏移设置向导"
        /// </summary>
        public static LocalisableString OffsetWizard => new TranslatableString(getKey(@"offset_wizard"), @"偏移设置向导");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
