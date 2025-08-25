// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.IPC;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.Screens
{
    public abstract partial class BeatmapInfoScreen : TournamentMatchScreen
    {
        protected readonly SongBar SongBar;
        protected ControlPanel ControlPanel;

        private Sprite supporterSprite = null!;
        private Sprite logoSprite = null!;
        private Sprite redPig = null!;

        protected virtual bool ShowLogo => false;

        protected virtual SongBar CreateSongBar() => new SongBar
        {
            Anchor = Anchor.BottomRight,
            Origin = Anchor.BottomRight,
            Depth = float.MinValue,
        };

        protected BeatmapInfoScreen()
        {
            AddRangeInternal(
            [
                SongBar = CreateSongBar(),
                ControlPanel = new ControlPanel
                {
                    Children = new Drawable[]
                    {
                        new TournamentSpriteText
                        {
                            Text = "Set Mods"
                        },
                        new TourneyButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Text = "Reset",
                            Action = () => setMods(LegacyMods.None, string.Empty)
                        },
                        new TourneyButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Text = "Set FM",
                            Action = () => setMods(LegacyMods.FreeMod, "FM")
                        },
                        new TourneyButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Text = "Set HR",
                            Action = () => setMods(LegacyMods.HardRock, "HR")
                        },
                        new TourneyButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Text = "Set HD",
                            Action = () => setMods(LegacyMods.Hidden, "HD")
                        },
                        new TourneyButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Text = "Set DT",
                            Action = () => setMods(LegacyMods.DoubleTime, "DT")
                        },
                    }
                }
            ]);
        }

        [BackgroundDependencyLoader]
        private void load(MatchIPCInfo ipc, TextureStore store)
        {
            ipc.Beatmap.BindValueChanged(IpcBeatmapChanged, true);
            ipc.Mods.BindValueChanged(IpcModsChanged, true);

            if (ShowLogo)
            {
                AddRangeInternal([
                    supporterSprite = new Sprite
                    {
                        RelativeSizeAxes = Axes.Both,
                        Texture = store.Get("我们至高无上的金主大人的赞助商图片"),
                        FillMode = FillMode.Fit,
                        Depth = float.MinValue,
                    },
                    logoSprite = new Sprite
                    {
                        RelativeSizeAxes = Axes.Both,
                        Texture = store.Get("我们尊贵的比赛logo"),
                        FillMode = FillMode.Fit,
                        Alpha = 0,
                        Depth = float.MinValue,
                    },
                    redPig = new Sprite
                    {
                        Name = "red pig",
                        RelativeSizeAxes = Axes.Both,
                        Texture = store.Get("传奇红猪嗜灭警告"),
                        FillMode = FillMode.Fit,
                        Alpha = 0,
                        Depth = float.MinValue,
                    }
                ]);

                supporterSprite.FadeIn(200).Then(10000).FadeOut(200).Then(10000).Loop();
                logoSprite.FadeOut(200).Then(10000).FadeIn(200).Then(10000).Loop();
            }

            banPicks.BindCollectionChanged((_, _) => updateDisplay());
        }

        private void setMods(LegacyMods mods, string acronym)
        {
            SongBar.Mods = mods;
            SetModAcronym(acronym);
        }

        private readonly BindableList<BeatmapChoice> banPicks = new BindableList<BeatmapChoice>();

        protected override void CurrentMatchChanged(ValueChangedEvent<TournamentMatch?> match)
        {
            base.CurrentMatchChanged(match);

            if (!ShowLogo)
                return;

            if (match.OldValue != null)
            {
                banPicks.UnbindFrom(match.OldValue.PicksBans);
            }

            if (match.NewValue != null)
            {
                banPicks.BindTo(match.NewValue.PicksBans);
            }
        }

        private void updateDisplay() => Scheduler.AddOnce(() =>
        {
            if (!ShowLogo)
                return;

            int beatOf = CurrentMatch.Value?.Round.Value?.BestOf.Value ?? -1;

            if (beatOf == -1)
            {
                hideRedPig();
                return;
            }

            if (banPicks.Count(p => p.Type == ChoiceType.Pick) > (beatOf - 1) / 2)
            {
                redPig.FadeIn(100);
            }
            else
            {
                redPig.FadeOut(100);
            }

            void hideRedPig() => redPig.FadeOut(100);
        });

        protected virtual void SetModAcronym(string acronym) { }

        protected virtual void IpcModsChanged(ValueChangedEvent<LegacyMods> mods)
        {
            SongBar.Mods = mods.NewValue;
        }

        protected virtual void IpcBeatmapChanged(ValueChangedEvent<TournamentBeatmap?> beatmap)
        {
            SongBar.FadeInFromZero(300, Easing.OutQuint);
            SongBar.Beatmap = beatmap.NewValue;
        }
    }
}
