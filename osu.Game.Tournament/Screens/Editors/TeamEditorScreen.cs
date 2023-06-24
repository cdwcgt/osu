// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Online.API;
using osu.Game.Overlays.Settings;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Models;
using osu.Game.Users;
using osuTK;

namespace osu.Game.Tournament.Screens.Editors
{
    public partial class TeamEditorScreen : TournamentEditorScreen<TeamEditorScreen.TeamRow, TournamentTeam>
    {
        protected override BindableList<TournamentTeam> Storage => LadderInfo.Teams;

        private SettingsTextBox teamData;

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
                Width = 150,
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
            string[] data = teamData.Current.Value.Split(",");

            int[] beatmaps = { 4128922, 4128929, 4128945, 4128925, 4128923, 4128927 };

            string[] mods = { "HyBird", "Jack", "Rice Mixed", "LN Mixed", "Stamina", "LN Density" };

            var team = new TournamentTeam
            {
                FullName = { Value = data[1] },
                Seed = { Value = data[0] },
                FlagName = { Value = string.Empty }
            };

            for (int i = 15; i < data.Length; i++)
            {
                if (int.TryParse(data[i], out int id))
                {
                    team.Players.Add(new TournamentUser
                    {
                        OnlineID = id
                    });
                }
            }

            for (int i = 0; i < 6; i++)
            {
                var result = new SeedingResult();
                result.Mod.Value = mods[i];

                var beatmap = new SeedingBeatmap
                {
                    ID = beatmaps[i],
                    Score = int.Parse(data[i + 3]),
                    Seed =
                    {
                        Value = int.Parse(data[i + 9])
                    }
                };

                result.Beatmaps.Add(beatmap);

                result.Seed.Value = int.Parse(data[i + 3]);

                team.SeedingResults.Add(result);
            }

            Storage.Add(team);
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

            private readonly Container drawableContainer;

            [Resolved(canBeNull: true)]
            private TournamentSceneManager sceneManager { get; set; }

            [Resolved]
            private LadderInfo ladderInfo { get; set; }

            public TeamRow(TournamentTeam team, TournamentScreen parent)
            {
                Model = team;

                Masking = true;
                CornerRadius = 10;

                PlayerEditor playerEditor = new PlayerEditor(Model)
                {
                    Width = 0.95f
                };

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        Colour = OsuColour.Gray(0.1f),
                        RelativeSizeAxes = Axes.Both,
                    },
                    drawableContainer = new Container
                    {
                        Size = new Vector2(100, 50),
                        Margin = new MarginPadding(10),
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                    },
                    new FillFlowContainer
                    {
                        Margin = new MarginPadding(5),
                        Spacing = new Vector2(5),
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
                            new SettingsSlider<int>
                            {
                                LabelText = "Last Year Placement",
                                Width = 0.33f,
                                Current = Model.LastYearPlacing
                            },
                            new SettingsButton
                            {
                                Width = 0.11f,
                                Margin = new MarginPadding(10),
                                Text = "Add player",
                                Action = () => playerEditor.CreateNew()
                            },
                            new DangerousSettingsButton
                            {
                                Width = 0.11f,
                                Text = "Delete Team",
                                Margin = new MarginPadding(10),
                                Action = () =>
                                {
                                    Expire();
                                    ladderInfo.Teams.Remove(Model);
                                },
                            },
                            playerEditor,
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
                        }
                    },
                };

                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;

                Model.FlagName.BindValueChanged(updateDrawable, true);
            }

            private void updateDrawable(ValueChangedEvent<string> flag)
            {
                drawableContainer.Child = new DrawableTeamFlag(Model);
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
                    protected IAPIProvider API { get; private set; }

                    [Resolved]
                    private TournamentGameBase game { get; set; }

                    private readonly Bindable<int?> playerId = new Bindable<int?>();

                    private readonly Container drawableContainer;

                    public PlayerRow(TournamentTeam team, TournamentUser user)
                    {
                        this.user = user;

                        Margin = new MarginPadding(10);

                        RelativeSizeAxes = Axes.X;
                        AutoSizeAxes = Axes.Y;

                        Masking = true;
                        CornerRadius = 5;

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
                                Padding = new MarginPadding { Right = 160 },
                                Spacing = new Vector2(5),
                                Direction = FillDirection.Horizontal,
                                AutoSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    new SettingsNumberBox
                                    {
                                        LabelText = "User ID",
                                        RelativeSizeAxes = Axes.None,
                                        Width = 200,
                                        Current = playerId,
                                    },
                                    drawableContainer = new Container
                                    {
                                        Size = new Vector2(100, 70),
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
                        drawableContainer.Child = new UserGridPanel(user.ToAPIUser()) { Width = 300 };
                    });
                }
            }
        }
    }
}
