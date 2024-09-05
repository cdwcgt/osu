// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Osu.Judgements;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Osu.Scoring
{
    public partial class OsuScoreProcessor : ScoreProcessor
    {
        public OsuScoreProcessor()
            : base(new OsuRuleset())
        {
        }

        public override ScoreRank RankFromScore(double accuracy, IReadOnlyDictionary<HitResult, int> results)
        {
            ScoreRank rank = base.RankFromScore(accuracy, results);

            switch (rank)
            {
                case ScoreRank.S:
                case ScoreRank.X:
                    if (results.GetValueOrDefault(HitResult.Miss) > 0)
                        rank = ScoreRank.A;
                    break;
            }

            return rank;
        }

        protected override double ComputeTotalScore(double comboProgress, double accuracyProgress, double bonusPortion)
        {
            return 700000 * comboProgress
                   + 300000 * Math.Pow(Accuracy.Value, 10) * accuracyProgress
                   + bonusPortion;
        }

        protected override double GetComboScoreChange(JudgementResult result) => GetBaseScoreForResult(result.Type) * (1 + result.ComboAfterJudgement / 10d);

        protected override HitEvent CreateHitEvent(JudgementResult result)
            => base.CreateHitEvent(result).With((result as OsuHitCircleJudgementResult)?.CursorPositionAtHit);
    }
}
