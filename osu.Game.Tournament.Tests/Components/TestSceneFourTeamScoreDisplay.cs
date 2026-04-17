// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Utils;
using osu.Game.Tournament.IPC;
using osu.Game.Tournament.Screens.Gameplay.Components;

namespace osu.Game.Tournament.Tests.Components
{
    public partial class TestSceneFourTeamScoreDisplay : TournamentTestScene
    {
        [Cached(Type = typeof(MatchIPCInfo))]
        private MatchIPCInfo matchInfo = new MatchIPCInfo();

        public TestSceneFourTeamScoreDisplay()
        {
            Add(new FourTeamScoreDisplay
            {
                Width = 500f,
                Height = 145f,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Scheduler.AddDelayed(() =>
            {
                int amount = (int)((RNG.NextDouble() - 0.5) * 10000);
                if (amount < 0)
                    matchInfo.Score1.Value -= amount;
                else
                    matchInfo.Score2.Value += amount;

                int amount1 = (int)((RNG.NextDouble() - 0.5) * 10000);
                if (amount1 < 0)
                    matchInfo.Score3.Value -= amount1;
                else
                    matchInfo.Score4.Value += amount1;
            }, 100, true);
        }
    }
}
