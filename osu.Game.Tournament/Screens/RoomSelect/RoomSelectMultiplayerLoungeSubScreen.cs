// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Game.Online.Rooms;
using osu.Game.Screens.OnlinePlay.Multiplayer;
using osu.Game.Tournament.IPC;

namespace osu.Game.Tournament.Screens.RoomSelect
{
    public partial class RoomSelectMultiplayerLoungeSubScreen : MultiplayerLoungeSubScreen
    {
        [Resolved]
        private LazerRoomMatchInfo lazerInfo { get; set; } = null!;

        protected override void JoinInternal(Room room, string? password, Action<Room> onSuccess, Action<string, Exception?> onFailure)
        {
            lazerInfo.Join(room, password, onSuccess, onFailure);
        }
    }
}
