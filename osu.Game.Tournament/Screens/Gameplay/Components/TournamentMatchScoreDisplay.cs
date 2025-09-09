// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Game.Screens.Play.HUD;
using osu.Game.Tournament.IPC;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class TournamentMatchScoreDisplay : MatchScoreDisplay
    {
        private bool invertTextColor;
        private readonly Colour4 black = Colour4.FromHex("1f1f1f");

        public readonly BindableBool ShowSuccess = new BindableBool();

        [Resolved]
        private LadderInfo ladderInfo { get; set; } = null!;

        [Resolved]
        private MatchIPCInfo ipc { get; set; } = null!;

        public bool InvertTextColor
        {
            get => invertTextColor;
            set
            {
                invertTextColor = value;
                updateColor();
            }
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Team1Score.BindTo(ipc.Score1);
            Team2Score.BindTo(ipc.Score2);
        }

        private void updateColor()
        {
            if (!ShowSuccess.Value)
            {
                Score1Text.Background.FadeOut(50);
                Score2Text.Background.FadeOut(50);
                ScoreDiffText.Background.FadeOut(50);
                ScoreDiffText.SuccessIcon.FadeOut(50);
                var color = invertTextColor ? black : Colour4.White;
                Score1Text.DrawableCount.Colour = Score2Text.DrawableCount.Colour = ScoreDiffText.DrawableCount.Colour = color;
                return;
            }

            Score1Text.Background.FadeIn(50);
            Score2Text.Background.FadeIn(50);
            ScoreDiffText.Background.FadeIn(50);
            ScoreDiffText.SuccessIcon.FadeIn(50);

            Score1Text.DrawableCount.Colour = Score2Text.DrawableCount.Colour = ScoreDiffText.DrawableCount.Colour = Color4Extensions.FromHex("383838");

            // 不是哥们，太蠢了吧
            Scheduler.AddDelayed(() => ShowSuccess.Value = false, 10000);
        }
    }
}
