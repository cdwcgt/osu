// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Game.Tournament.Models;
using osu.Game.Tournament.Screens.Gameplay.Components;

namespace osu.Game.Tournament.Tests.Components
{
    public partial class TestSceneRoundInformationPreview : TournamentTestScene
    {
        [Test]
        public void TestRoundInformatinoPreview()
        {
            AddStep("Clear All", () =>
            {
                Clear();
                Ladder.CurrentMatch.Value!.PicksBans.Clear();
            });
            AddStep("Add Round Information preview", () => Add(new RoundInformationPreview
            {
                Origin = Anchor.Centre,
                Anchor = Anchor.Centre,
            }));
            AddStep("Add a red pick", () => AddChoice(TeamColour.Red, ChoiceType.Pick));
            AddStep("Add a blue pick", () => AddChoice(TeamColour.Blue, ChoiceType.Pick));
            AddStep("Add a red protected", () => AddChoice(TeamColour.Red, ChoiceType.Protected));
            AddStep("Add a blue pick", () => AddChoice(TeamColour.Blue, ChoiceType.Protected));
            AddStep("Add a red ban", () => AddChoice(TeamColour.Red, ChoiceType.Ban));
            AddStep("Add a blue ban", () => AddChoice(TeamColour.Blue, ChoiceType.Ban));
        }

        private void AddChoice(TeamColour colour, ChoiceType type)
        {
            Ladder.CurrentMatch.Value!.PicksBans.Add(new BeatmapChoice
            {
                BeatmapID = 1,
                Team = colour,
                Type = type,
            });
        }
    }
}
