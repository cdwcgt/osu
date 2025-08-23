// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Graphics;
using osu.Game.Models;
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

        public bool HiddenInformationBeforePicked { get; set; }

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

            if (beatmap.IsRandom)
            {
                HiddenInformationBeforePicked = true;
            }
        }

        private readonly int? id;
        private readonly bool isMappool;
        private Colour4 textColor;
        private Colour4 backgroundColor;
        protected Container MainContainer = null!;

        public TournamentBeatmapPanel(IBeatmapInfo? beatmap, string mod = "", bool isMappool = false)
        {
            Beatmap = beatmap;
            this.mod = mod;
            this.isMappool = isMappool;

            Width = mod == "TB" && isMappool ? 600 : 400;
            Height = HEIGHT;
        }

        [BackgroundDependencyLoader]
        private void load(LadderInfo ladder)
        {
            currentMatch.BindValueChanged(matchChanged);
            currentMatch.BindTo(ladder.CurrentMatch);

            if (!string.IsNullOrEmpty(mod))
            {
                backgroundColor = ladder.GetModColorByModName(mod).BackgroundColor;
                textColor = ladder.GetModColorByModName(mod).TextColor;
            }

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
                                    //particleBorder = new ParticleBorder
                                    //{
                                    //    Alpha = HiddenInformationBeforePicked ? 1 : 0,
                                    //},
                                    MainContainer = new Container
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Masking = true,
                                        Children = new Drawable[]
                                        {
                                            new Box
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                Colour = OsuColour.Gray(0.2f)
                                            },
                                            beatmapCover = new NoUnloadBeatmapSetCover
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                Colour = OsuColour.Gray(0.5f),
                                                Alpha = HiddenInformationBeforePicked ? 0 : 1,
                                                OnlineInfo = (Beatmap as IBeatmapSetOnlineInfo),
                                            },
                                            information = new FillFlowContainer
                                            {
                                                AutoSizeAxes = Axes.Both,
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                Padding = new MarginPadding(15),
                                                Direction = FillDirection.Vertical,
                                                Children = CreateInformation(HiddenInformationBeforePicked ? new BeatmapInfo() : Beatmap)
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
                                Width = 20,
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

            Scheduler.AddOnce(UpdateState);
        }

        protected virtual Drawable[] CreateInformation(IBeatmapInfo? beatmapInfo = null)
        {
            beatmapInfo ??= Beatmap;

            return new Drawable[]
            {
                new TournamentSpriteText
                {
                    Text = beatmapInfo?.GetDisplayTitleRomanisable(false, false) ?? (LocalisableString)@"未知",
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
                            Text = beatmapInfo?.Metadata.Author.Username ?? "未知",
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
                            Text = beatmapInfo?.DifficultyName ?? "未知",
                            Font = OsuFont.Torus.With(weight: FontWeight.Bold, size: 14)
                        },
                    }
                },
            };
        }

        private void picksBansOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            => Scheduler.AddOnce(UpdateState);

        private BeatmapChoice? choice;
        private bool centerText = false;
        private FillFlowContainer information = null!;
        private NoUnloadBeatmapSetCover beatmapCover;
        private ParticleBorder particleBorder;

        protected virtual void UpdateState()
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

                MainContainer.BorderThickness = 6;

                MainContainer.BorderColour = TournamentGame.GetTeamColour(lastFound.Team);

                switch (lastFound.Type)
                {
                    case ChoiceType.Pick:
                        MainContainer.Colour = Color4.White;

                        if (CornerRadius > 0)
                        {
                            MainContainer.EdgeEffect = new EdgeEffectParameters
                            {
                                Type = EdgeEffectType.Glow,
                                Colour = BorderColour,
                                Hollow = true,
                                Radius = 15
                            };

                            MainContainer.Alpha = 1;
                        }

                        break;

                    case ChoiceType.Ban:
                        MainContainer.Colour = Color4.Gray;
                        MainContainer.Alpha = 0.5f;
                        bannedSprite.Texture = textures.Get($"Ban/ban-{lastFound.Team.ToString().ToLowerInvariant()}");
                        bannedSprite.Show();
                        break;

                    case ChoiceType.Protected:
                        MainContainer.Alpha = 1;
                        MainContainer.BorderThickness = 0;
                        break;
                }
            }
            else
            {
                MainContainer.EdgeEffect = new EdgeEffectParameters();
                MainContainer.Colour = Color4.White;
                MainContainer.BorderThickness = 0;
                MainContainer.Alpha = 1;
                bannedSprite.Hide();
            }

            if (HiddenInformationBeforePicked && lastFound?.Type == ChoiceType.Pick)
            {
                beatmapCover.FadeIn(100);
                information.Children = CreateInformation();
                //particleBorder.FadeOut();
            }
            else if (HiddenInformationBeforePicked)
            {
                beatmapCover.FadeOut(50);
                information.Children = CreateInformation(HiddenBeatmap);
                //particleBorder.FadeIn();
            }

            choice = lastFound;
        }

        protected static BeatmapInfo HiddenBeatmap => new BeatmapInfo
        {
            Metadata = new BeatmapMetadata
            {
                Artist = @"?????",
                Title = @"?????????",
                Author = new RealmUser
                {
                    Username = @"????"
                },
            },
            DifficultyName = @"?????"
        };

        private partial class NoUnloadBeatmapSetCover : UpdateableOnlineBeatmapSetCover
        {
            // As covers are displayed on stream, we want them to load as soon as possible.
            protected override double LoadDelay => 0;

            // Use DelayedLoadWrapper to avoid content unloading when switching away to another screen.
            protected override DelayedLoadWrapper CreateDelayedLoadWrapper(Func<Drawable> createContentFunc, double timeBeforeLoad)
                => new DelayedLoadWrapper(createContentFunc(), timeBeforeLoad);

            [Resolved]
            private LargeTextureStore textures { get; set; } = null!;

            protected override Drawable? CreateDrawable(IBeatmapSetOnlineInfo? model)
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
