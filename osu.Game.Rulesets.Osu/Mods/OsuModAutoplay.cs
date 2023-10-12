// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Replays;
using osu.Game.Rulesets.Osu.Replays.Danse;

namespace osu.Game.Rulesets.Osu.Mods
{
    public class OsuModAutoplay : ModAutoplay
    {
        public override Type[] IncompatibleMods => base.IncompatibleMods.Concat(new[] { typeof(OsuModMagnetised), typeof(OsuModRepel), typeof(OsuModAutopilot), typeof(OsuModSpunOut), typeof(OsuModAlternate), typeof(OsuModSingleTap) }).ToArray();

        [SettingSource("Cursor Dance", "Enable cursor dance")]
        public Bindable<bool> CursorDance { get; } = new BindableBool();

        public override ModReplayData CreateReplayData(IBeatmap beatmap, IReadOnlyList<Mod> mods)
            => new ModReplayData(CursorDance.Value ? new OsuDanceGenerator(beatmap, mods).Generate() : new OsuAutoGenerator(beatmap, mods).Generate(),
                new ModCreatedUser { Username = CursorDance.Value ? "danser" : "Autoplay" });
    }
}
