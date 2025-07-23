// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Screens.Play.HUD;
using osu.Game.Tournament.Components;
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
        private RoundInfo roundInfo { get; set; } = null!;

        public readonly BindableBool ShowSuccess = new BindableBool();

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
            Team1Score.BindTo(roundInfo.Score1);
            Team2Score.BindTo(roundInfo.Score2);

            ShowSuccess.BindValueChanged(_ => updateColor());

            if (ipc is IProvideAdditionalData)
            {
                playersPerTeam.BindTo(ladderInfo.PlayersPerTeam);
                return;
            }
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
            if (!ShowSuccess.Value)
            {
                Score1Text.Background.FadeOut(50);
                Score2Text.Background.FadeOut(50);
                ScoreDiffText.Background.FadeOut(50);
                ScoreDiffText.SuccessIcon.FadeOut(50);
                var color = invertTextColor ? black : Colour4.White;
                Score1Text.DrawableCount.Colour = Score2Text.DrawableCount.Colour = ScoreDiffText.DrawableCount.Colour = color;
                return;
            }

            Score1Text.Background.FadeIn(50);
            Score2Text.Background.FadeIn(50);
            ScoreDiffText.Background.FadeIn(50);
            ScoreDiffText.SuccessIcon.FadeIn(50);

            Score1Text.DrawableCount.Colour = Score2Text.DrawableCount.Colour = ScoreDiffText.DrawableCount.Colour = Color4Extensions.FromHex("383838");

            // 不是哥们，太蠢了吧
            Scheduler.AddDelayed(() => ShowSuccess.Value = false, 10000);
        }
    }
}
