// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;

namespace osu.Game.Tournament.Components
{
    public partial class AutoAdvancePrompt : CompositeDrawable
    {
        private readonly Action delayAction;
        private readonly double delayTime;
        private readonly TournamentSpriteText promptText;

        private double countdownStartTime;

        public AutoAdvancePrompt(Action delayAction, double delayTime)
        {
            this.delayAction = delayAction;
            this.delayTime = delayTime;

            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;

            InternalChildren = new Drawable[]
            {
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Y,
                    RelativeSizeAxes = Axes.X,
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        promptText = new TournamentSpriteText
                        {
                            Font = OsuFont.GetFont(size: 10),
                        },
                        new TourneyButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 30,
                            Text = "取消",
                            Action = Cancel
                        }
                    }
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            countdownStartTime = Time.Current;
        }

        protected override void Update()
        {
            base.Update();

            double remainTime = delayTime - (Time.Current - countdownStartTime);

            if (remainTime <= 0)
            {
                delayAction.Invoke();
                Expire();
                return;
            }

            promptText.Text = $"自动转场还有{remainTime / 1000:0.00}秒";
        }

        public void Cancel()
        {
            Expire();
        }
    }
}
