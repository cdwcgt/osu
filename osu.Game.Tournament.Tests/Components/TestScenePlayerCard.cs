// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Tournament.Components;
using osuTK;

namespace osu.Game.Tournament.Tests.Components
{
    public partial class TestScenePlayerCard : TournamentTestScene
    {
        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        protected override bool UseOnlineAPI => true;

        [BackgroundDependencyLoader]
        private void load()
        {
            var req = new GetMeRequest();

            req.Success += me =>
            {
                Add(new TeamPlayerCard(me)
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Scale = new Vector2(4)
                });
            };

            api.Queue(req);
        }
    }
}
