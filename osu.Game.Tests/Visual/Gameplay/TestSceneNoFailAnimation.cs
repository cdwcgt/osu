// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Configuration;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Scoring;

namespace osu.Game.Tests.Visual.Gameplay
{
    public partial class TestSceneNoFailAnimation : OsuPlayerTestScene
    {
        protected override bool AllowFail => true;

        protected override bool HasCustomSteps => true;

        private bool enableFailAnimationByPlayer = true;

        protected override TestPlayer CreatePlayer(Ruleset ruleset)
        {
            SelectedMods.Value = new[] { new OsuModSuddenDeath() };
            return new TestNoFailAnimationPlayer(enableFailAnimationByPlayer);
        }

        [Test]
        public void TestOsuWithNoFailAnimationEnable()
        {
            AddStep("allow fail animation by player", () => enableFailAnimationByPlayer = true);
            AddStep("enable No Fail Animation", () => LocalConfig.SetValue(OsuSetting.NoFailAnimation, true));
            CreateTest();
            AddUntilStep("wait for score rank F", () => Player.Score.ScoreInfo.Rank == ScoreRank.F);
            AddUntilStep("player has not fail", () => !Player.GameplayState.HasFailed);
        }

        [Test]
        public void TestOsuWithNoFailAnimationDisable()
        {
            AddStep("allow fail animation by player", () => enableFailAnimationByPlayer = true);
            AddStep("disable no fail animation", () => LocalConfig.SetValue(OsuSetting.NoFailAnimation, false));
            CreateTest();
            AddUntilStep("wait for score rank F", () => Player.Score.ScoreInfo.Rank == ScoreRank.F);
            AddUntilStep("player has failed", () => Player.GameplayState.HasFailed);
        }

        /// <summary>
        /// This case is mainly for multiplayer games, Multiplayer has Fail Animation disallow itself, but it cannot be affects by settings.
        /// </summary>
        [Test]
        public void TestNoFailAnimationEnableByPlayer()
        {
            AddStep("disallow fail animation by player", () => enableFailAnimationByPlayer = false);
            AddStep("disable no fail animation", () => LocalConfig.SetValue(OsuSetting.NoFailAnimation, false));
            CreateTest();
            AddUntilStep("wait for score rank F", () => Player.Score.ScoreInfo.Rank == ScoreRank.F);
            AddUntilStep("player has not fail", () => !Player.GameplayState.HasFailed);
        }

        private partial class TestNoFailAnimationPlayer : TestPlayer
        {
            public TestNoFailAnimationPlayer(bool allowFailAnimation)
            {
                Configuration.AllowFailAnimation = allowFailAnimation;
            }
        }
    }
}
