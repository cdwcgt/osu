// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;
using osu.Game.Tournament.Models;
using osu.Game.Tournament.Screens.Editors.Components;
using osu.Game.Tournament.Screens.Drawings.Components;
using osu.Game.Users;
using osuTK;

namespace osu.Game.Tournament.Screens.Editors
{
    public partial class TeamEditorScreen : TournamentEditorScreen<TeamEditorScreen.TeamRow, TournamentTeam>
    {
        protected override BindableList<TournamentTeam> Storage => LadderInfo.Teams;

        private SettingsTextBox teamData = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            ControlPanel.Add(new TourneyButton
            {
                RelativeSizeAxes = Axes.X,
                Text = "Add all countries",
                Action = addAllCountries
            });

            ControlPanel.Add(teamData = new SettingsTextBox
            {
                RelativeSizeAxes = Axes.X,
                LabelText = "data",
            });

            ControlPanel.Add(new TourneyButton
            {
                RelativeSizeAxes = Axes.X,
                Text = "Add Team",
                Action = addTeam
            });

            ControlPanel.Add(new TourneyButton
            {
                RelativeSizeAxes = Axes.X,
                Text = "Update Flag",
                Action = updateFlag
            });

            ControlPanel.Add(new TourneyButton
            {
                RelativeSizeAxes = Axes.X,
                Text = "Update Seed",
                Action = updateSeed
            });
        }

        protected override TeamRow CreateDrawable(TournamentTeam model) => new TeamRow(model, this);

        private void addAllCountries()
        {
            var countries = new List<TournamentTeam>();

            foreach (var country in Enum.GetValues<CountryCode>().Skip(1))
            {
                countries.Add(new TournamentTeam
                {
                    FlagName = { Value = country.ToString() },
                    FullName = { Value = country.GetDescription() },
                    Acronym = { Value = country.GetAcronym() },
                });
            }

            foreach (var c in countries)
                Storage.Add(c);
        }

        private void addTeam()
        {
            try
            {
                string path = teamData.Current.Value;
                string[] content = File.ReadAllText(path, Encoding.UTF8).Split(Environment.NewLine.ToCharArray());

                int[] beatmaps = content[0].Split(",").Skip(2).Select(int.Parse).ToArray();

                foreach (string item in content.Skip(1))
                {
                    try
                    {
                        string[] data = item.Split(",");

                        int[][] point =
                        {
                            new[] { 0, 1, 2 },
                            new[] { 3, 4 },
                            new[] { 5, 6 },
                            new[] { 7, 8 }
                        };

                        string[] mods = { "RC", "HB", "LN", "SV" };

                        var team = new TournamentTeam
                        {
                            FullName = { Value = data[0] },
                            Seed = { Value = data[19] },
                            Acronym = { Value = data[0] },
                            FlagName = { Value = data[1] }
                        };

                        if (int.TryParse(data[1], out int id))
                        {
                            team.Players.Add(new TournamentUser
                            {
                                OnlineID = id
                            });
                        }

                        for (int i = 0; i < mods.Length; i++)
                        {
                            var result = new SeedingResult();
                            result.Mod.Value = mods[i];

                            for (int j = 0; j < point[i].Length; j++)
                            {
                                int p = point[i][j];
                                var beatmap = new SeedingBeatmap
                                {
                                    ID = beatmaps[p],
                                    Score = int.Parse(data[p + 2]),
                                    Seed =
                                    {
                                        Value = int.Parse(data[p + 10])
                                    }
                                };
                                result.Beatmaps.Add(beatmap);
                            }

                            result.Seed.Value = int.Parse(data[i + 20]);

                            team.SeedingResults.Add(result);
                        }

                        Storage.Add(team);
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }

        private void updateSeed()
        {
            foreach (var team in Storage)
            {
                team.SeedingResults[1].Beatmaps[0].ID = 4128929;
                team.SeedingResults[1].Beatmaps[0].Beatmap = null;

                foreach (var seed in team.SeedingResults)
                {
                    seed.Seed.Value = seed.Beatmaps[0].Seed.Value;
                }
            }
        }

        private void updateFlag()
        {
            foreach (var team in Storage)
            {
                team.FlagName.Value = team.Players[0].CountryCode.ToString();
            }
        }

        public partial class TeamRow : CompositeDrawable, IModelBacked<TournamentTeam>
        {
            public TournamentTeam Model { get; }

            [Resolved]
            private TournamentSceneManager? sceneManager { get; set; }

            [Resolved]
            private IDialogOverlay? dialogOverlay { get; set; }

            [Resolved]
            private LadderInfo ladderInfo { get; set; } = null!;

            public TeamRow(TournamentTeam team, TournamentScreen parent)
            {
                Model = team;

                Masking = true;
                CornerRadius = 10;

                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;

                PlayerEditor playerEditor = new PlayerEditor(Model);

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        Colour = OsuColour.Gray(0.1f),
                        RelativeSizeAxes = Axes.Both,
                    },
                    new GroupTeam(team)
                    {
                        Margin = new MarginPadding(16),
                        Scale = new Vector2(2),
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                    },
                    new FillFlowContainer
                    {
                        Spacing = new Vector2(5),
                        Padding = new MarginPadding(10),
                        Direction = FillDirection.Full,
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Children = new Drawable[]
                        {
                            new SettingsTextBox
                            {
                                LabelText = "Name",
                                Width = 0.2f,
                                Current = Model.FullName
                            },
                            new SettingsTextBox
                            {
                                LabelText = "Acronym",
                                Width = 0.2f,
                                Current = Model.Acronym
                            },
                            new SettingsTextBox
                            {
                                LabelText = "Flag",
                                Width = 0.2f,
                                Current = Model.FlagName
                            },
                            new SettingsTextBox
                            {
                                LabelText = "Seed",
                                Width = 0.2f,
                                Current = Model.Seed
                            },
                            new SettingsSlider<int, LastYearPlacementSlider>
                            {
                                LabelText = "Last Year Placement",
                                Width = 0.33f,
                                Current = Model.LastYearPlacing
                            },
                            new SettingsButton
                            {
                                Width = 0.2f,
                                Margin = new MarginPadding(10),
                                Text = "Edit seeding results",
                                Action = () =>
                                {
                                    sceneManager?.SetScreen(new SeedingEditorScreen(team, parent));
                                }
                            },
                            playerEditor,
                            new SettingsButton
                            {
                                Text = "Add player",
                                Action = () => playerEditor.CreateNew()
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Children = new Drawable[]
                                {
                                    new DangerousSettingsButton
                                    {
                                        Width = 0.2f,
                                        Text = "Delete Team",
                                        Anchor = Anchor.TopRight,
                                        Origin = Anchor.TopRight,
                                        Action = () => dialogOverlay?.Push(new DeleteTeamDialog(Model, () =>
                                        {
                                            Expire();
                                            ladderInfo.Teams.Remove(Model);
                                        })),
                                    },
                                }
                            },
                        }
                    },
                };
            }

            private partial class LastYearPlacementSlider : RoundedSliderBar<int>
            {
                public override LocalisableString TooltipText => Current.Value == 0 ? "N/A" : base.TooltipText;
            }

            public partial class PlayerEditor : CompositeDrawable
            {
                private readonly TournamentTeam team;
                private readonly FillFlowContainer flow;

                public PlayerEditor(TournamentTeam team)
                {
                    this.team = team;

                    RelativeSizeAxes = Axes.X;
                    AutoSizeAxes = Axes.Y;

                    InternalChild = flow = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding(5),
                        Spacing = new Vector2(5),
                        ChildrenEnumerable = team.Players.Select(p => new PlayerRow(team, p))
                    };
                }

                public void CreateNew()
                {
                    var player = new TournamentUser();
                    team.Players.Add(player);
                    flow.Add(new PlayerRow(team, player));
                }

                public partial class PlayerRow : CompositeDrawable
                {
                    private readonly TournamentUser user;

                    [Resolved]
                    private TournamentGameBase game { get; set; } = null!;

                    private readonly Bindable<int?> playerId = new Bindable<int?>();

                    private readonly Container userPanelContainer;

                    public PlayerRow(TournamentTeam team, TournamentUser user)
                    {
                        this.user = user;

                        RelativeSizeAxes = Axes.X;
                        AutoSizeAxes = Axes.Y;

                        Masking = true;
                        CornerRadius = 10;

                        InternalChildren = new Drawable[]
                        {
                            new Box
                            {
                                Colour = OsuColour.Gray(0.2f),
                                RelativeSizeAxes = Axes.Both,
                            },
                            new FillFlowContainer
                            {
                                Margin = new MarginPadding(5),
                                Padding = new MarginPadding { Right = 60 },
                                Spacing = new Vector2(5),
                                Direction = FillDirection.Horizontal,
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Children = new Drawable[]
                                {
                                    new SettingsNumberBox
                                    {
                                        LabelText = "User ID",
                                        RelativeSizeAxes = Axes.None,
                                        Width = 200,
                                        Current = playerId,
                                    },
                                    userPanelContainer = new Container
                                    {
                                        Width = 400,
                                        RelativeSizeAxes = Axes.Y,
                                    },
                                }
                            },
                            new DangerousSettingsButton
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                RelativeSizeAxes = Axes.None,
                                Width = 150,
                                Text = "Delete Player",
                                Action = () =>
                                {
                                    Expire();
                                    team.Players.Remove(user);
                                },
                            }
                        };
                    }

                    [BackgroundDependencyLoader]
                    private void load()
                    {
                        playerId.Value = user.OnlineID;
                        playerId.BindValueChanged(id =>
                        {
                            user.OnlineID = id.NewValue ?? 0;

                            if (id.NewValue != id.OldValue)
                                user.Username = string.Empty;

                            if (!string.IsNullOrEmpty(user.Username))
                            {
                                updatePanel();
                                return;
                            }

                            game.PopulatePlayer(user, updatePanel, updatePanel);
                        }, true);
                    }

                    private void updatePanel() => Scheduler.AddOnce(() =>
                    {
                        userPanelContainer.Child = new UserListPanel(user.ToAPIUser())
                        {
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft,
                            Scale = new Vector2(1f),
                        };
                    });
                }
            }
        }
    }
}
