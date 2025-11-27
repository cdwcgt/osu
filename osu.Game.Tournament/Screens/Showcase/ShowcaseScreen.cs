// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Tournament.Components;
using osu.Framework.Platform;
using osu.Framework.Timing;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Configuration;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens.Menu;
using osu.Game.Tournament.Models;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Showcase
{
    [Cached]
    public partial class ShowcaseScreen : BeatmapInfoScreen, IBeatSyncProvider
    {
        [Resolved]
        private GameHost host { get; set; } = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private LadderInfo ladder { get; set; } = null!;

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        private NestedOsuGame? nestedGame;
        private ShowCaseSideFlash? flash;

        private readonly BindableFloat flashIntensity = new BindableFloat(2)
        {
            MinValue = 0,
            MaxValue = 8,
        };

        private readonly BindableBool flashKiaiOnly = new BindableBool(true);
        private readonly Bindable<string> flashColorString = new Bindable<string>("#FFFFFF");
        private readonly Bindable<Color4> flashColor = new Bindable<Color4>(Color4.White);

        private FillFlowContainer nestedLazerSettingContainer = null!;

        private MusicController? musicController;
        private readonly Bindable<IReadOnlyList<Mod>> mods = new Bindable<IReadOnlyList<Mod>>();
        private readonly Bindable<LegacyMods> legacyMods = new Bindable<LegacyMods>();
        private Container showcaseContainer = null!;

        private readonly BindableBool useOsuLazer = new BindableBool();

        [BackgroundDependencyLoader]
        private void load()
        {
            AddRangeInternal(new Drawable[]
            {
                new TournamentLogo(),
                new TourneyVideo("showcase")
                {
                    Loop = true,
                    RelativeSizeAxes = Axes.Both,
                },
                showcaseContainer = new Container
                {
                    Padding = new MarginPadding { Bottom = SongBar.HEIGHT + 14f },
                    RelativeSizeAxes = Axes.Both,
                },
            });

            flashColor.BindTo(ladder.ShowcaseSettings.FlashColor);
            flashIntensity.BindTo(ladder.ShowcaseSettings.FlashIntensity);
            flashKiaiOnly.BindTo(ladder.ShowcaseSettings.FlashKiaiOnly);
            useOsuLazer.BindTo(ladder.ShowcaseSettings.UseLazer);

            mods.BindValueChanged(m =>
            {
                var ruleset = ladder.Ruleset.Value?.CreateInstance();
                legacyMods.Value = ruleset!.ConvertToLegacyMods(m.NewValue.ToArray());
            });

            legacyMods.BindValueChanged(m =>
            {
                SongBar.Mods = m.NewValue;
            });

            ControlPanel.Add(new SettingsCheckbox
            {
                LabelText = "Use osu!lazer",
                Current = { BindTarget = useOsuLazer },
            });

            flashColorString.Value = flashColor.Value.ToHex();

            ControlPanel.Add(nestedLazerSettingContainer = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.None,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 5f),
                Masking = true,
                Children = new Drawable[]
                {
                    new SettingsSlider<float>
                    {
                        LabelText = "闪光强度",
                        Current = { BindTarget = flashIntensity },
                        KeyboardStep = 0.5f,
                    },
                    new SettingsCheckbox
                    {
                        LabelText = "仅kiai闪光",
                        Current = { BindTarget = flashKiaiOnly }
                    },
                    new SettingsTextBox
                    {
                        LabelText = "闪光颜色",
                        Current = { BindTarget = flashColorString }
                    }
                }
            });

            flashColorString.BindValueChanged(c =>
            {
                if (Colour4.TryParseHex(c.NewValue, out var colour))
                {
                    flashColor.Value = colour;
                }
            });

            useOsuLazer.BindValueChanged(u =>
            {
                nestedLazerSettingContainer.ClearTransforms();
                nestedLazerSettingContainer.AutoSizeDuration = 400;
                nestedLazerSettingContainer.AutoSizeEasing = Easing.OutQuint;

                if (u.NewValue)
                {
                    nestedLazerSettingContainer.AutoSizeAxes = Axes.Y;
                    showcaseContainer.Clear(true);
                    startInnerLazer();
                }
                else
                {
                    nestedLazerSettingContainer.ResizeHeightTo(0, 400, Easing.OutQuint);
                    nestedLazerSettingContainer.AutoSizeAxes = Axes.None;
                    closeInnerLazer();
                    showcaseContainer.Clear();
                    showcaseContainer.Add(new Box
                    {
                        // chroma key area for stable gameplay
                        Name = "chroma",
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(0, 255, 0, 255),
                    });
                }
            }, true);
        }

        public override void Show()
        {
            base.Show();

            if (useOsuLazer.Value)
                startInnerLazer();
        }

        private void startInnerLazer() => Scheduler.AddOnce(() =>
        {
            if (nestedGame != null)
                return;

            nestedGame = new NestedOsuGame(host.Storage, new ForwardingAPIAccess(api), config)
            {
                Masking = true
            };
            nestedGame.SetHost(host);

            host.Window.CursorState = CursorState.Default;

            showcaseContainer.Add(nestedGame);

            nestedGame.OnLoadComplete += _ =>
            {
                musicController = nestedGame.Dependencies.Get<MusicController>();

                musicController.TrackChanged += beatmapChanged;

                mods.BindTarget = nestedGame.Dependencies.Get<Bindable<IReadOnlyList<Mod>>>();
            };

            AddInternal(flash = new ShowCaseSideFlash());
            flash.FlashIntensity.BindTo(flashIntensity);
            flash.FlashColor.BindTo(flashColor);
            flash.FlashKiaiOnly.BindTo(flashKiaiOnly);
        });

        private void closeInnerLazer()
        {
            if (nestedGame != null)
                showcaseContainer.Remove(nestedGame, true);

            if (flash != null)
                RemoveInternal(flash, true);

            musicController = null;
            nestedGame = null;
            flash = null;
        }

        private GetBeatmapRequest? beatmapLookupRequest;

        private void beatmapChanged(WorkingBeatmap workingBeatmap, TrackChangeDirection _)
        {
            beatmapLookupRequest?.Cancel();

            int beatmapId = workingBeatmap.BeatmapInfo.OnlineID;

            var existing = ladder.CurrentMatch.Value?.Round.Value?.Beatmaps.FirstOrDefault(b => b.ID == beatmapId);

            if (existing != null)
            {
                SongBar.Beatmap = existing.Beatmap;

                switch (existing.MapType)
                {
                    case MapType.Starter:
                        SongBar.SongBarColour.Value = Color4Extensions.FromHex("#F5BB17");
                        break;

                    case MapType.Counter:
                        SongBar.SongBarColour.Value = Color4Extensions.FromHex("#25356E");
                        break;
                }
            }
            else
            {
                SongBar.SongBarColour.Value = null;

                beatmapLookupRequest = new GetBeatmapRequest(new APIBeatmap { OnlineID = workingBeatmap.BeatmapInfo.OnlineID });

                beatmapLookupRequest.Success += b =>
                {
                    //SongBar.FadeInFromZero(300, Easing.OutQuint);
                    SongBar.Beatmap = new TournamentBeatmap(b);
                };

                beatmapLookupRequest.Failure += f =>
                {
                    if (f is OperationCanceledException)
                    {
                        return;
                    }

                    //SongBar.FadeInFromZero(300, Easing.OutQuint);
                    SongBar.Beatmap = new TournamentBeatmap(workingBeatmap.BeatmapInfo);
                };

                api.Queue(beatmapLookupRequest);
            }
        }

        public override void Hide()
        {
            closeInnerLazer();

            base.Hide();
        }

        protected override void IpcBeatmapChanged(ValueChangedEvent<TournamentBeatmap?> beatmap)
        {
        }

        protected override void CurrentMatchChanged(ValueChangedEvent<TournamentMatch?> match)
        {
            // showcase screen doesn't care about a match being selected.
            // base call intentionally omitted to not show match warning.
        }

        ControlPointInfo IBeatSyncProvider.ControlPoints => ((IBeatSyncProvider?)nestedGame)?.ControlPoints ?? new ControlPointInfo();
        IClock IBeatSyncProvider.Clock => ((IBeatSyncProvider?)nestedGame)?.Clock ?? Clock;

        ChannelAmplitudes IHasAmplitudes.CurrentAmplitudes => ((IBeatSyncProvider?)nestedGame)?.CurrentAmplitudes ?? ChannelAmplitudes.Empty;

        private partial class ShowCaseSideFlash : MenuSideFlashes
        {
            protected override bool RefreshColoursEveryFlash => true;

            protected override Color4 GetBaseColour() => FlashColor.Value;

            protected override float Intensity => FlashIntensity.Value;

            protected override bool OnlyKiai =>
                FlashKiaiOnly.Value;

            public readonly BindableFloat FlashIntensity = new BindableFloat(2)
            {
                MinValue = 0,
                MaxValue = 8,
            };

            public readonly BindableBool FlashKiaiOnly = new BindableBool(true);
            public readonly Bindable<Color4> FlashColor = new Bindable<Color4>();

            protected override void LoadComplete()
            {
                base.LoadComplete();

                FlashIntensity.BindValueChanged(i =>
                {
                    LeftBox.Width = BOX_WIDTH * i.NewValue;
                    RightBox.Width = BOX_WIDTH * i.NewValue;
                });
            }
        }
    }
}
