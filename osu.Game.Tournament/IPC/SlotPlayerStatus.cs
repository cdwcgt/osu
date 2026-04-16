// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Game.Beatmaps.Legacy;

namespace osu.Game.Tournament.IPC
{
    public class SlotPlayerStatus
    {
        public bool SlotEquipped => OnlineID.Value != -1;

        public BindableInt OnlineID { get; } = new BindableInt(-1);

        public Bindable<LegacyMods> Mods { get; } = new Bindable<LegacyMods>();

        public BindableDouble Accuracy { get; } = new BindableDouble();
        public BindableInt Hit100 { get; } = new BindableInt();
        public BindableInt Hit300 { get; } = new BindableInt();
        public BindableInt Hit50 { get; } = new BindableInt();
        public BindableInt HitGeki { get; } = new BindableInt();
        public BindableInt HitKatu { get; } = new BindableInt();
        public BindableInt HitMiss { get; } = new BindableInt();
        public BindableInt Combo { get; } = new BindableInt();
        public BindableInt MaxCombo { get; } = new BindableInt();

        public BindableInt Score { get; } = new BindableInt();
    }
}
