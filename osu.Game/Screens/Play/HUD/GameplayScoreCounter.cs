// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring.Legacy;

namespace osu.Game.Screens.Play.HUD
{
    public abstract partial class GameplayScoreCounter : ScoreCounter
    {
        private Bindable<ScoringMode> scoreDisplayMode = null!;

        private Bindable<long> totalScoreBindable = null!;

        protected GameplayScoreCounter()
            : base(8)
        {
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config, ScoreProcessor scoreProcessor)
        {
            totalScoreBindable = scoreProcessor.TotalScore.GetBoundCopy();
            totalScoreBindable.BindValueChanged(_ => updateDisplayScore());

            scoreDisplayMode = config.GetBindable<ScoringMode>(OsuSetting.ScoreDisplayMode);
            updateDisplayScore();

            void updateDisplayScore() => Current.Value = scoreProcessor.GetDisplayScore(scoreDisplayMode.Value);
        }
    }
}
