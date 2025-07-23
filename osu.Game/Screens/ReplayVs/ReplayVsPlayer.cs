// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Scoring;
using osu.Game.Screens.OnlinePlay.Multiplayer.Spectate;
using osu.Game.Screens.Play;
using osu.Game.Screens.Ranking;

namespace osu.Game.Screens.ReplayVs
{
    public partial class ReplayVsPlayer : Player
    {
        private readonly SpectatorPlayerClock spectatorPlayerClock;
        private readonly Score score;
        private readonly ColourInfo teamColor;

        protected override bool CheckModsAllowFailure() => false;
        public IAggregateAudioAdjustment ClockAdjustmentsFromMods => clockAdjustmentsFromMods;

        private readonly AudioAdjustments clockAdjustmentsFromMods = new AudioAdjustments();

        public ReplayVsPlayer(Score score, SpectatorPlayerClock spectatorPlayerClock, ColourInfo teamColor)
            : base(new PlayerConfiguration { AllowUserInteraction = false })
        {
            this.spectatorPlayerClock = spectatorPlayerClock;
            this.score = score;
            this.teamColor = teamColor;
        }

        [BackgroundDependencyLoader]
        private void load(CancellationToken cancellationToken)
        {
            // HUD overlay may not be loaded if load has been cancelled early.
            if (cancellationToken.IsCancellationRequested)
                return;

            HUDOverlay.PlayerSettingsOverlay.Expire();
            HUDOverlay.HoldToQuit.Expire();

            AddInternal(new OsuSpriteText
            {
                Text = score.ScoreInfo.User.Username,
                Font = OsuFont.Default.With(size: 50),
                Colour = teamColor,
                Y = 100,
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
            });
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            spectatorPlayerClock.WaitingOnFrames = false;
        }

        protected override void Update()
        {
            // The player clock's running state is controlled externally, but the local pausing state needs to be updated to start/stop gameplay.
            if (GameplayClockContainer.IsRunning)
                GameplayClockContainer.Start();
            else
                GameplayClockContainer.Stop();

            base.Update();
        }

        protected override void PrepareReplay()
        {
            DrawableRuleset?.SetReplayScore(score);
        }

        protected override Score CreateScore(IBeatmap beatmap) => score;

        protected override ResultsScreen CreateResults(ScoreInfo score) => new SoloResultsScreen(score)
        {
            AllowRetry = true,
            IsLocalPlay = true,
        };

        protected override GameplayClockContainer CreateGameplayClockContainer(WorkingBeatmap beatmap, double gameplayStart)
        {
            var gameplayClockContainer = new GameplayClockContainer(spectatorPlayerClock, applyOffsets: false, requireDecoupling: false);
            clockAdjustmentsFromMods.BindAdjustments(gameplayClockContainer.AdjustmentsFromMods);
            return gameplayClockContainer;
        }
    }
}
