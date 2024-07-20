// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
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

        protected Bindable<LegacyMods> Mods = new Bindable<LegacyMods>();
        protected BindableBool ManualModsSelect = new BindableBool();

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
                            Action = reset
                        },
                        new TourneyButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Text = "Set FM",
                            Action = () => setMods(LegacyMods.None)
                        },
                        new TourneyButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Text = "Set HR",
                            Action = () => setMods(LegacyMods.HardRock)
                        },
                        new TourneyButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Text = "Set HD",
                            Action = () => setMods(LegacyMods.Hidden)
                        },
                        new TourneyButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Text = "Set DT",
                            Action = () => setMods(LegacyMods.DoubleTime)
                        },
                    }
                }
            ]);

            Mods.BindValueChanged(modsChanged);
        }

        [Resolved]
        private MatchIPCInfo ipc { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            ipc.Beatmap.BindValueChanged(beatmapChanged, true);
            ipc.Mods.BindValueChanged(m =>
            {
                ManualModsSelect.Value = false;
                Mods.Value = m.NewValue;
            });
        }

        private void setMods(LegacyMods mods)
        {
            ManualModsSelect.Value = true;
            Mods.Value = mods;
        }

        private void reset()
        {
            ManualModsSelect.Value = false;
            Mods.Value = ipc.Mods.Value;
        }

        private void modsChanged(ValueChangedEvent<LegacyMods> mods)
        {
            SongBar.Mods = mods.NewValue;
        }

        private void beatmapChanged(ValueChangedEvent<TournamentBeatmap?> beatmap)
        {
            SongBar.FadeInFromZero(300, Easing.OutQuint);
            SongBar.Beatmap = beatmap.NewValue;
        }
    }
}
