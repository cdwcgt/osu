// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Game.Online.Chat;

namespace osu.Game.Tournament.IPC.MemoryIPC
{
    public interface IProvideAdditionalData
    {
        public SlotPlayerStatus[] SlotPlayers { get; }

        public Bindable<Channel> TourneyChatChannel { get; }

        public BindableInt Team1Combo { get; }

        public BindableInt Team2Combo { get; }
    }
}
