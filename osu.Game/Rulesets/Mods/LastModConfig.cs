// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Newtonsoft.Json;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Game.Online.API;
using Realms;

namespace osu.Game.Rulesets.Mods
{
    public class LastModConfig : RealmObject
    {
        [PrimaryKey]
        public Guid ID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The ruleset that the preset is valid for.
        /// </summary>
        public RulesetInfo Ruleset { get; set; } = null!;

        public string ModAcronym { get; set; } = string.Empty;

        /// <summary>
        /// The set of configured mods that are part of the preset.
        /// </summary>
        [Ignored]
        public Mod? Mod
        {
            get
            {
                if (string.IsNullOrEmpty(ModsJson))
                    return null;

                var apiMods = JsonConvert.DeserializeObject<APIMod>(ModsJson);
                var ruleset = Ruleset.CreateInstance();
                return apiMods.AsNonNull().ToMod(ruleset);
            }
            set
            {
                if (value == null)
                {
                    ModsJson = string.Empty;
                    return;
                }

                var apiMods = new APIMod(value);
                ModsJson = JsonConvert.SerializeObject(apiMods);
            }
        }

        /// <summary>
        /// The set of configured mods that are part of the preset, serialised as a JSON blob.
        /// </summary>
        [MapTo("Mods")]
        public string ModsJson { get; set; } = string.Empty;
    }
}
