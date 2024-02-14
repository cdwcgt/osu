// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Graphics;
using osu.Game.Tournament.Models;
using osuTK.Graphics;

namespace osu.Game.Tournament.Components
{
    public partial class TournamentBeatmapPanel : CompositeDrawable
    {
        public readonly IBeatmapInfo? Beatmap;

        private readonly string mod;

        public const float HEIGHT = 50;

        private readonly Bindable<TournamentMatch?> currentMatch = new Bindable<TournamentMatch?>();

        private Box flash = null!;

        public TournamentBeatmapPanel(RoundBeatmap beatmap, int? id = null)
            : this(beatmap.Beatmap, beatmap.Mods)
        {
            this.id = id;
            backgroundColor = beatmap.BackgroundColor;
            textColor = beatmap.TextColor;
        }

        private readonly int? id;
        private readonly Colour4 textColor;
        private readonly Colour4 backgroundColor;
        private Container container = null!;

        public TournamentBeatmapPanel(IBeatmapInfo? beatmap, string mod = "", float cornerRadius = 0)
        {
            Beatmap = beatmap;
            this.mod = mod;

            Width = mod == "TB" ? 600 : 400;
            Height = HEIGHT;
            CornerRadius = cornerRadius;
        }

        [BackgroundDependencyLoader]
        private void load(LadderInfo ladder)
        {
            currentMatch.BindValueChanged(matchChanged);
            currentMatch.BindTo(ladder.CurrentMatch);

            AddRangeInternal(new Drawable[]
            {
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
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Width = id.HasValue ? 0.95f : 1,
                    Direction = FillDirection.Horizontal,
                    Children = new Drawable[]
                    {
                        container = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Masking = true,
                            Children = new Drawable[]
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
                                                    Text = "谱师",
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
                                                    Text = "难度",
                                                    Padding = new MarginPadding { Right = 5 },
                                                    Font = OsuFont.Torus.With(weight: FontWeight.Regular, size: 14)
                                                },
                                                new TournamentSpriteText
                                                {
                                                    Text = Beatmap?.DifficultyName ?? "unknown",
                                                    Font = OsuFont.Torus.With(weight: FontWeight.Bold, size: 14)
                                                },
                                            }
                                        },
                                    },
                                },

                                flash = new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = Color4.Gray,
                                    Blending = BlendingParameters.Additive,
                                    Alpha = 0,
                                },
                            }
                        },
                        new Container
                        {
                            AutoSizeAxes = Axes.Y,
                            RelativeSizeAxes = Axes.X,
                            Width = 0.05f,
                            Alpha = id.HasValue ? 1 : 0,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Colour = backgroundColor,
                                    Height = 42,
                                },
                                new TournamentSpriteText
                                {
                                    Anchor = Anchor.TopLeft,
                                    Text = id.GetValueOrDefault().ToString(),
                                    Colour = textColor,
                                    Margin = new MarginPadding { Top = 5, Left = 3 },
                                    Font = OsuFont.Torus.With(weight: FontWeight.SemiBold, size: 30)
                                }
                            }
                        }
                    }
                },
            });

            if (!string.IsNullOrEmpty(mod))
            {
                container.Add(new TournamentModIcon(mod)
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

            var newChoice = currentMatch.Value.PicksBans.FirstOrDefault(p => p.BeatmapID == Beatmap?.OnlineID);

            bool shouldFlash = newChoice != choice;

            if (newChoice != null)
            {
                if (shouldFlash)
                    flash.FadeOutFromOne(500).Loop(0, 10);

                container.BorderThickness = 6;

                container.BorderColour = TournamentGame.GetTeamColour(newChoice.Team);

                switch (newChoice.Type)
                {
                    case ChoiceType.Pick:
                        container.Colour = Color4.White;

                        if (CornerRadius > 0)
                        {
                            container.EdgeEffect = new EdgeEffectParameters
                            {
                                Type = EdgeEffectType.Glow,
                                Colour = BorderColour,
                                Hollow = true,
                                Radius = 15
                            };

                            container.Alpha = 1;
                        }

                        break;

                    case ChoiceType.Ban:
                        container.Colour = Color4.Gray;
                        container.Alpha = 0.5f;
                        break;
                }
            }
            else
            {
                container.EdgeEffect = new EdgeEffectParameters();
                container.Colour = Color4.White;
                container.BorderThickness = 0;
                container.Alpha = 1;
            }

            choice = newChoice;
        }

        private partial class NoUnloadBeatmapSetCover : UpdateableOnlineBeatmapSetCover
        {
            // As covers are displayed on stream, we want them to load as soon as possible.
            protected override double LoadDelay => 0;

            // Use DelayedLoadWrapper to avoid content unloading when switching away to another screen.
            protected override DelayedLoadWrapper CreateDelayedLoadWrapper(Func<Drawable> createContentFunc, double timeBeforeLoad)
                => new DelayedLoadWrapper(createContentFunc(), timeBeforeLoad);

            [Resolved]
            private LargeTextureStore textures { get; set; } = null!;

            protected override Drawable CreateDrawable(IBeatmapSetOnlineInfo model)
            {
                if (model == null)
                {
                    return new Sprite
                    {
                        RelativeSizeAxes = Axes.Both,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        FillMode = FillMode.Fill,
                        Texture = textures.Get("beatmap-empty")
                    };
                }

                return base.CreateDrawable(model);
            }
        }
    }
}
