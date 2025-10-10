// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Screens.Play.HUD;
using osu.Game.Tournament.IPC;
using osu.Game.Tournament.IPC.MemoryIPC;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class TournamentMatchScoreDisplay : MatchScoreDisplay
    {
        // private bool invertTextColor;
        private ComboCounter team1MaxCombo;
        private ComboCounter team2MaxCombo;

        [Resolved]
        private MatchIPCInfo ipc { get; set; } = null!;

        // public bool InvertTextColor
        // {
        //     get => invertTextColor;
        //     set
        //     {
        //         invertTextColor = value;
        //         updateColor();
        //     }
        // }

        [BackgroundDependencyLoader]
        private void load()
        {
            Team1Score.BindTo(ipc.Score1);
            Team2Score.BindTo(ipc.Score2);

            if (ipc is not IProvideAdditionalData additionalData)
                return;

            team1MaxCombo = new TournamentComboCounter();
            team2MaxCombo = new TournamentComboCounter();

            Score1Text.CustomContent.Anchor = Anchor.BottomLeft;
            Score1Text.CustomContent.Origin = Anchor.BottomRight;
            Score1Text.CustomContent.Child = team1MaxCombo;

            Score2Text.CustomContent.Anchor = Anchor.BottomRight;
            Score2Text.CustomContent.Origin = Anchor.BottomLeft;
            Score2Text.CustomContent.Child = team2MaxCombo;

            team1MaxCombo.Current.BindTo(additionalData.Team1Combo);
            team2MaxCombo.Current.BindTo(additionalData.Team2Combo);
        }

        // private void updateColor()
        // {
        //     Score1Text.DrawableCount.Colour = Score2Text.DrawableCount.Colour = ScoreDiffText.DrawableCount.Colour = Color4Extensions.FromHex("383838");
        // }

        private partial class TournamentComboCounter : ComboCounter
        {
            protected override double RollingDuration => 1000;
            protected override Easing RollingEasing => Easing.Out;

            protected override OsuSpriteText CreateSpriteText()
                => base.CreateSpriteText().With(s => s.Font = OsuFont.Torus.With(size: 20f));

            protected override LocalisableString FormatCount(int count)
            {
                return $@"{count}x";
            }
        }
    }
}
