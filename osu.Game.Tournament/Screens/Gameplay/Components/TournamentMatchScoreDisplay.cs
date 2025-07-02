// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Screens.Play.HUD;
using osu.Game.Tournament.IPC;
using osu.Game.Tournament.IPC.MemoryIPC;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class TournamentMatchScoreDisplay : MatchScoreDisplay
    {
        private bool invertTextColor;
        private readonly Colour4 black = Colour4.FromHex("1f1f1f");

        [Resolved]
        private LadderInfo ladderInfo { get; set; } = null!;

        [Resolved]
        private MatchIPCInfo ipc { get; set; } = null!;

        private readonly BindableInt playersPerTeam = new BindableInt
        {
            MinValue = 1,
            MaxValue = 4,
        };

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
        private void load()
        {
            if (ipc is IProvideAdditionalData)
            {
                playersPerTeam.BindTo(ladderInfo.PlayersPerTeam);
                return;
            }

            Team1Score.BindTo(ipc.Score1);
            Team2Score.BindTo(ipc.Score2);
        }

        protected override void Update()
        {
            base.Update();

            if (ipc is not IProvideAdditionalData memoryIPC)
                return;

            int team1Score = 0;

            for (int i = 0; i < playersPerTeam.Value; i++)
            {
                var player = memoryIPC.SlotPlayers[i];
                team1Score += (int)(player.Score.Value * customModMultiplier(player.Mods.Value));
            }

            int team2Score = 0;

            for (int i = playersPerTeam.Value; i < playersPerTeam.Value * 2; i++)
            {
                var player = memoryIPC.SlotPlayers[i];
                team2Score += (int)(player.Score.Value * customModMultiplier(player.Mods.Value));
            }

            Team1Score.Value = team1Score;
            Team2Score.Value = team2Score;
        }

        private double customModMultiplier(LegacyMods mods)
        {
            double multiplier = 1;

            if (mods.HasFlag(LegacyMods.Easy))
            {
                multiplier *= 1.8;
            }

            return multiplier;
        }

        private void updateColor()
        {
            var color = invertTextColor ? black : Colour4.White;
            Score1Text.Colour = Score2Text.Colour = ScoreDiffText.Colour = color;
        }
    }
}
