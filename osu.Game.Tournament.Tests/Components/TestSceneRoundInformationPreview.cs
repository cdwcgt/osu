// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Game.Tournament.Screens.Gameplay.Components;

namespace osu.Game.Tournament.Tests.Components
{
    public partial class TestSceneRoundInformationPreview : TournamentTestScene
    {
        [Test]
        public void TestRoundInformatinoPreview()
        {
            AddStep("Clear All", Clear);
            AddStep("Add Round Information preview", () => Add(new RoundInformationPreview()
            {
                Origin = Anchor.Centre,
                Anchor = Anchor.Centre,
            }));
        }
    }
}
