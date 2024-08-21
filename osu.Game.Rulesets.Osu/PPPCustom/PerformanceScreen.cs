// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Screens;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.Leaderboards;
using osu.Game.Scoring;
using osu.Game.Screens;
using osu.Game.Screens.Ranking;
using Box = osu.Framework.Graphics.Shapes.Box;
using Color4 = osuTK.Graphics.Color4;

namespace osu.Game.Rulesets.Osu.PPPCustom
{
    public partial class PerformanceScreen : OsuScreen
    {
        private readonly FillFlowContainer dataHeaderContainer;

        private List<PPPCalculateNotification.PerformanceWithScore> performances;
        private readonly OsuTabControl<Skills> control;
        private readonly Container detailContainer;

        private readonly Dictionary<Skills, FillFlowContainer> skillContainers = new Dictionary<Skills, FillFlowContainer>();

        public PerformanceScreen(List<PPPCalculateNotification.PerformanceWithScore> performances)
        {
            InternalChildren = new Drawable[]
            {
                new Box
                {
                    Colour = Color4.Black,
                    Alpha = 0.8f,
                    RelativeSizeAxes = Axes.Both,
                },
                new OsuScrollContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Children = new Drawable[]
                        {
                            new FillFlowContainer
                            {
                                AutoSizeAxes = Axes.Y,
                                Width = 200f,
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Direction = FillDirection.Horizontal,
                                Children = new Drawable[]
                                {
                                    new OsuSpriteText
                                    {
                                        Height = 100f,
                                        Text = "Performance Data"
                                    },
                                    dataHeaderContainer = new FillFlowContainer
                                    {
                                        Width = 200f,
                                        AutoSizeAxes = Axes.Y
                                    }
                                }
                            },
                            control = new OsuTabControl<Skills>
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Width = 500f,
                                Height = 50f,
                            },
                            detailContainer = new Container
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Width = 500f,
                                AutoSizeAxes = Axes.Y,
                            }
                        }
                    },
                },
            };

            this.performances = performances;

            getData();

            control.Current.BindValueChanged(s =>
            {
                if (!skillContainers.TryGetValue(s.NewValue, out var container))
                    return;

                detailContainer.Clear(false);
                detailContainer.Add(container);
            }, true);
        }

        public enum Skills
        {
            All,
            Aim,
            Jump,
            Flow,
            Speed,
            Precision,
            Stamina,
            Accuracy,
        }

        private void getData()
        {
            ScoreWithSkillPP[] scoreWithAllpp = performances.Select(score => new ScoreWithSkillPP(score.OsuPerformanceAttributes.Total, score.Score)).ToArray();
            ScoreWithSkillPP[] scoreWithAimpp = performances.Select(score => new ScoreWithSkillPP(score.OsuPerformanceAttributes.Aim, score.Score)).ToArray();
            ScoreWithSkillPP[] scoreWithJumppp = performances.Select(score => new ScoreWithSkillPP(score.OsuPerformanceAttributes.JumpAim, score.Score)).ToArray();
            ScoreWithSkillPP[] scoreWithFlowpp = performances.Select(score => new ScoreWithSkillPP(score.OsuPerformanceAttributes.FlowAim, score.Score)).ToArray();
            ScoreWithSkillPP[] scoreWithSpeedpp = performances.Select(score => new ScoreWithSkillPP(score.OsuPerformanceAttributes.Speed, score.Score)).ToArray();
            ScoreWithSkillPP[] scoreWithPrecisionpp = performances.Select(score => new ScoreWithSkillPP(score.OsuPerformanceAttributes.Precision, score.Score)).ToArray();
            ScoreWithSkillPP[] scoreWithStaminanpp = performances.Select(score => new ScoreWithSkillPP(score.OsuPerformanceAttributes.Stamina, score.Score)).ToArray();
            ScoreWithSkillPP[] scoreWithAccuracynpp = performances.Select(score => new ScoreWithSkillPP(score.OsuPerformanceAttributes.Accuracy, score.Score)).ToArray();

            addDataToContainer(Skills.All, scoreWithAllpp);
            addDataToContainer(Skills.Aim, scoreWithAimpp);
            addDataToContainer(Skills.Jump, scoreWithJumppp);
            addDataToContainer(Skills.Flow, scoreWithFlowpp);
            addDataToContainer(Skills.Speed, scoreWithSpeedpp);
            addDataToContainer(Skills.Precision, scoreWithPrecisionpp);
            addDataToContainer(Skills.Stamina, scoreWithStaminanpp);
            addDataToContainer(Skills.Accuracy, scoreWithAccuracynpp);
        }

        private void addDataToContainer(Skills skillName, IEnumerable<ScoreWithSkillPP> scores)
        {
            ScoreWithSkillPP[] groupedScores = scores
                                               .GroupBy(i => i.score.BeatmapInfo?.OnlineID)
                                               .Select(g => g.MaxBy(i => i.PP))
                                               .OrderByDescending(i => i!.PP)
                                               .ToArray();

            double factor = 1;
            double totalPp = 0;

            var fillFlowContainer = new FillFlowContainer
            {
                Width = 500f,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical
            };

            int i = 0;

            foreach (var score in groupedScores)
            {
                totalPp += score.PP * factor;
                factor *= 0.95;

                if (i <= 100)
                {
                    i++;
                    fillFlowContainer.Add(createScore(score, i));
                }
            }

            skillContainers.Add(skillName, fillFlowContainer);

            var data = new Container
            {
                Width = 200f,
                Height = 50f,
                Children = new[]
                {
                    new OsuSpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Text = skillName.ToString(),
                        Margin = new MarginPadding(5)
                    },
                    new OsuSpriteText
                    {
                        Anchor = Anchor.CentreRight,
                        Text = Math.Round(totalPp, 2, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture),
                        Margin = new MarginPadding(5)
                    },
                }
            };

            dataHeaderContainer.Add(data);
        }

        private Container createScore(ScoreWithSkillPP score, int? rank)
        {
            return new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Children = new Drawable[]
                {
                    new LeaderboardScore(score.score, rank)
                    {
                        RelativeSizeAxes = Axes.None,
                        Width = 450,
                        Action = () => this.Push(new SoloResultsScreen(score.score))
                    },
                    new OsuSpriteText
                    {
                        Origin = Anchor.CentreRight,
                        Anchor = Anchor.CentreRight,
                        Text = Math.Round(score.PP, 2, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture),
                    }
                }
            };
        }

        public class ScoreWithSkillPP
        {
            public double PP;
            public ScoreInfo score;

            public ScoreWithSkillPP(double pp, ScoreInfo score)
            {
                PP = pp;
                this.score = score;
            }
        }
    }
}
