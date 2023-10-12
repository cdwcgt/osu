// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Screens;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Rulesets;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;
using osu.Game.Screens.Edit.Setup;
using osuTK;

namespace osu.Game.Screens.ReplayVs
{
    public partial class ReplayVsSelectScreen : OsuScreen
    {
        private TeamContainer teamRedContainer;
        private TeamContainer teamBlueContainer;
        private OsuSpriteText errorText;
        private DatabasedLegacyScoreDecoder decoder;

        [Resolved]
        private BeatmapManager beatmapManager { get; set; }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours, RulesetStore rulesetStore)
        {
            decoder = new DatabasedLegacyScoreDecoder(rulesetStore, beatmapManager);

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
                        Size = new Vector2(1, 0.8f),
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(15),
                        Children = new Drawable[]
                        {
                            teamRedContainer = new TeamContainer("Team red", colours.Red)
                            {
                                RelativeSizeAxes = Axes.Both,
                                Size = new Vector2(0.5f, 1)
                            },
                            teamBlueContainer = new TeamContainer("Team blue", colours.Blue)
                            {
                                RelativeSizeAxes = Axes.Both,
                                Size = new Vector2(0.5f, 1)
                            },
                        }
                    },
                    new RoundedButton
                    {
                        Text = "Start",
                        Action = validateReplays,
                        Size = new Vector2(0.4f, 0.1f),
                        RelativeSizeAxes = Axes.Both,
                        Y = -50,
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre
                    },
                    errorText = new OsuSpriteText
                    {
                        Font = OsuFont.Default.With(size: 30),
                        Alpha = 0,
                        Y = -20,
                        Colour = colours.Red,
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre
                    }
                }
            };
        }

        private partial class BasicOsuButton : OsuButton
        {
        }

        private partial class TeamContainer : Container
        {
            private readonly string name;
            private readonly ColourInfo colour;
            private int index = 1;

            private LabelledFileChooser newFileChooser => new LabelledFileChooser(".osr")
            {
                Width = 0.8f,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                FixedLabelWidth = 60,
                Label = index.ToString(),
                Text = "Click to select a replay",
                TabbableContentContainer = this
            };

            private FillFlowContainer flowContainer;

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
                        Width = 1,
                    },
                    new OsuScrollContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(1, 0.8f),
                        Y = 20f,
                        Child = flowContainer = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Spacing = new Vector2(15),
                            Direction = FillDirection.Vertical
                        }
                    },
                    new OsuSpriteText
                    {
                        Font = OsuFont.Default.With(size: 40),
                        Text = name,
                        Colour = colour,
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre
                    },
                    new BasicOsuButton
                    {
                        Text = "+",
                        Size = new Vector2(30),
                        Action = addFileChooser,
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        RelativePositionAxes = Axes.X,
                        X = 0.3f,
                        Padding = new MarginPadding
                        {
                            Top = 5f
                        }
                    }
                };
                addFileChooser();
            }

            private void addFileChooser()
            {
                var fileChooser = newFileChooser;
                flowContainer.Add(fileChooser);
                index++;
            }

            public string[] Files => flowContainer.Children
                                                  .Cast<LabelledFileChooser>()
                                                  .Where(f => f.Current.Value?.FullName != string.Empty)
                                                  .Select(f => f.Current.Value?.FullName)
                                                  .ToArray();
        }

        private void validateReplays()
        {
            string[] teamRedFiles = teamRedContainer.Files;
            string[] teamBlueFiles = teamBlueContainer.Files;

            if (teamRedFiles.Length == 0 || teamBlueFiles.Length == 0)
            {
                showError("Select at least one replay for each team");
                return;
            }

            var teamRedScores = new List<Score>();
            var teamBlueScores = new List<Score>();

            try
            {
                teamRedScores.Add(parseReplay(teamRedFiles[0]));
                var beatmapInfo = teamRedScores[0].ScoreInfo.BeatmapInfo;
                var beatmap = beatmapManager.GetWorkingBeatmap(beatmapInfo);

                for (int i = 1; i < teamRedFiles.Length; i++)
                {
                    string file = teamRedFiles[i];
                    var score = parseReplay(file);
                    if (score.ScoreInfo.BeatmapInfo.Equals(beatmapInfo))
                        teamRedScores.Add(score);
                }

                for (int i = 0; i < teamBlueFiles.Length; i++)
                {
                    string file = teamBlueFiles[i];
                    var score = parseReplay(file);
                    if (score.ScoreInfo.BeatmapInfo.Equals(beatmapInfo))
                        teamBlueScores.Add(score);
                }

                this.Push(new ReplayVsScreen(teamRedScores.ToArray(), teamBlueScores.ToArray(), beatmap));
            }
            catch (LegacyScoreDecoder.BeatmapNotFoundException e)
            {
                showError(e.Message);
            }
        }

        private void showError(string error)
        {
            errorText.Text = error;
            errorText.FadeIn().Then().ScaleTo(1.05f, 100, Easing.Out).Then().ScaleTo(1f, 50f);
        }

        private Score parseReplay(string path)
        {
            using var stream = new FileStream(path, FileMode.Open);
            return decoder.Parse(stream);
        }
    }
}
