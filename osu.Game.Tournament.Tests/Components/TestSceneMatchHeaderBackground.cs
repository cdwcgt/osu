// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Tournament.Screens.Gameplay.Components.MatchHeader;
using osuTK;

namespace osu.Game.Tournament.Tests.Components
{
    public partial class TestSceneMatchHeaderBackground : TournamentTestScene
    {
        public TestSceneMatchHeaderBackground()
        {
            Child = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Spacing = new Vector2(0f, 5f),
                Scale = new Vector2(5f),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Direction = FillDirection.Vertical,
                Children = new Drawable[]
                {
                    new MatchRoundDisplay.MatchHeaderBackground()
                }
            };
        }
    }
}
