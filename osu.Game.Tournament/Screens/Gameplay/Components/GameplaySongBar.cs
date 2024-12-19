// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Game.Extensions;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Models;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class GameplaySongBar : SongBar
    {
        private readonly Bindable<TournamentMatch?> currentMatch = new Bindable<TournamentMatch?>();
        private readonly Bindable<ColourInfo> arrowColor = new Bindable<ColourInfo>(Color4.White);

        private TeamColour? pickTeamColour;
        private bool expanded;

        public bool Expanded
        {
            get => expanded;
            set
            {
                expanded = value;
                updatePosition();
            }
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            currentMatch.BindValueChanged(matchChanged);
            currentMatch.BindTo(Ladder.CurrentMatch);

            arrowColor.BindValueChanged(c =>
            {
                leftArrow.FadeColour(c.NewValue, 300);
                rightArrow.FadeColour(c.NewValue, 300);
            });

            Expanded = true;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            leftArrow.MoveToX(-25, 1500, Easing.Out).Then().MoveToX(0, 1500, Easing.In).Loop();
            rightArrow.MoveToX(25, 1500, Easing.Out).Then().MoveToX(0, 1500, Easing.In).Loop();
        }

        private void matchChanged(ValueChangedEvent<TournamentMatch?> match)
        {
            if (match.OldValue != null)
                match.OldValue.PicksBans.CollectionChanged -= picksBansOnCollectionChanged;
            if (match.NewValue != null)
                match.NewValue.PicksBans.CollectionChanged += picksBansOnCollectionChanged;

            Scheduler.AddOnce(updateState);
        }

        private void picksBansOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            => Scheduler.AddOnce(updateState);

        private void updateState()
        {
            pickTeamColour = currentMatch.Value?.PicksBans.FirstOrDefault(p => p.BeatmapID == beatmap?.OnlineID && p.Type == ChoiceType.Pick)?.Team;

            if (pickTeamColour == null)
            {
                arrowColor.Value = Color4.White;
                return;
            }

            arrowColor.Value = getTeamColour(pickTeamColour.Value);

            static ColourInfo getTeamColour(TeamColour teamColour) => teamColour == TeamColour.Red ? Color4Extensions.FromHex("#D43030") : Color4Extensions.FromHex("#2A82E4");
        }

        private void updatePosition()
        {
            this.MoveTo(expanded ? new Vector2(0, -25) : Vector2.Zero, 300, Easing.Out);
        }

        protected override void PostUpdate()
        {
            updateState();

            GetBeatmapInformation(out double bpm, out double length, out string srExtra, out var stats);

            (string, string)[] srAndModStats =
            {
                ("星级", $"{beatmap!.StarRating:0.00}{srExtra}")
            };

            string? modPosition = GetBeatmapModPosition();

            if (modPosition != null)
            {
                srAndModStats = srAndModStats.Append(("谱面位置", modPosition)).ToArray();
            }

            (string, string)[] bpmAndPickTeam =
            {
                ("选图方", pickTeamColour == null ? "无" : pickTeamColour.Value == TeamColour.Red ? "红队" : "蓝队"),
                ("BPM", $"{bpm:0.#}")
            };

            leftData.Children = new Drawable[]
            {
                new DiffPiece(bpmAndPickTeam)
                {
                    Origin = Anchor.CentreRight,
                    Anchor = Anchor.CentreRight,
                },
                new DiffPiece(("谱面长度", length.ToFormattedDuration().ToString()))
                {
                    Origin = Anchor.CentreRight,
                    Anchor = Anchor.CentreRight,
                },
            };

            rightData.Children = new Drawable[]
            {
                new DiffPiece(stats)
                {
                    Origin = Anchor.CentreLeft,
                    Anchor = Anchor.CentreLeft,
                },
                new DiffPiece(srAndModStats)
                {
                    Origin = Anchor.CentreLeft,
                    Anchor = Anchor.CentreLeft,
                }
            };

            beatmapPanel.Child = new TournamentBeatmapPanel(beatmap, isGameplaySongBar: true)
            {
                Width = 500,
                CenterText = true,
            };
        }
    }
}
