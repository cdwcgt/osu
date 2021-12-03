using System;
using System.Collections.Generic;
using System.Linq;
using Mvis.Plugin.CloudMusicSupport.Misc;
using Mvis.Plugin.CloudMusicSupport.Sidebar.Graphic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Pooling;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Cursor;

namespace Mvis.Plugin.CloudMusicSupport.Sidebar.Screens
{
    public abstract class LyricScreen<T> : SidebarScreen
        where T : DrawableLyric, new()
    {
        protected abstract T CreateDrawableLyric(Lyric lyric);

        [Resolved]
        private LyricPlugin plugin { get; set; }

        protected LyricPlugin Plugin => plugin;

        protected readonly OsuScrollContainer<DrawableLyric> LyricScroll;
        private readonly DrawablePool<T> lyricPool = new DrawablePool<T>(100);

        private readonly List<DrawableLyric> visibleLyrics = new List<DrawableLyric>();
        protected readonly List<DrawableLyric> AvaliableDrawableLyrics = new List<DrawableLyric>();

        private float distanceLoadUnload => 150;

        protected LyricScreen()
        {
            RelativeSizeAxes = Axes.Both;
            InternalChildren = new Drawable[]
            {
                lyricPool,
                new OsuContextMenuContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = LyricScroll = new OsuScrollContainer<DrawableLyric>
                    {
                        RelativeSizeAxes = Axes.Both,
                        ScrollContent = { AutoSizeAxes = Axes.None },
                        Padding = new MarginPadding(5)
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            plugin.CurrentStatus.ValueChanged += onPluginStatusChanged;
            UpdateStatus(plugin.CurrentStatus.Value);

            base.LoadComplete();
        }

        private readonly DrawableLyric dummyDrawableLyric = new DummyDrawableLyric();

        private float visibleTop => LyricScroll.Current;
        private float visibleBottom => LyricScroll.Current + DrawHeight;

        private (int first, int last) getRange()
        {
            dummyDrawableLyric.CurrentY = visibleTop - distanceLoadUnload;
            int first = visibleLyrics.BinarySearch(dummyDrawableLyric);
            if (first < 0) first = ~first;

            dummyDrawableLyric.CurrentY = visibleBottom + distanceLoadUnload;
            int last = visibleLyrics.BinarySearch(dummyDrawableLyric);
            if (last < 0) last = ~last;

            first = Math.Max(0, first - 1);
            last = Math.Clamp(last + 1, last - 1, Math.Max(0, visibleLyrics.Count - 1));

            return (first, last);
        }

        protected (int first, int last) CurrentRange;

        protected override void Update()
        {
            visibleLyrics.Clear();

            int currentY = 0;

            foreach (var drawableLyric in AvaliableDrawableLyrics)
            {
                drawableLyric.CurrentY = currentY;
                visibleLyrics.Add(drawableLyric);

                currentY += drawableLyric.FinalHeight();
            }

            LyricScroll.ScrollContent.Height = currentY;

            //获取显示范围
            var range = getRange();

            if (range != CurrentRange)
                updateFromRange(range);

            base.Update();
        }

        private void updateFromRange((int first, int last) range)
        {
            //赋值
            CurrentRange = range;

            //如果可用歌词>0
            if (visibleLyrics.Count > 0)
            {
                //获取要显示的歌词
                var toDisplay = visibleLyrics.GetRange(range.first, range.last - range.first + 1);

                //遍历lyricScroll的所有Child
                foreach (var drawableLyric in LyricScroll.Children)
                {
                    //如果已经在显示了，则从toDisplay里去掉
                    if (toDisplay.Remove(toDisplay.Find(d => d.Value.Equals(drawableLyric.Value)))) continue;

                    //如果面板不在显示区，则直接Expire
                    if (drawableLyric.Y + drawableLyric.DrawHeight < visibleTop - distanceLoadUnload
                        || drawableLyric.Y > visibleBottom + distanceLoadUnload)
                        drawableLyric.Expire();
                }

                //添加要显示的面板
                foreach (var item in toDisplay)
                {
                    var panel = lyricPool.Get(p => p.Value = item.Value);

                    panel.Depth = item.CurrentY;
                    panel.Y = item.CurrentY;

                    LyricScroll.Add(panel);
                }
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            plugin.CurrentStatus.ValueChanged -= onPluginStatusChanged;
            base.Dispose(isDisposing);
        }

        protected virtual void UpdateStatus(LyricPlugin.Status status)
        {
            switch (status)
            {
                case LyricPlugin.Status.Finish:
                    RefreshLrcInfo(plugin.Lyrics);
                    break;

                case LyricPlugin.Status.Failed:
                    break;

                default:
                    visibleLyrics.Clear();
                    break;
            }
        }

        private void onPluginStatusChanged(ValueChangedEvent<LyricPlugin.Status> v)
            => UpdateStatus(v.NewValue);

        protected virtual void ScrollToCurrent()
        {
            float pos = AvaliableDrawableLyrics.FirstOrDefault(p =>
                p.Value.Equals(plugin.CurrentLine))?.CurrentY ?? 0;

            if (pos + DrawHeight > LyricScroll.ScrollContent.Height)
                LyricScroll.ScrollToEnd();
            else
                LyricScroll.ScrollTo(pos);
        }

        protected virtual void RefreshLrcInfo(List<Lyric> lyrics)
        {
            LyricScroll.Clear();
            AvaliableDrawableLyrics.Clear();
            lyricPool.Clear();

            LyricScroll.ScrollToStart();

            foreach (var t in lyrics)
                AvaliableDrawableLyrics.Add(CreateDrawableLyric(t));

            //workaround: 恢复后歌词不显示
            CurrentRange.first = CurrentRange.last = 0;
        }
    }
}
