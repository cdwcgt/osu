// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Scoring;
using osu.Game.Screens.Select;

namespace osu.Game.Screens.ReplayVs.Select
{
    public partial class ReplayVsSongSelect : SongSelect
    {
        private readonly ReplayVsSelectScreen.TeamContainer teamContainer;

        public ReplayVsSongSelect(ReplayVsSelectScreen.TeamContainer teamContainer)
        {
            this.teamContainer = teamContainer;
        }

        protected override BeatmapDetailArea CreateBeatmapDetailArea()
        {
            return new ReplayVsBeatmapDetailArea
            {
                Leaderboard =
                {
                    ScoreSelected = scoreSelected
                }
            };
        }

        private void scoreSelected(ScoreInfo s)
        {
            teamContainer.AddScore(s);
        }

        protected override bool OnStart()
        {
            return false;
        }
    }
}
