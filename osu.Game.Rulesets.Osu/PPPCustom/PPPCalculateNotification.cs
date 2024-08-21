using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Screens;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Online.API;
using osu.Game.Overlays.Notifications;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Scoring;
using osu.Game.Screens;

namespace osu.Game.Rulesets.Osu.PPPCustom
{
    public partial class PPPCalculateNotification : ProgressNotification
    {
        [Resolved]
        private RealmAccess realm { get; set; } = null!;

        [Resolved]
        private BeatmapManager beatmaps { get; set; } = null!;

        [Resolved]
        private IPerformFromScreenRunner performer { get; set; } = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        private readonly List<PerformanceWithScore> performances = new List<PerformanceWithScore>();

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Task.Run(async () =>
            {
                Text = "Preparing scores...";
                var scores = realm.Run(r => r.All<ScoreInfo>().Detach()).Where(s => s.RulesetID == 0
                                                                                    && s.UserID == api.LocalUser.Value.OnlineID
                                                                                    && s.BeatmapInfo?.Status.GrantsPerformancePoints() == true
                                                                                    && !hasUnrankedMods(s)
                                                                                    && s.Rank != ScoreRank.F);
                await calculate(scores).ConfigureAwait(false);
            });
        }

        private static bool hasUnrankedMods(ScoreInfo scoreInfo)
        {
            IEnumerable<Mod> modsToCheck = scoreInfo.Mods;

            if (scoreInfo.IsLegacyScore)
                modsToCheck = modsToCheck.Where(m => m is not ModClassic);

            return modsToCheck.Any(m => !m.Ranked);
        }

        private async Task calculate(IEnumerable<ScoreInfo> scores)
        {
            State = ProgressNotificationState.Active;

            var osuRuleset = new OsuRuleset();
            var performanceCalculator = osuRuleset.CreatePerformanceCalculator();
            totalScores = scores.Count();

            var tasks = scores.Select(async score =>
            {
                if (score.BeatmapInfo == null)
                    return;

                var difficultyAttributes = await Task.Run(() =>
                                                         osuRuleset.CreateDifficultyCalculator(beatmaps.GetWorkingBeatmap(score.BeatmapInfo)).Calculate(score.Mods), CancellationToken)
                                                     .ConfigureAwait(false);

                var performanceAttributes = (OsuPerformanceAttributes)await performanceCalculator
                                                                            .CalculateAsync(score, difficultyAttributes, CancellationToken)
                                                                            .ConfigureAwait(false);

                lock (performances) // Ensure thread-safety when modifying shared list
                {
                    performances.Add(new PerformanceWithScore(performanceAttributes, score));
                }

                int completed = Interlocked.Increment(ref tasksCompleted);
                Text = $"Calculating... {completed} / {totalScores}";
                Progress = (float)completed / totalScores;
            }).ToArray();

            await Task.WhenAll(tasks).ConfigureAwait(false);

            CompletionText = "Click to get result";
            CompletionClickAction += () =>
            {
                performer.PerformFromScreen(screen =>
                {
                    screen.Push(new PerformanceScreen(performances));
                });

                return false;
            };
            State = ProgressNotificationState.Completed;
        }

        private int tasksCompleted = 0;
        private int totalScores;

        public class PerformanceWithScore
        {
            public OsuPerformanceAttributes OsuPerformanceAttributes { get; }
            public ScoreInfo Score { get; }

            public PerformanceWithScore(OsuPerformanceAttributes osuPerformanceAttributes, ScoreInfo score)
            {
                OsuPerformanceAttributes = osuPerformanceAttributes;
                Score = score;
            }
        }
    }
}
