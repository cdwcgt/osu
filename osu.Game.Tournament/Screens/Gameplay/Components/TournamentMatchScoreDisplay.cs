// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Screens.Play.HUD;
using osu.Game.Tournament.IPC;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class TournamentMatchScoreDisplay : MatchScoreDisplay
    {
        private bool invertTextColor;
        private readonly Colour4 black = Colour4.FromHex("1f1f1f");

        public bool InvertTextColor
        {
            get => invertTextColor;
            set
            {
                invertTextColor = value;
                updateColor();
            }
        }

        [BackgroundDependencyLoader]
        private void load(MatchIPCInfo ipc)
        {
            Team1Score.BindTo(ipc.Score1);
            Team2Score.BindTo(ipc.Score2);
        }

        private void updateColor()
        {
            var color = invertTextColor ? black : Colour4.White;
            Score1Text.Colour = Score2Text.Colour = color;
        }
    }
}
