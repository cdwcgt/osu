// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Utils;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.IO;
using osu.Game.Tournament.IPC;
using osu.Game.Tournament.IPC.MemoryIPC;
using osuTK;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class PlayerWindow : CapturedWindowSprite
    {
        private readonly int clientIndex;
        private SlotPlayerStatus player = null!;

        [Resolved]
        private AudioManager audioManager { get; set; } = null!;

        [Resolved]
        private TournamentStorage storage { get; set; } = null!;

        public PlayerWindow(int clientIndex)
            : base($"{TournamentGame.TOURNAMENT_CLIENT_NAME}{clientIndex}")
        {
            this.clientIndex = clientIndex;
        }

        [BackgroundDependencyLoader]
        private void load(MatchIPCInfo matchIpc)
        {
            player = ((MemoryBasedIPCWithMatchListener)matchIpc).SlotPlayers[clientIndex];

            playerCombo.BindValueChanged(comboChanged);
            playerCombo.BindTo(player.Combo);
            initSamples();
        }

        #region 红猪起飞

        public void FlyingLaunch(bool clockWise)
        {
            this.ScaleTo(0.9f, 1200, Easing.InQuint)
                .RotateTo(180f * (clockWise ? 1 : -1), 1200, Easing.InQuint)
                .MoveToOffset(new Vector2(0, -50), 1200, Easing.InQuint);

            this.Delay(1200f)
                .RotateTo(720f * (clockWise ? 1 : -1), 5000, Easing.OutQuint)
                .MoveToOffset(new Vector2(0, -DrawHeight * 4), 5000, Easing.OutQuint);
        }

        public void Reset()
        {
            this.ScaleTo(1, 5000, Easing.InOutSine);
            this.MoveTo(new Vector2(0, 0), 5000, Easing.InOutSine);
            this.RotateTo(0, 5000, Easing.InOutSine);
        }

        #endregion

        #region 欢乐无限

        private readonly BindableInt playerCombo = new BindableInt();
        private readonly List<ISample> samples = new List<ISample>();

        private void initSamples()
        {
            var samplePaths = storage.GetDirectories("Funny");

            foreach (string? path in samplePaths)
            {
                var sample = audioManager.Samples.Get(path);

                samples.Add(sample);
            }

            Scheduler.Add(() =>
            {
                foreach (var sample in samples.ToArray())
                {
                    if (sample.Length == 0)
                    {
                        samples.Remove(sample);
                    }
                }
            });
        }

        private void comboChanged(ValueChangedEvent<int> combo)
        {
            if (combo.NewValue > combo.OldValue)
                return;

            if (combo.OldValue < 20)
                return;

            playRandomSample();
        }

        private void playRandomSample()
        {
            int randomIndex = RNG.Next(0, samples.Count);
            samples[randomIndex].Play();
        }

        #endregion
    }
}
