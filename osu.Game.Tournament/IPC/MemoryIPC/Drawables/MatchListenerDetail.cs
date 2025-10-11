// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;

namespace osu.Game.Tournament.IPC.MemoryIPC.Drawables
{
    public partial class MatchListenerDetail : CompositeDrawable
    {
        private readonly OsuSpriteText currentListeningText;
        private readonly OsuSpriteText currentlyPlayingText;
        private readonly OsuSpriteText latestMatchEventIDText;
        private readonly OsuSpriteText abortedText;

        [Resolved]
        private MatchIPCInfo matchInfo { get; set; } = null!;

        private MemoryBasedIPCWithMatchListener? ipc => matchInfo as MemoryBasedIPCWithMatchListener;

        public MatchListenerDetail()
        {
            InternalChild = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Y,
                RelativeSizeAxes = Axes.X,
                Direction = FillDirection.Vertical,
                Children = new Drawable[]
                {
                    currentListeningText = createText(),
                    currentlyPlayingText = createText(),
                    latestMatchEventIDText = createText(),
                    abortedText = createText(),
                }
            };
        }

        private static OsuSpriteText createText() => new TournamentSpriteText
        {
            Font = OsuFont.Torus.With(size: 10f)
        };

        protected override void Update()
        {
            base.Update();

            if (ipc == null)
                return;

            currentListeningText.Text = $"监听状态: {(ipc.CurrentlyListening.Value ? "监听中" : "未监听")}";
            currentlyPlayingText.Text = $"游玩状态: {(ipc.CurrentlyListening.Value ? "游玩中" : "未游玩")}";
            latestMatchEventIDText.Text = $"最后一个EventID: {ipc.LatestMatchEventID}";
            abortedText.Text = $"Aborted: {ipc.Aborted}";
        }
    }
}
