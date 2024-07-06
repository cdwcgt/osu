// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays.Settings;
using osu.Game.Screens.Play.PlayerSettings;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Models;
using osuTK;

namespace osu.Game.Tournament.Screens.Ladder.Components
{
    public partial class LadderEditorSettings : CompositeDrawable
    {
        private SettingsDropdown<TournamentRound?> roundDropdown = null!;
        private PlayerCheckbox losersCheckbox = null!;
        private DateTextBox dateTimeBox = null!;
        private SettingsTeamDropdown team1Dropdown = null!;
        private SettingsTeamDropdown team2Dropdown = null!;
        private SettingsTextBox dataPathTextBox = null!;
        public readonly BindableList<TournamentMatch> MatchInfoList = new BindableList<TournamentMatch>();
        private OsuSpriteText remainData = null!;

        [Resolved]
        private LadderEditorInfo editorInfo { get; set; } = null!;

        [Resolved]
        private LadderInfo ladderInfo { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(5),
                Children = new Drawable[]
                {
                    team1Dropdown = new SettingsTeamDropdown(ladderInfo.Teams) { LabelText = "Team 1" },
                    team2Dropdown = new SettingsTeamDropdown(ladderInfo.Teams) { LabelText = "Team 2" },
                    roundDropdown = new SettingsRoundDropdown(ladderInfo.Rounds) { LabelText = "Round" },
                    losersCheckbox = new PlayerCheckbox { LabelText = "Losers Bracket" },
                    dateTimeBox = new DateTextBox { LabelText = "Match Time" },
                    dataPathTextBox = new SettingsTextBox { LabelText = "Data path" },
                    new SettingsButton { Text = "Import data", Action = importCsvData },
                    remainData = new OsuSpriteText()
                },
            };

            editorInfo.Selected.ValueChanged += selection =>
            {
                // ensure any ongoing edits are committed out to the *current* selection before changing to a new one.
                GetContainingFocusManager()?.TriggerFocusContention(null);

                // Required to avoid cyclic failure in BindableWithCurrent (TriggerChange called during the Current_Set process).
                // Arguable a framework issue but since we haven't hit it anywhere else a local workaround seems best.
                roundDropdown.Current.ValueChanged -= roundDropdownChanged;

                roundDropdown.Current = selection.NewValue.Round;
                losersCheckbox.Current = selection.NewValue.Losers;
                dateTimeBox.Current = selection.NewValue.Date;

                team1Dropdown.Current = selection.NewValue.Team1;
                team2Dropdown.Current = selection.NewValue.Team2;

                roundDropdown.Current.ValueChanged += roundDropdownChanged;
            };

            MatchInfoList.BindCollectionChanged((_, _) =>
            {
                remainData.Current.Value = $"Remain data: {MatchInfoList.Count}";
            }, true);
        }

        private void importCsvData()
        {
            try
            {
                string path = dataPathTextBox.Current.Value;
                string[] content = File.ReadAllText(path, Encoding.UTF8).Split(Environment.NewLine.ToCharArray());

                const string dateFormat = "'('ddd')' MMM d";
                const string timeFormat = "h\\:mm";

                foreach (string datas in content)
                {
                    try
                    {
                        string[] data = datas.Split(",");

                        DateTime date = DateTime.ParseExact(data[0], dateFormat, CultureInfo.InvariantCulture);
                        TimeSpan time = TimeSpan.ParseExact(data[1], timeFormat, CultureInfo.InvariantCulture);
                        DateTime combinedDateTime = date.Add(time);

                        TournamentTeam? team1 = ladderInfo.Teams.FirstOrDefault(t => t.FullName.Value == data[2]);
                        TournamentTeam? team2 = ladderInfo.Teams.FirstOrDefault(t => t.FullName.Value == data[3]);

                        var match = new TournamentMatch
                        {
                            Date =
                            {
                                Value = combinedDateTime
                            },
                            ID = 0,
                            Team1Acronym = team1?.Acronym.Value,
                            Team2Acronym = team2?.Acronym.Value
                        };

                        MatchInfoList.Add(match);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
            catch
            {
            }
        }

        private void roundDropdownChanged(ValueChangedEvent<TournamentRound?> round)
        {
            if (editorInfo.Selected.Value?.Date.Value < round.NewValue?.StartDate.Value)
            {
                editorInfo.Selected.Value.Date.Value = round.NewValue.StartDate.Value;
                editorInfo.Selected.TriggerChange();
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            this.FadeIn();
        }

        protected override bool OnHover(HoverEvent e)
        {
            return false;
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
        }

        private partial class SettingsRoundDropdown : SettingsDropdown<TournamentRound?>
        {
            public SettingsRoundDropdown(BindableList<TournamentRound> rounds)
            {
                Current = new Bindable<TournamentRound?>();

                foreach (var r in rounds.Prepend(new TournamentRound()))
                    add(r);

                rounds.CollectionChanged += (_, args) =>
                {
                    switch (args.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            Debug.Assert(args.NewItems != null);

                            args.NewItems.Cast<TournamentRound>().ForEach(add);
                            break;

                        case NotifyCollectionChangedAction.Remove:
                            Debug.Assert(args.OldItems != null);

                            args.OldItems.Cast<TournamentRound>().ForEach(i => Control.RemoveDropdownItem(i));
                            break;
                    }
                };
            }

            private readonly List<IUnbindable> refBindables = new List<IUnbindable>();

            private T boundReference<T>(T obj)
                where T : IBindable
            {
                obj = (T)obj.GetBoundCopy();
                refBindables.Add(obj);
                return obj;
            }

            private void add(TournamentRound round)
            {
                Control.AddDropdownItem(round);
                boundReference(round.Name).BindValueChanged(_ =>
                {
                    Control.RemoveDropdownItem(round);
                    Control.AddDropdownItem(round);
                });
            }
        }
    }
}
