// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Models;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Rulesets;
using osu.Game.Tournament.Models;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Components
{
    [Cached]
    public partial class SongBar : CompositeDrawable
    {
        private IBeatmapInfo? beatmap;

        protected FillFlowContainer LeftDataContainer = null!;
        protected Container BeatmapPanel = null!;
        protected FillFlowContainer RightDataContainer = null!;

        private SpriteIcon leftArrow = null!;
        private SpriteIcon rightArrow = null!;
        private Container modContainer = null!;

        public Bindable<ColourInfo?> SongBarColour { get; } = new Bindable<ColourInfo?>();

        protected readonly Bindable<ColourInfo> ArrowColor = new Bindable<ColourInfo>(Color4.White);

        public const float HEIGHT = 50f;

        [Resolved]
        protected LadderInfo Ladder { get; private set; } = null!;

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        protected List<Drawable[]> LeftData = new List<Drawable[]>();
        protected List<Drawable[]> RightData = new List<Drawable[]>();

        public IBeatmapInfo? Beatmap
        {
            protected get => beatmap;
            set
            {
                if (beatmap == value)
                    return;

                beatmap = value;
                refreshContent();
            }
        }

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        private LegacyMods mods;

        public LegacyMods Mods
        {
            get => mods;
            set
            {
                if (mods == value)
                    return;

                mods = value;
                refreshContent();
            }
        }

        protected FillFlowContainer Flow = null!;

        private string? modString;

        // Todo: This is a hack for https://github.com/ppy/osu-framework/issues/3617 since this container is at the very edge of the screen and potentially initially masked away.
        protected override bool ComputeIsMaskedAway(RectangleF maskingBounds) => false;

        [Resolved]
        private TextureStore store { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            Anchor = Anchor.BottomCentre;
            Origin = Anchor.BottomCentre;

            RelativeSizeAxes = Axes.None;
            AutoSizeAxes = Axes.X;
            Height = HEIGHT + 7f;

            Padding = new MarginPadding { Bottom = 7f };

            InternalChild = new FillFlowContainer
            {
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
                RelativeSizeAxes = Axes.Y,
                AutoSizeAxes = Axes.X,
                Direction = FillDirection.Horizontal,
                Children = new Drawable[]
                {
                    new Container
                    {
                        Name = "Left arrow",
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Child = leftArrow = new SpriteIcon
                        {
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreRight,
                            Size = new Vector2(30),
                            Icon = FontAwesome.Solid.ChevronRight,
                            Shadow = true
                        },
                    },
                    new Container
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        RelativeSizeAxes = Axes.Y,
                        AutoSizeAxes = Axes.X,
                        Children = new Drawable[]
                        {
                            new Container
                            {
                                Padding = new MarginPadding { Vertical = 6f },
                                RelativeSizeAxes = Axes.Both,
                                Child = new BackdropBlurContainer
                                {
                                    BlurSigma = new Vector2(10f),
                                    RelativeSizeAxes = Axes.Both,
                                    CornerRadius = 5,
                                    Masking = true,
                                    Child = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = Colour4.Black,
                                        Alpha = 0.5f,
                                    }
                                },
                            },
                            new FillFlowContainer
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                AutoSizeAxes = Axes.X,
                                RelativeSizeAxes = Axes.Y,
                                Direction = FillDirection.Horizontal,
                                Masking = true,

                                Children = new Drawable[]
                                {
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.Y,
                                        Width = 240,
                                        Name = "Left data",
                                        Children = new Drawable[]
                                        {
                                            modContainer = new Container
                                            {
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                AutoSizeAxes = Axes.X,
                                                RelativeSizeAxes = Axes.Y,
                                                Padding = new MarginPadding { Left = 17f }
                                            },
                                            LeftDataContainer = new FillFlowContainer
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                                Anchor = Anchor.CentreRight,
                                                Origin = Anchor.CentreRight,
                                                Direction = FillDirection.Vertical,
                                            }
                                        },
                                    },
                                    BeatmapPanel = new Container
                                    {
                                        RelativeSizeAxes = Axes.Y,
                                        AutoSizeAxes = Axes.X,
                                        Child = new SongBarBeatmapPanel(beatmap)
                                        {
                                            Width = 500,
                                            CenterText = true
                                        },
                                    },
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.Y,
                                        Width = 240,
                                        Name = "Right data",
                                        Children = new Drawable[]
                                        {
                                            RightDataContainer = new FillFlowContainer
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                Direction = FillDirection.Vertical,
                                            }
                                        },
                                    },
                                }
                            },
                        }
                    },
                    new Container
                    {
                        Name = "Right arrow",
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Child = rightArrow = new SpriteIcon
                        {
                            Size = new Vector2(30),
                            Icon = FontAwesome.Solid.ChevronLeft,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Shadow = true
                        },
                    }
                }
            };

            ArrowColor.BindValueChanged(c =>
            {
                leftArrow.FadeColour(c.NewValue, 300);
                rightArrow.FadeColour(c.NewValue, 300);
            });

            LeftDataIndex.BindValueChanged(i =>
            {
                if (i.NewValue >= LeftData.Count && i.NewValue != 0)
                {
                    LeftDataIndex.Value = 0;
                    return;
                }

                if (i.NewValue >= LeftData.Count)
                    return;

                LeftDataContainer.FinishTransforms();

                LeftDataContainer.FadeOut(150).Then().Schedule(c =>
                {
                    c.Clear(false);
                    if (i.NewValue >= LeftData.Count)
                        return;

                    c.Children = LeftData.ElementAt(i.NewValue);
                }, LeftDataContainer).Then().FadeIn(150);
            });

            RightDataIndex.BindValueChanged(i =>
            {
                if (i.NewValue >= RightData.Count && i.NewValue != 0)
                {
                    RightDataIndex.Value = 0;
                    return;
                }

                if (i.NewValue >= RightData.Count)
                    return;

                RightDataContainer.FinishTransforms();

                RightDataContainer.FadeOut(150).Then().Schedule(c =>
                {
                    c.Clear(false);
                    if (i.NewValue >= RightData.Count)
                        return;

                    c.Children = RightData.ElementAt(i.NewValue);
                }, RightDataContainer).Then().FadeIn(150);
            });

            SongBarColour.BindValueChanged(c => ArrowColor.Value = c.NewValue ?? Color4.White);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            leftArrow.MoveToX(-25, 1500, Easing.Out).Then().MoveToX(0, 1500, Easing.In).Loop();
            rightArrow.MoveToX(25, 1500, Easing.Out).Then().MoveToX(0, 1500, Easing.In).Loop();
        }

        protected readonly BindableInt LeftDataIndex = new BindableInt();
        protected readonly BindableInt RightDataIndex = new BindableInt();

        // 轮换时间5秒
        private const int wait_turn_time = 5000;

        private double waitTime;

        protected override void Update()
        {
            base.Update();

            if (waitTime > wait_turn_time)
            {
                LeftDataIndex.Value = (LeftDataIndex.Value + 1) % Math.Max(1, LeftData.Count);
                RightDataIndex.Value = (RightDataIndex.Value + 1) % Math.Max(1, RightData.Count);
                waitTime = 0;
            }

            waitTime += Time.Elapsed;
        }

        private void refreshContent() => Scheduler.AddOnce(() =>
        {
            waitTime = 0;

            var modsForFetch = mods;
            modsForFetch &= ~LegacyMods.FreeMod;

            modString = Ladder.CurrentMatch.Value?.Round.Value?.Beatmaps.FirstOrDefault(b => b.ID == beatmap?.OnlineID)?.Mods;

            modContainer.Clear();

            if (!string.IsNullOrEmpty(modString))
            {
                modContainer.Add(new TournamentModIcon(modString)
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Y,
                    Width = 44f,
                });
            }

            if (beatmap == null)
            {
                beatmap = new BeatmapInfo
                {
                    Metadata = new BeatmapMetadata
                    {
                        Artist = "未知",
                        Title = "未选择谱面",
                        Author = new RealmUser { Username = "未知" },
                    },
                    DifficultyName = "未知",
                    BeatmapSet = new BeatmapSetInfo(),
                    StarRating = 0,
                    Difficulty = new BeatmapDifficulty
                    {
                        CircleSize = 0,
                        DrainRate = 0,
                        OverallDifficulty = 0,
                        ApproachRate = 0,
                    },
                };

                Schedule(PostUpdate);
                return;
            }

            var req = new GetBeatmapAttributesRequest(beatmap.OnlineID, ((int)modsForFetch).ToString(), ruleset.Value.ShortName);
            req.Success += res =>
            {
                ((TournamentBeatmap)beatmap).StarRating = res.Attributes.StarRating;
                Schedule(PostUpdate);
            };
            req.Failure += _ =>
            {
                Schedule(PostUpdate);
            };
            api.Queue(req);
        });

        protected string? GetBeatmapModPosition()
        {
            var roundBeatmap = Ladder.CurrentMatch.Value?.Round.Value?.Beatmaps.FirstOrDefault(roundMap => roundMap.ID == beatmap!.OnlineID);

            if (roundBeatmap == null)
                return null;

            // hardcode
            if (roundBeatmap.Mods == "FM")
            {
                mods = LegacyMods.FreeMod;
            }

            var modArray = Ladder.CurrentMatch.Value!.Round.Value.Beatmaps.Where(b => b.Mods == roundBeatmap.Mods).ToArray();

            if (modArray.Length == 1)
            {
                return roundBeatmap.Mods;
            }

            int id = Array.FindIndex(modArray, b => b.ID == roundBeatmap?.ID) + 1;

            return $"{roundBeatmap.Mods}{id}";
        }

        protected virtual void PostUpdate()
        {
            // 这步会顺便判断是否为FM谱面
            string? modPosition = GetBeatmapModPosition();

            LeftData.Clear();
            RightData.Clear();
            LeftDataContainer.Clear();
            RightDataContainer.Clear();

            LeftData.AddRange(CreateLeftData());

            RightData.AddRange(CreateRightData(modPosition));

            BeatmapPanel.Child = new SongBarBeatmapPanel(beatmap)
            {
                Width = 500,
                CenterText = true,
            };

            LeftDataIndex.Value = 0;
            LeftDataIndex.TriggerChange();
            RightDataIndex.Value = 0;
            RightDataIndex.TriggerChange();
        }

        protected virtual Drawable[][] CreateLeftData()
        {
            GetBeatmapInformation(mods, out double bpm, out double length, out _, out _);

            return new Drawable[][]
            {
                new Drawable[]
                {
                    new DiffPiece(("BPM", $"{bpm:0.#}"))
                    {
                        Origin = Anchor.CentreRight,
                        Anchor = Anchor.CentreRight,
                    },
                    new DiffPiece(("谱面长度", length.ToFormattedDuration().ToString()))
                    {
                        Origin = Anchor.CentreRight,
                        Anchor = Anchor.CentreRight,
                    },
                }
            };
        }

        protected virtual DiffPiece[][] CreateRightData(string? modPosition)
        {
            if ((mods & LegacyMods.FreeMod) > 0)
            {
                return CreateFmDiffPieces(modPosition);
            }

            GetBeatmapInformation(mods, out double _, out double _, out _, out var stats);

            List<(string, string)> diffPieces = new List<(string, string)>(2)
            {
                ("星级", $"{beatmap!.StarRating:0.00}")
            };

            if (modPosition != null)
            {
                diffPieces.Add(("谱面位置", modPosition));
            }

            return new[]
            {
                new[]
                {
                    new DiffPiece(stats),
                    new DiffPiece(diffPieces.ToArray()),
                }
            };
        }

        protected virtual DiffPiece[][] CreateFmDiffPieces(string? modPosition)
        {
            List<DiffPiece[]> diffPieces = new List<DiffPiece[]>();

            foreach (string mod in TournamentGameBase.Freemods)
            {
                GetBeatmapInformation(TournamentGameBase.ConvertFromAcronym(mod), out _, out _, out _, out var stats);
                diffPieces.Add(new[]
                {
                    new DiffPiece(stats)
                    {
                        Origin = Anchor.CentreLeft,
                        Anchor = Anchor.CentreLeft,
                    },
                    new DiffPiece(createSrAndPosition(beatmap!.StarRating, mod))
                    {
                        Origin = Anchor.CentreLeft,
                        Anchor = Anchor.CentreLeft,
                    }
                });
            }

            return diffPieces.ToArray();

            (Drawable heading, Drawable content)[] createSrAndPosition(double sr, string mod)
            {
                var modColor = Ladder.ModColors.FirstOrDefault(m => m.ModName == mod);

                var modSection = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(5, 0),
                    Children = new Drawable[]
                    {
                        modPosition != null ? createText($"{modPosition}", false) : Empty(),
                        new TournamentSpriteTextWithBackground(mod, text =>
                        {
                            text.Font = OsuFont.Default.With(weight: FontWeight.Bold, size: 15);
                        })
                        {
                            BackgroundColor = modColor?.BackgroundColor ?? TournamentGame.ELEMENT_BACKGROUND_COLOUR,
                            TextColor = modColor?.TextColor ?? TournamentGame.ELEMENT_FOREGROUND_COLOUR
                        }
                    }
                };

                return new[]
                {
                    (createText("星级", true) as Drawable, createText($"{sr:0.00}", false) as Drawable),
                    (createText("铺面位置", true) as Drawable, modSection as Drawable),
                };
            }

            TournamentSpriteText createText(string text, bool bold) => new TournamentSpriteText
                { Text = text, Font = OsuFont.Torus.With(weight: bold ? FontWeight.Bold : FontWeight.Regular, size: 15) };
        }

        public partial class DiffPiece : FillFlowContainer
        {
            public DiffPiece(params (string heading, string content)[] tuples)
                : this()
            {
                for (int i = 0; i < tuples.Length; i++)
                {
                    (string heading, string content) = tuples[i];

                    if (i > 0)
                    {
                        addDelimiter();
                    }

                    addText(CreateHeadingText(heading), s => cp(s, false));
                    addText(" ", s => cp(s, false));
                    addText(CreateContentText(content), s => cp(s, true));
                }
            }

            /// <summary>
            /// Creates the heading text drawable. Override to customize font.
            /// </summary>
            protected virtual TournamentSpriteText CreateHeadingText(string text) => new TournamentSpriteText { Text = text };

            /// <summary>
            /// Creates the content text drawable. Override to customize font.
            /// </summary>
            protected virtual TournamentSpriteText CreateContentText(string text) => new TournamentSpriteText { Text = text };

            public DiffPiece()
            {
                Margin = new MarginPadding { Horizontal = 7, Vertical = 1 };
                AutoSizeAxes = Axes.Both;
                Direction = FillDirection.Horizontal;
            }

            public DiffPiece(params Drawable[] drawables)
                : this()
            {
                for (int i = 0; i < drawables.Length; i++)
                {
                    if (i > 0)
                    {
                        addDelimiter();
                    }

                    Add(drawables[i]);
                }
            }

            public DiffPiece(params (Drawable heading, Drawable content)[] customPairs)
                : this()
            {
                for (int i = 0; i < customPairs.Length; i++)
                {
                    if (i > 0)
                    {
                        addDelimiter();
                    }

                    Add(customPairs[i].heading);
                    Add(new DrawableSpacer());
                    Add(customPairs[i].content);
                }
            }

            private static void cp(SpriteText s, bool bold)
            {
                s.Font = OsuFont.Torus.With(weight: bold ? FontWeight.Bold : FontWeight.Regular, size: 15);
            }

            private void addText(LocalisableString text, Action<SpriteText>? creationParameters = null)
            {
                var spriteText = new TournamentSpriteText { Text = text };
                creationParameters?.Invoke(spriteText);
                Add(spriteText);
            }

            private void addText(SpriteText text, Action<SpriteText>? creationParameters = null)
            {
                creationParameters?.Invoke(text);
                Add(text);
            }

            private void addDelimiter()
            {
                addText("/", s =>
                {
                    cp(s, false);
                    s.Margin = new MarginPadding { Horizontal = 2f };
                });
            }

            private partial class DrawableSpacer : Drawable
            {
                public DrawableSpacer()
                {
                    Width = 4;
                    Height = 1;
                }
            }
        }

        protected void GetBeatmapInformation(LegacyMods mods, out double bpm, out double length, out string srExtra, out (string heading, string content)[] stats)
        {
            bpm = beatmap!.BPM;
            length = beatmap.Length;
            string hardRockExtra = "";
            srExtra = "";

            float ar = beatmap.Difficulty.ApproachRate;
            float cs = beatmap.Difficulty.CircleSize;
            float od = beatmap.Difficulty.OverallDifficulty;
            float hp = beatmap.Difficulty.DrainRate;

            if ((mods & LegacyMods.Easy) > 0)
            {
                cs /= 2;
                ar /= 2;
                od /= 2;
                hp /= 2;
                hardRockExtra = "*";
            }

            if ((mods & LegacyMods.HardRock) > 0)
            {
                cs = MathF.Min(cs * 1.3f, 10);
                ar = MathF.Min(ar * 1.4f, 10);
                od = MathF.Min(od * 1.4f, 10);
                hp = MathF.Min(hp * 1.4f, 10);
                hardRockExtra = "*";
            }

            double preempt = (int)IBeatmapDifficultyInfo.DifficultyRange(ar, 1800, 1200, 450);
            double hitWindow = (int)IBeatmapDifficultyInfo.DifficultyRange(od, 80, 50, 20);

            if ((mods & LegacyMods.DoubleTime) > 0)
            {
                preempt /= 1.5;
                hitWindow /= 1.5;
                bpm *= 1.5f;
                length /= 1.5f;
            }

            if ((mods & LegacyMods.HalfTime) > 0)
            {
                preempt /= 0.75;
                hitWindow /= 0.75;
                bpm *= 0.75f;
                length /= 0.75f;
            }

            // temporary local calculation (taken from OsuDifficultyCalculator)
            ar = (float)(preempt > 1200 ? (1800 - preempt) / 120 : (1200 - preempt) / 150 + 5);
            od = (float)((80 - hitWindow) / 6);

            switch (ruleset.Value.OnlineID)
            {
                default:
                    stats = new (string heading, string content)[]
                    {
                        ("圆圈大小", $"{cs:0.#}{hardRockExtra}"),
                        ("缩圈速度", $"{ar:0.#}{hardRockExtra}"),
                        ("准度要求", $"{od:0.#}{hardRockExtra}"),
                    };
                    break;

                case 1:
                case 3:
                    stats = new (string heading, string content)[]
                    {
                        ("准度要求", $"{beatmap.Difficulty.OverallDifficulty:0.#}{hardRockExtra}"),
                        ("掉血速度", $"{hp:0.#}{hardRockExtra}")
                    };
                    break;

                case 2:
                    stats = new (string heading, string content)[]
                    {
                        ("水果大小", $"{cs:0.#}{hardRockExtra}"),
                        ("下落速度", $"{ar:0.#}"),
                    };
                    break;
            }
        }
    }
}
