﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
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
                            Action = () => setMods(LegacyMods.None, "FM")
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
        private void load(MatchIPCInfo ipc)
        {
            ipc.Beatmap.BindValueChanged(IpcBeatmapChanged, true);
            ipc.Mods.BindValueChanged(IpcModsChanged, true);
        }

        private void setMods(LegacyMods mods, string acronym)
        {
            SongBar.Mods = mods;
            SetModAcronym(acronym);
        }

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
