// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Game.Overlays.Dialog;
using osu.Game.Scoring;
using System.Diagnostics;
using osu.Framework.Graphics.Sprites;
using osu.Game.Beatmaps;

namespace osu.Game.Screens.Select
{
    public class LocalScoreDeleteDialog : PopupDialog
    {
        private readonly ScoreInfo score;

        [Resolved]
        private ScoreManager scoreManager { get; set; }

        [Resolved]
        private BeatmapManager beatmapManager { get; set; }

        public LocalScoreDeleteDialog(ScoreInfo score)
        {
            this.score = score;
            Debug.Assert(score != null);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            BeatmapInfo beatmapInfo = beatmapManager.QueryBeatmap(b => b.ID == score.BeatmapInfoID);
            Debug.Assert(beatmapInfo != null);

            BodyText = $"{score.User} ({score.DisplayAccuracy}, {score.Rank})";

            Icon = FontAwesome.Regular.TrashAlt;
            HeaderText = "请确认是否删除这个成绩?";
            Buttons = new PopupDialogButton[]
            {
                new PopupDialogDangerousButton
                {
                    Text = "是的",
                    Action = () => scoreManager?.Delete(score)
                },
                new PopupDialogCancelButton
                {
                    Text = "我需要再想想",
                },
            };
        }
    }
}
