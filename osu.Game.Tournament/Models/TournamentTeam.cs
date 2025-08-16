﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using Newtonsoft.Json;
using osu.Framework.Bindables;

namespace osu.Game.Tournament.Models
{
    /// <summary>
    /// A team representation. For official tournaments this is generally a country.
    /// </summary>
    [Serializable]
    public class TournamentTeam
    {
        /// <summary>
        /// The name of this team.
        /// </summary>
        public Bindable<string> FullName = new Bindable<string>(string.Empty);

        /// <summary>
        /// Name of the file containing the flag.
        /// </summary>
        public Bindable<string> FlagName = new Bindable<string>(string.Empty);

        /// <summary>
        /// Short acronym which appears in the group boxes post-selection.
        /// </summary>
        public Bindable<string> Acronym = new Bindable<string>(string.Empty);

        public BindableList<SeedingResult> SeedingResults = new BindableList<SeedingResult>();

        public double AverageRank
        {
            get
            {
                int[] ranks = Players.Select(p => p.Rank)
                                     .Where(i => i.HasValue)
                                     .Select(i => i!.Value)
                                     .ToArray();

                if (ranks.Length == 0)
                    return 0;

                return ranks.Average();
            }
        }

        public Bindable<string> Seed = new Bindable<string>(string.Empty);

        public Bindable<int> LastYearPlacing = new BindableInt
        {
            MinValue = 0,
            MaxValue = 256
        };

        public BindableColour4 Color = new BindableColour4(TournamentGame.ELEMENT_BACKGROUND_COLOUR);
        public BindableColour4 IdTextColor = new BindableColour4(TournamentGame.ELEMENT_FOREGROUND_COLOUR);
        public BindableColour4 NameBackgroundColor = new BindableColour4(TournamentGame.ELEMENT_BACKGROUND_COLOUR);
        public BindableColour4 NameTextColor = new BindableColour4(TournamentGame.ELEMENT_FOREGROUND_COLOUR);

        [JsonProperty]
        public BindableList<TournamentUser> Players { get; } = new BindableList<TournamentUser>();

        public TournamentTeam()
        {
            FullName.ValueChanged += val =>
            {
                // use a sane acronym based on full name.
                if (val.OldValue.StartsWith(Acronym.Value, StringComparison.InvariantCultureIgnoreCase))
                    Acronym.Value = val.NewValue?.Length >= 3 ? val.NewValue.Substring(0, 3).ToUpperInvariant() : string.Empty;
            };
        }

        public override string ToString() => FullName.Value ?? Acronym.Value;
    }
}
