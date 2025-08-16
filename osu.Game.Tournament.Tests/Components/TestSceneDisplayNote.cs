// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Tournament.Models;
using osu.Game.Tournament.Screens.Gameplay.Components.MatchHeader;
using osuTK;

namespace osu.Game.Tournament.Tests.Components
{
    public partial class TestSceneDisplayNote : TournamentTestScene
    {
        [Test]
        public void TestDisplayNote()
        {
            AddStep("clear", Clear);
            AddStep("add display note", () => Add(new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Spacing = new Vector2(0f, 5f),
                Scale = new Vector2(5f),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Direction = FillDirection.Vertical,
                Children = new Drawable[]
                {
                    new TeamDisplayNote(TeamColour.Red)
                    {
                        Text = "开始接单了"
                    },
                    new TeamDisplayNote(TeamColour.Blue)
                    {
                        Text = "支持红猪加速器"
                    },
                }
            }));
        }
    }
}
