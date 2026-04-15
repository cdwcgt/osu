// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Tournament.Models;
using osu.Game.Tournament.Screens.Gameplay.Components;

namespace osu.Game.Tournament.Tests.Components
{
    public partial class TestSceneFourTeamMatchHeader : TournamentTestScene
    {
        public TestSceneFourTeamMatchHeader()
        {
            var teamList = new BindableList<TournamentMatchSlot>(new[]
            {
                new TournamentMatchSlot(getTeam(), TeamColour.Red),
                new TournamentMatchSlot(getTeam(), TeamColour.Blue),
                new TournamentMatchSlot(getTeam(), TeamColour.Yellow),
                new TournamentMatchSlot(getTeam(), TeamColour.Green)
            });

            Child = new FourTeamMatchHeader(teamList)
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            };
        }

        private TournamentTeam getTeam() => new TournamentTeam
        {
            FlagName = { Value = "1" },
            FullName = { Value = "Test" },
            Seed = { Value = "#5" },
            Players =
            {
                new TournamentUser { Username = "Banchobot" },
            },
        };
    }
}
