// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Tournament.Components;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.Mods;
using osu.Game.Tournament.Models;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Showcase
{
    public partial class ShowcaseScreen : BeatmapInfoScreen
    {
        [Resolved]
        private GameHost host { get; set; } = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private LadderInfo ladder { get; set; } = null!;

        private NestedOsuGame? nestedGame;

        private MusicController? musicController;
        private readonly Bindable<IReadOnlyList<Mod>> mods = new Bindable<IReadOnlyList<Mod>>();
        private readonly Bindable<LegacyMods> legacyMods = new Bindable<LegacyMods>();
        private Container showcaseContainer = null!;

        private readonly BindableBool useOsuLazer = new BindableBool();

        [BackgroundDependencyLoader]
        private void load()
        {
            nestedGame = new NestedOsuGame(host.Storage, new ForwardingAPIAccess(api))
            {
                Masking = true
            };
            nestedGame.SetHost(host);

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
                Current = useOsuLazer,
            });

            useOsuLazer.BindValueChanged(u =>
            {
                if (u.NewValue)
                {
                    showcaseContainer.Clear(true);
                    startInnerLazer();
                }
                else
                {
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

        private void startInnerLazer()
        {
            nestedGame = new NestedOsuGame(host.Storage, new ForwardingAPIAccess(api))
            {
                Masking = true
            };
            nestedGame.SetHost(host);
            showcaseContainer.Add(nestedGame);

            nestedGame.OnLoadComplete += _ =>
            {
                musicController = nestedGame.Dependencies.Get<MusicController>();

                musicController.TrackChanged += beatmapChanged;

                mods.BindTarget = nestedGame.Dependencies.Get<Bindable<IReadOnlyList<Mod>>>();
            };
        }

        private void closeInnerLazer()
        {
            if (nestedGame != null)
                showcaseContainer.Remove(nestedGame, true);

            musicController = null;
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
                SongBar.ModString = existing.Mods;
            }
            else
            {
                beatmapLookupRequest = new GetBeatmapRequest(new APIBeatmap { OnlineID = workingBeatmap.BeatmapInfo.OnlineID });

                beatmapLookupRequest.Success += b =>
                {
                    //SongBar.FadeInFromZero(300, Easing.OutQuint);
                    SongBar.Beatmap = new TournamentBeatmap(b);
                };

                beatmapLookupRequest.Failure += _ =>
                {
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
    }
}
