// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Game.Tournament.IPC.MemoryIPC
{
    public class TourneyChatItem
    {
        public DateTimeOffset Timestamp { get; set; }
        public string Sender { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
