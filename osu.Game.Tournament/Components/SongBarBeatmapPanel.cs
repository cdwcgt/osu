// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Game.Beatmaps;

namespace osu.Game.Tournament.Components
{
    public partial class SongBarBeatmapPanel : TournamentBeatmapPanel
    {
        protected override bool TranslucentProtectedAfterPick => false;

        public SongBarBeatmapPanel(IBeatmapInfo? beatmap)
            : base(beatmap)
        {
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            ModIconContainer.With(d =>
            {
                d.Anchor = Anchor.CentreRight;
                d.Origin = Anchor.CentreRight;
                d.Margin = new MarginPadding { Right = 20 };
            });

            PadLockContainer.With(d =>
            {
                d.Anchor = Anchor.CentreRight;
                d.Origin = Anchor.CentreRight;
                d.Margin = new MarginPadding { Right = 100 };
            });
        }
    }
}
