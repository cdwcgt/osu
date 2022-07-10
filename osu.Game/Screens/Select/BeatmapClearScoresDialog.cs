﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using osu.Game.Beatmaps;
using osu.Game.Overlays.Dialog;
using osu.Game.Scoring;

namespace osu.Game.Screens.Select
{
    public class BeatmapClearScoresDialog : PopupDialog
    {
        [Resolved]
        private ScoreManager scoreManager { get; set; }

        public BeatmapClearScoresDialog(BeatmapInfo beatmapInfo, Action onCompletion)
        {
            BodyText = beatmapInfo.GetDisplayTitle();
            Icon = FontAwesome.Solid.Eraser;
            HeaderText = @"是否要清理所有本地成绩?";
            Buttons = new PopupDialogButton[]
            {
                new PopupDialogOkButton
                {
                    Text = @"是的,抹除这些黑历史",
                    Action = () =>
                    {
                        Task.Run(() => scoreManager.Delete(beatmapInfo))
                            .ContinueWith(_ => onCompletion);
                    }
                },
                new PopupDialogCancelButton
                {
                    Text = @"我需要再想想",
                },
            };
        }
    }
}
