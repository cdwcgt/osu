// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osu.Game.Beatmaps;

namespace osu.Game.Overlays.Toolbar
{
    public class ToolbarTimeButton : ToolbarButton
    {
        private readonly TimeSpan launchTick = new TimeSpan(DateTime.Now.Ticks);

        [Resolved]
        private BeatmapManager beatmapManager { get; set; }

        public ToolbarTimeButton()
        {
            AutoSizeAxes = Axes.X;
        }

        private int beatmapCountReal;

        private int beatmapCount
        {
            get => beatmapCountReal;
            set
            {
                beatmapCountReal = value;
                Schedule(updateBeatmapTooltip);
                Logger.Log($"更新！ -> {value}");
            }
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            beatmapCount = beatmapManager.GetAllUsableBeatmapSets().Count;
            beatmapManager.OnBeatmapAdded += c => beatmapCount += c;
            beatmapManager.OnBeatmapHide += c => beatmapCount -= c;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            updateTime();
            updateBeatmapTooltip();
        }

        private void updateTime()
        {
            var currentTime = DateTime.Now;
            DrawableText.Text = currentTime.ToString(CultureInfo.CurrentCulture);

            var currentTick = new TimeSpan(currentTime.Ticks);
            var xE = currentTick.Subtract(launchTick);
            string tooltipMainArg = "";

            if (xE.Hours > 0)
                tooltipMainArg += $"{(xE.Hours + xE.Days * 24):00}:";

            tooltipMainArg += $"{xE.Minutes:00}:{xE.Seconds:00}";

            TooltipMain = $"osu!已经运行了 {tooltipMainArg}。";

            this.Delay(500).Schedule(updateTime);
        }

        private void updateBeatmapTooltip() =>
            TooltipSub = $"你共有{beatmapCount}张谱面!";

        protected override bool OnClick(ClickEvent e)
        {
            return true;
        }
    }
}
