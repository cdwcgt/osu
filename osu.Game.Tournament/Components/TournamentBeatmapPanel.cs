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
using osuTK;
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
        private TournamentProtectIcon protectIcon = null!;
        private Sprite bannedSprite = null!;
        private TournamentModIcon? modIcon;
        private FillFlowContainer modContainer = null!;

        public bool CenterText
        {
            get => centerText;
            set
            {
                centerText = value;

                if (IsLoaded)
                    updateIsCenter();
            }
        }

        private void updateIsCenter()
        {
            setAnchor(information, centerText);
            information.Anchor = centerText ? Anchor.Centre : Anchor.CentreLeft;
            information.Origin = centerText ? Anchor.Centre : Anchor.CentreLeft;
        }

        private void setAnchor(FillFlowContainer fillContainer, bool isCenter)
        {
            foreach (var child in fillContainer.Children)
            {
                if (child is FillFlowContainer fillChild)
                {
                    setAnchor(fillChild, isCenter);
                }

                child.Anchor = isCenter ? Anchor.Centre : Anchor.TopLeft;
                child.Origin = isCenter ? Anchor.Centre : Anchor.TopLeft;
            }
        }

        [Resolved]
        private TextureStore textures { get; set; } = null!;

        public TournamentBeatmapPanel(RoundBeatmap beatmap, int? id = null, bool isMappool = false)
            : this(beatmap.Beatmap, beatmap.Mods, isMappool: isMappool)
        {
            this.id = id;
            backgroundColor = beatmap.BackgroundColor;
            textColor = beatmap.TextColor;
        }

        private readonly int? id;
        private readonly bool isMappool;
        private readonly Colour4 textColor;
        private readonly Colour4 backgroundColor;
        private Container container = null!;

        public TournamentBeatmapPanel(IBeatmapInfo? beatmap, string mod = "", float cornerRadius = 0, bool isMappool = false)
        {
            Beatmap = beatmap;
            this.mod = mod;
            this.isMappool = isMappool;

            Width = mod == "TB" && id.HasValue ? 600 : 400;
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
                new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    ColumnDimensions = new[]
                    {
                        new Dimension(),
                        new Dimension(GridSizeMode.AutoSize, maxSize: 30)
                    },
                    Content = new[]
                    {
                        new[]
                        {
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
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
                                            information = new FillFlowContainer
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
                                                        Text = Beatmap?.GetDisplayTitleRomanisable(false, false) ?? (LocalisableString)@"未知",
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
                                                                Text = Beatmap?.Metadata.Author.Username ?? "未知",
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
                                                                Text = Beatmap?.DifficultyName ?? "未知",
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
                                    bannedSprite = new Sprite
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Alpha = 0,
                                    },
                                    modContainer = new FillFlowContainer
                                    {
                                        Anchor = Anchor.CentreRight,
                                        Origin = Anchor.CentreRight,
                                        AutoSizeAxes = Axes.X,
                                        RelativeSizeAxes = Axes.Y,
                                        Direction = FillDirection.Horizontal,
                                        Margin = new MarginPadding(10),
                                        Spacing = new Vector2(-1, 0),
                                        Child = protectIcon = new TournamentProtectIcon
                                        {
                                            RelativeSizeAxes = Axes.Y,
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                            AlwaysPresent = true,
                                            Width = 21,
                                            Alpha = 0
                                        }
                                    },
                                }
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.Y,
                                Width = 30,
                                Alpha = id.HasValue ? 1 : 0,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = backgroundColor,
                                    },
                                    new TournamentSpriteText
                                    {
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Text = id.GetValueOrDefault().ToString(),
                                        Colour = textColor,
                                        Font = OsuFont.Torus.With(size: 30)
                                    }
                                }
                            }
                        }
                    },
                },
            });

            if (!string.IsNullOrEmpty(mod))
            {
                modContainer.Add(modIcon = new TournamentModIcon(mod)
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Width = 55,
                    RelativeSizeAxes = Axes.Y,
                });
            }

            updateIsCenter();
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
        private bool centerText = false;
        private FillFlowContainer information = null!;

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

            if (foundProtected != null)
            {
                protectIcon.Team = foundProtected.Team;
                protectIcon.Show();
            }
            else
            {
                protectIcon.Hide();
            }

            if (lastFound != null)
            {
                if (shouldFlash)
                    flash.FadeOutFromOne(500).Loop(0, 10);

                container.BorderThickness = 6;

                container.BorderColour = TournamentGame.GetTeamColour(lastFound.Team);

                switch (lastFound.Type)
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
                        bannedSprite.Texture = textures.Get($"Ban/ban-{lastFound.Team.ToString().ToLowerInvariant()}");
                        bannedSprite.Show();
                        break;

                    case ChoiceType.Protected:
                        container.Alpha = 1;
                        container.BorderThickness = 0;
                        break;
                }
            }
            else
            {
                container.EdgeEffect = new EdgeEffectParameters();
                container.Colour = Color4.White;
                container.BorderThickness = 0;
                container.Alpha = 1;
                bannedSprite.Hide();
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
