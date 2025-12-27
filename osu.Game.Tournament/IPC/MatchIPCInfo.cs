// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.IPC
{
    public partial class MatchIPCInfo : CompositeComponent
    {
        public Bindable<TournamentBeatmap?> Beatmap { get; } = new Bindable<TournamentBeatmap?>();
        public Bindable<LegacyMods> Mods { get; } = new Bindable<LegacyMods>();
        public Bindable<TourneyState> State { get; } = new Bindable<TourneyState>();
        public Bindable<int> ChatChannel { get; } = new Bindable<int>();
        public BindableLong Score1 { get; } = new BindableLong();
        public BindableLong Score2 { get; } = new BindableLong();

        public BindableInt Team1Combo { get; } = new BindableInt();
        public BindableInt Team2Combo { get; } = new BindableInt();

        public virtual bool ReadScoreFromFile => true;
    }
}
