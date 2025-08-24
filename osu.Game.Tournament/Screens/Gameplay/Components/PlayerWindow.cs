// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Game.Tournament.Components;
using osuTK;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class PlayerWindow : CapturedWindowSprite
    {
        public PlayerWindow(string windowTitle)
            : base(windowTitle)
        {
        }

        public void FlyingLaunch(bool clockWise)
        {
            this.ScaleTo(0.9f, 1200, Easing.InQuint)
                .RotateTo(180f * (clockWise ? 1 : -1), 1200, Easing.InQuint)
                .MoveToOffset(new Vector2(0, -50), 1200, Easing.InQuint);

            this.Delay(1200f)
                .RotateTo(720f * (clockWise ? 1 : -1), 5000, Easing.OutQuint)
                .MoveToOffset(new Vector2(0, -DrawHeight * 4), 5000, Easing.OutQuint);
        }

        public void Reset()
        {
            this.ScaleTo(1, 5000, Easing.InOutSine);
            this.MoveTo(new Vector2(0, 0), 5000, Easing.InOutSine);
            this.RotateTo(0, 5000, Easing.InOutSine);
        }
    }
}
