// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Scoring;
using osu.Game.Screens.Backgrounds;
using osu.Game.Screens.Play;

namespace osu.Game.Screens.MapGuess
{
    public partial class MapGuessPlayer : ReplayPlayer
    {
        private readonly MapGuessConfig config;
        private readonly double startTime;

        public BindableBool Paused { get; private set; } = new BindableBool();

        public MapGuessPlayer(Score score, double startTime, MapGuessConfig config)
            : base(score, new PlayerConfiguration
            {
                AllowUserInteraction = false,
                AllowFailAnimation = false,
                ShowResults = false,
                AutomaticallySkipIntro = true
            })
        {
            this.startTime = startTime;
            this.config = config;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            HUDOverlay.ShowHud.Value = false;
            HUDOverlay.ShowHud.Disabled = true;
            HUDOverlay.PlayfieldSkinLayer.Hide();
            BreakOverlay.Hide();
            DrawableRuleset.Overlays.Hide();
            DrawableRuleset.Playfield.DisplayJudgements.Value = false;

            if (!config.Music.Value)
                Beatmap.Value.Track.Volume.Value = 0;

            if (!config.ShowHitobjects.Value)
                DrawableRuleset.Hide();

            Reset();
            Schedule(() =>
            {
                bool showBackground = config.ShowBackground.Value;
                ApplyToBackground(b =>
                {
                    b.IgnoreUserSettings.Value = true;

                    if (!showBackground)
                        b.Hide();
                    b.DimWhenUserSettingsIgnored.Value = showBackground ? (config.ShowHitobjects.Value ? 0.7f : 0) : 1;
                    b.BlurAmount.Value = config.BackgroundBlur.Value * BackgroundScreenBeatmap.USER_BLUR_FACTOR;
                });
            });
        }

        protected override void Update()
        {
            base.Update();

            if (GameplayClockContainer.CurrentTime >= startTime + config.PreviewLength.Value && !GameplayClockContainer.IsPaused.Value)
            {
                GameplayClockContainer.Stop();
                Paused.Value = true;
                this.FadeOut(500, Easing.OutQuart);
            }
        }

        public void Reset()
        {
            GameplayClockContainer.Stop();
            SetGameplayStartTime(startTime);
            GameplayClockContainer.Start();
            Paused.Value = false;
            this.FadeIn(200, Easing.In);
        }

        public void ShowBackground(bool blur)
        {
            ApplyToBackground(b =>
            {
                b.Show();
                b.DimWhenUserSettingsIgnored.Value = config.ShowHitobjects.Value ? 0.7f : 0;
                b.BlurAmount.Value = blur ? 0.2f * BackgroundScreenBeatmap.USER_BLUR_FACTOR : 0;
            });
        }
    }
}
