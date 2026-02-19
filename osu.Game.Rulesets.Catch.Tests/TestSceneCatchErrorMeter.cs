// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osu.Framework.Threading;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Catch.HUD;
using osu.Game.Rulesets.Catch.Judgements;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch.UI;
using osu.Game.Rulesets.Scoring;
using osu.Game.Tests.Visual;
using osuTK;

namespace osu.Game.Rulesets.Catch.Tests
{
    public partial class TestSceneCatchErrorMeter : OsuManualInputManagerTestScene
    {
        private DependencyProvidingContainer dependencyContainer = null!;
        private ScoreProcessor scoreProcessor = null!;

        private TestAimErrorMeter catchErrorMeter = null!;

        private ScheduledDelegate? automaticAdditionDelegate;
        private Catcher catcher;

        [SetUpSteps]
        public void SetupSteps() => AddStep("Create components", () =>
        {
            automaticAdditionDelegate?.Cancel();
            automaticAdditionDelegate = null;

            var ruleset = new CatchRuleset();

            var difficulty = new BeatmapDifficulty
            {
                CircleSize = 5,
            };
            var droppedObjectContainer = new DroppedObjectContainer();

            scoreProcessor = new ScoreProcessor(ruleset);
            Child = dependencyContainer = new DependencyProvidingContainer
            {
                RelativeSizeAxes = Axes.Both,
                CachedDependencies = new (Type, object)[]
                {
                    (typeof(ScoreProcessor), scoreProcessor)
                }
            };
            dependencyContainer.Children = new Drawable[]
            {
                catchErrorMeter = new TestAimErrorMeter
                {
                    Y = 100,
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Scale = new Vector2(2),
                },
                droppedObjectContainer,
                catcher = new Catcher(droppedObjectContainer, difficulty)
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Y = 200,
                }
            };
        });

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            catchErrorMeter.AddPoint(catcher.ToLocalSpace(e.ScreenSpaceMouseDownPosition).X - (catcher.Width / 2));
            return true;
        }

        private partial class TestAimErrorMeter : CatchErrorMeter
        {
            public void AddPoint(float position)
            {
                OnNewJudgement(new CatchJudgementResult(new Fruit(), new CatchJudgement())
                {
                    CatcherPosition = position
                });
            }
        }
    }
}
