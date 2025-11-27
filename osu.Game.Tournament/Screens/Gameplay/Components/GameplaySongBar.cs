// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Threading;
using osu.Framework.Utils;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Models;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class GameplaySongBar : SongBar
    {
        private readonly Bindable<TournamentMatch?> currentMatch = new Bindable<TournamentMatch?>();

        private TeamColour? pickTeamColour;
        private bool isDefault;
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

            Expanded = true;
        }

        private void matchChanged(ValueChangedEvent<TournamentMatch?> match)
        {
            if (match.OldValue != null)
                match.OldValue.PicksBans.CollectionChanged -= picksBansOnCollectionChanged;
            if (match.NewValue != null)
                match.NewValue.PicksBans.CollectionChanged += picksBansOnCollectionChanged;

            Scheduler.AddOnce(UpdateState);
        }

        private void picksBansOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            => Scheduler.AddOnce(UpdateState);

        protected override void UpdateState()
        {
            var choices = currentMatch.Value?.PicksBans.Where(p => p.BeatmapID == Beatmap?.OnlineID);
            pickTeamColour = choices?.FirstOrDefault(p => p.Type == ChoiceType.Pick)?.Team;
            isDefault = choices?.FirstOrDefault(p => p.Type == ChoiceType.Default) != null;

            if (isDefault)
            {
                rainbowArrow();
            }
            else
            {
                arrowRainbowSchedule?.Cancel();
            }

            if (pickTeamColour == null)
            {
                ArrowColor.Value = Color4.White;
                return;
            }

            ArrowColor.Value = getTeamColour(pickTeamColour.Value);
            Scheduler.AddOnce(PostUpdate);
            static ColourInfo getTeamColour(TeamColour teamColour) => teamColour == TeamColour.Red ? Color4Extensions.FromHex("#D43030") : Color4Extensions.FromHex("#2A82E4");
        }

        private ScheduledDelegate? arrowRainbowSchedule;

        private void rainbowArrow()
        {
            arrowRainbowSchedule?.Cancel();

            this.TransformBindableTo(ArrowColor, ColourInfo.GradientHorizontal(getRandomColour(), getRandomColour()), 1000);

            arrowRainbowSchedule = Scheduler.AddDelayed(() =>
            {
                this.TransformBindableTo(ArrowColor, ColourInfo.GradientHorizontal(getRandomColour(), getRandomColour()), 1000);
            }, 1000, true);

            Color4 getRandomColour()
            {
                return Color4Extensions.FromHSV(RNG.NextSingle(0, 360) % 360, 1, 1);
            }
        }

        private void updatePosition()
        {
            this.MoveTo(expanded ? new Vector2(0, -25) : Vector2.Zero, 300, Easing.Out);
        }

        protected override Drawable[][] CreateLeftData()
        {
            var leftData = base.CreateLeftData();

            GetBeatmapInformation(Mods, out double bpm, out double _, out _, out _);

            string picker;

            if (isDefault)
                picker = "自动";
            else
            {
                picker = pickTeamColour == null ? "第三方" : pickTeamColour.Value == TeamColour.Red ? "红队" : "蓝队";
            }

            (string, string)[] bpmAndPickTeam =
            {
                ("选图方", picker),
                ("BPM", $"{bpm:0.#}")
            };

            leftData[0][0] = new DiffPiece(bpmAndPickTeam)
            {
                Origin = Anchor.CentreRight,
                Anchor = Anchor.CentreRight,
            };

            return leftData;
        }
    }
}
