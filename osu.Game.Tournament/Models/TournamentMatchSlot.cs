// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Text.Json.Serialization;
using osu.Framework.Bindables;

namespace osu.Game.Tournament.Models
{
    [Serializable]
    public class TournamentMatchSlot
    {
        [JsonIgnore]
        public readonly Bindable<TournamentTeam?> Team = new Bindable<TournamentTeam?>();

        public string? TeamAcronym;

        public readonly Bindable<int?> Score = new Bindable<int?>();

        public readonly Bindable<TeamColour> Colour = new Bindable<TeamColour>();

        public TournamentMatchSlot()
        {
            Team.BindValueChanged(t => TeamAcronym = t.NewValue?.Acronym.Value, true);
        }

        public TournamentMatchSlot(TournamentTeam? team, TeamColour colour)
            : this()
        {
            Team.Value = team;
            Colour.Value = colour;
        }
    }
}
