// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Testing;
using osu.Game.Screens.MapGuess;

namespace osu.Game.Tests.Visual.SongSelect
{
    public partial class TestSceneMapGuess : ScreenTestScene
    {
        [SetUpSteps]
        public override void SetUpSteps()
        {
            base.SetUpSteps();

            AddStep("load screen", () => Stack.Push(new MapGuessConfigScreen()));
            AddUntilStep("wait for load", () => Stack.CurrentScreen is MapGuessConfigScreen mapGuess && mapGuess.IsLoaded);
        }
    }
}
