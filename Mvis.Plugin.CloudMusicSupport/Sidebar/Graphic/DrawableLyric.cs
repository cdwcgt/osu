using System;
using Mvis.Plugin.CloudMusicSupport.Misc;
using osu.Framework.Graphics.Pooling;

namespace Mvis.Plugin.CloudMusicSupport.Sidebar.Graphic
{
    public abstract partial class DrawableLyric : PoolableDrawable, IComparable<DrawableLyric>
    {
        public Lyric Value
        {
            get => value;
            set
            {
                if (IsLoaded)
                    UpdateValue(value);

                this.value = value;
            }
        }

        private Lyric value = null!;

        public float CurrentY;
        public abstract int FinalHeight();

        protected override void LoadComplete()
        {
            UpdateValue(value);
            base.LoadComplete();
        }

        protected abstract void UpdateValue(Lyric lyric);

        public int CompareTo(DrawableLyric? other) => CurrentY.CompareTo(other?.CurrentY ?? 0);
    }
}
