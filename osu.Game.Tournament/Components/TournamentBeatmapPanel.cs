// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Graphics;
using osu.Game.Tournament.Models;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Components
{
    public partial class TournamentBeatmapPanel : CompositeDrawable
    {
        public readonly IBeatmapInfo? Beatmap;

        private readonly string? mod;

        public const float HEIGHT = 50;

        private readonly Bindable<TournamentMatch?> currentMatch = new Bindable<TournamentMatch?>();

        private Box flash = null!;
        private PadLock padLock = null!;
        private readonly bool isMappool;

        public TournamentBeatmapPanel(IBeatmapInfo? beatmap, string mod = "", bool isMappool = false)
        {
            Beatmap = beatmap;
            this.mod = mod;
            this.isMappool = isMappool;

            Width = 400;
            Height = HEIGHT;
        }

        [BackgroundDependencyLoader]
        private void load(LadderInfo ladder)
        {
            currentMatch.BindValueChanged(matchChanged);
            currentMatch.BindTo(ladder.CurrentMatch);

            Masking = true;

            AddRangeInternal(new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black,
                },
                new NoUnloadBeatmapSetCover
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = OsuColour.Gray(0.5f),
                    OnlineInfo = (Beatmap as IBeatmapSetOnlineInfo),
                },
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Padding = new MarginPadding(15),
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        new TournamentSpriteText
                        {
                            Text = Beatmap?.GetDisplayTitleRomanisable(false, false) ?? (LocalisableString)@"unknown",
                            Font = OsuFont.Torus.With(weight: FontWeight.Bold),
                        },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Children = new Drawable[]
                            {
                                new TournamentSpriteText
                                {
                                    Text = "mapper",
                                    Padding = new MarginPadding { Right = 5 },
                                    Font = OsuFont.Torus.With(weight: FontWeight.Regular, size: 14)
                                },
                                new TournamentSpriteText
                                {
                                    Text = Beatmap?.Metadata.Author.Username ?? "unknown",
                                    Padding = new MarginPadding { Right = 20 },
                                    Font = OsuFont.Torus.With(weight: FontWeight.Bold, size: 14)
                                },
                                new TournamentSpriteText
                                {
                                    Text = "difficulty",
                                    Padding = new MarginPadding { Right = 5 },
                                    Font = OsuFont.Torus.With(weight: FontWeight.Regular, size: 14)
                                },
                                new TournamentSpriteText
                                {
                                    Text = Beatmap?.DifficultyName ?? "unknown",
                                    Font = OsuFont.Torus.With(weight: FontWeight.Bold, size: 14)
                                },
                            }
                        }
                    },
                },
                padLock = new PadLock
                {
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                    Alpha = 0f,
                },
                flash = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Gray,
                    Blending = BlendingParameters.Additive,
                    Alpha = 0,
                },
            });

            if (!string.IsNullOrEmpty(mod))
            {
                AddInternal(new TournamentModIcon(mod)
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Margin = new MarginPadding(10),
                    Width = 60,
                    RelativeSizeAxes = Axes.Y,
                });
            }
        }

        private void matchChanged(ValueChangedEvent<TournamentMatch?> match)
        {
            if (match.OldValue != null)
                match.OldValue.PicksBans.CollectionChanged -= picksBansOnCollectionChanged;
            if (match.NewValue != null)
                match.NewValue.PicksBans.CollectionChanged += picksBansOnCollectionChanged;

            Scheduler.AddOnce(updateState);
        }

        private void picksBansOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            => Scheduler.AddOnce(updateState);

        private BeatmapChoice? choice;

        private void updateState()
        {
            if (currentMatch.Value == null)
            {
                return;
            }

            var found = currentMatch.Value.PicksBans.Where(p => p.BeatmapID == Beatmap?.OnlineID).ToList();
            var foundProtected = isMappool ? found.FirstOrDefault(s => s.Type == ChoiceType.Protected) : null;
            var lastFound = found.LastOrDefault();

            bool shouldFlash = lastFound != choice;

            if (foundProtected != null && isMappool)
            {
                padLock.Team = foundProtected.Team;
                padLock.Show();

                if (currentMatch.Value.PicksBans.Any(p => p.Type == ChoiceType.Pick))
                {
                    padLock.FadeTo(0.5f);
                }
            }
            else
            {
                padLock.Hide();
            }

            if (lastFound != null)
            {
                if (shouldFlash)
                    flash.FadeOutFromOne(500).Loop(0, 10);

                BorderThickness = 6;

                BorderColour = TournamentGame.GetTeamColour(lastFound.Team);

                switch (lastFound.Type)
                {
                    case ChoiceType.Pick:
                        Colour = Color4.White;
                        Alpha = 1;
                        break;

                    case ChoiceType.Ban:
                        Colour = Color4.Gray;
                        Alpha = 0.5f;
                        break;

                    case ChoiceType.Protected:
                        Alpha = 1f;
                        BorderThickness = 0;
                        break;
                }
            }
            else
            {
                Colour = Color4.White;
                BorderThickness = 0;
                Alpha = 1;
            }

            choice = lastFound;
        }

        private partial class NoUnloadBeatmapSetCover : UpdateableOnlineBeatmapSetCover
        {
            // As covers are displayed on stream, we want them to load as soon as possible.
            protected override double LoadDelay => 0;

            // Use DelayedLoadWrapper to avoid content unloading when switching away to another screen.
            protected override DelayedLoadWrapper CreateDelayedLoadWrapper(Func<Drawable> createContentFunc, double timeBeforeLoad)
                => new DelayedLoadWrapper(createContentFunc(), timeBeforeLoad);
        }

        private partial class PadLock : Container
        {
            [Resolved]
            private OsuColour osuColour { get; set; } = null!;

            public TeamColour Team
            {
                set => lockIcon.Colour = value == TeamColour.Red ? osuColour.TeamColourRed : osuColour.TeamColourBlue;
            }

            private Sprite background = null!;
            private SpriteIcon lockIcon = null!;

            [BackgroundDependencyLoader]
            private void load(TextureStore textures)
            {
                Size = new Vector2(40);

                Children = new Drawable[]
                {
                    background = new Sprite
                    {
                        RelativeSizeAxes = Axes.Both,
                        FillMode = FillMode.Fit,
                        Texture = textures.Get("Icons/BeatmapDetails/mod-icon"),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    },
                    lockIcon = new SpriteIcon
                    {
                        Origin = Anchor.Centre,
                        Anchor = Anchor.Centre,
                        Size = new Vector2(22),
                        Icon = FontAwesome.Solid.ShieldAlt,
                        Shadow = true,
                    }
                };
            }
        }
    }
}
