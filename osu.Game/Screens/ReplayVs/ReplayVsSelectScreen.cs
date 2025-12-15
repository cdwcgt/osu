// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Screens;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Scoring;
using osu.Game.Screens.ReplayVs.Select;
using osuTK;

namespace osu.Game.Screens.ReplayVs
{
    public partial class ReplayVsSelectScreen : OsuScreen
    {
        private TeamContainer teamRedContainer = null!;
        private TeamContainer teamBlueContainer = null!;
        private OsuSpriteText errorText = null!;

        [Resolved]
        private ScoreManager scoreManager { get; set; } = null!;

        [Resolved]
        private BeatmapManager beatmapManager { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            InternalChild = new PopoverContainer
            {
                CornerRadius = 10,
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(0.8f, 0.9f),
                Children = new Drawable[]
                {
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(0.5f, 0.8f),
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(15),
                        Children = new Drawable[]
                        {
                            teamRedContainer = new TeamContainer("Team red", colours.Red)
                            {
                                RelativeSizeAxes = Axes.Both,
                            },
                            teamBlueContainer = new TeamContainer("Team blue", colours.Blue)
                            {
                                RelativeSizeAxes = Axes.Both,
                            },
                        }
                    },
                    new RoundedButton
                    {
                        Text = "Start",
                        Action = validateAndPush,
                        Size = new Vector2(0.4f, 0.1f),
                        RelativeSizeAxes = Axes.Both,
                        RelativePositionAxes = Axes.Y,
                        Y = -0.05f,
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre
                    },
                    errorText = new OsuSpriteText
                    {
                        Font = OsuFont.Default.With(size: 30),
                        Alpha = 0,
                        RelativePositionAxes = Axes.Y,
                        Y = -0.01f,
                        Colour = colours.Red,
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre
                    }
                }
            };
        }

        public partial class TeamContainer : Container
        {
            private readonly string name;
            private readonly ColourInfo colour;
            public ScoreList ScoreList { get; private set; } = null!;

            [Resolved]
            private IPerformFromScreenRunner? performer { get; set; }

            public TeamContainer(string name, ColourInfo colour)
            {
                this.name = name;
                this.colour = colour;
                CornerRadius = 15;
                Masking = true;
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                Children = new Drawable[]
                {
                    new Box
                    {
                        Colour = colours.GreySeaFoamDark,
                        RelativeSizeAxes = Axes.Both,
                    },
                    ScoreList = new ScoreList
                    {
                        RelativeSizeAxes = Axes.Both,
                        RelativePositionAxes = Axes.Y,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Y = 0.1f,
                    },
                    new OsuSpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Font = OsuFont.Default.With(size: 40),
                        Text = name,
                        Colour = colour
                    },
                    new IconButton
                    {
                        Icon = FontAwesome.Solid.PlusCircle,
                        Action = openSongSelect,
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        RelativePositionAxes = Axes.Both,
                        Position = new Vector2(0.25f, 0.01f)
                    }
                };
            }

            private void openSongSelect()
            {
                Schedule(() => performer?.PerformFromScreen(s => s.Push(new ReplayVsSongSelect(this)), new[] { typeof(ReplayVsSelectScreen) }));
            }

            public void AddScore(ScoreInfo scoreInfo)
            {
                if (!ScoreList.Items.Contains(scoreInfo))
                    ScoreList.Items.Add(scoreInfo);
            }
        }

        private void showError(string error)
        {
            errorText.Text = error;
            errorText.FadeInFromZero().ScaleTo(1.1f, 100, Easing.Out).Then().ScaleTo(1f, 50f).Delay(3000).FadeOut(500);
        }

        private void validateAndPush()
        {
            var teamRedScoreInfos = teamRedContainer.ScoreList.Items;
            var teamBlueScoreInfos = teamBlueContainer.ScoreList.Items;

            if (teamRedScoreInfos.Count + teamBlueScoreInfos.Count == 0)
            {
                showError("Select at least one replay");
                return;
            }

            var firstScore = teamRedScoreInfos.Count > 0 ? teamRedScoreInfos[0] : teamBlueScoreInfos[0];
            var beatmapInfo = firstScore.BeatmapInfo;
            var teamRedScores = teamRedScoreInfos.Where(s => s.BeatmapInfo!.Equals(beatmapInfo)).Select(s => scoreManager.GetScore(s)!).ToArray();
            var teamBlueScores = teamBlueScoreInfos.Where(s => s.BeatmapInfo!.Equals(beatmapInfo)).Select(s => scoreManager.GetScore(s)!).ToArray();

            this.Push(new ReplayVsScreen(teamRedScores, teamBlueScores, beatmapManager.GetWorkingBeatmap(beatmapInfo)));
        }
    }
}
