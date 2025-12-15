// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Tournament.IPC;
using osu.Game.Tournament.IPC.MemoryIPC;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.Screens
{
    public abstract partial class TournamentScreen : CompositeDrawable
    {
        public const double FADE_DELAY = 200;

        protected virtual bool FetchDataFromMemoryThisScreen => false;

        [Resolved]
        protected LadderInfo LadderInfo { get; private set; } = null!;

        [Resolved]
        protected MatchIPCInfo IPC { get; private set; } = null!;

        private MemoryBasedIPC? memoryIpc;

        protected TournamentScreen()
        {
            RelativeSizeAxes = Axes.Both;

            FillMode = FillMode.Fit;
            FillAspectRatio = 16 / 9f;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (IPC is MemoryBasedIPC memoryBasedIPC)
            {
                memoryIpc = memoryBasedIPC;
            }
        }

        public override void Hide() => this.FadeOut(FADE_DELAY);

        public override void Show()
        {
            if (OperatingSystem.IsWindows() && memoryIpc != null)
                memoryIpc.FetchDataFromMemory = FetchDataFromMemoryThisScreen;

            this.FadeIn(FADE_DELAY);
        }
    }
}
