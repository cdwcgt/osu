// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
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
    public partial class SongBar : CompositeDrawable
    {
        protected IBeatmapInfo? beatmap;

        public const float HEIGHT = 145 / 2f;

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        public IBeatmapInfo? Beatmap
        {
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

        protected LegacyMods mods;

        public LegacyMods Mods
        {
            get => mods;
            set
            {
                mods = value;
                refreshContent();
            }
        }

        protected FillFlowContainer Flow = null!;

        private bool expanded;

        public bool Expanded
        {
            get => expanded;
            set
            {
                expanded = value;
                Flow.Direction = expanded ? FillDirection.Full : FillDirection.Vertical;
            }
        }

        // Todo: This is a hack for https://github.com/ppy/osu-framework/issues/3617 since this container is at the very edge of the screen and potentially initially masked away.
        protected override bool ComputeIsMaskedAway(RectangleF maskingBounds) => false;

        [Resolved]
        private TextureStore store { get; set; } = null!;

        public SongBar()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            Masking = true;
            CornerRadius = 5;

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    Colour = colours.Gray3,
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0.4f,
                },
                Flow = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Full,
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                }
            };

            Expanded = true;
        }

        private void refreshContent()
        {
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

            var req = new GetBeatmapAttributesRequest(beatmap.OnlineID, ((int)mods).ToString(), ruleset.Value.ShortName);
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
        }

        protected virtual void PostUpdate()
        {
            GetBeatmapInformation(out double bpm, out double length, out string srExtra, out var stats);

            Flow.Children = new Drawable[]
            {
                new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Height = HEIGHT,
                    Width = 0.5f,
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,

                    Children = new Drawable[]
                    {
                        new GridContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            ColumnDimensions = new[] { new Dimension(GridSizeMode.Relative, 0.45f) },

                            Content = new[]
                            {
                                new Drawable[]
                                {
                                    new FillFlowContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Direction = FillDirection.Vertical,
                                        Children = new Drawable[]
                                        {
                                            new DiffPiece(stats),
                                            new DiffPiece(("难度星级", $"{beatmap.StarRating:0.00}{srExtra}"))
                                        }
                                    },
                                    new FillFlowContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Direction = FillDirection.Vertical,
                                        Children = new Drawable[]
                                        {
                                            new DiffPiece(("谱面长度", length.ToFormattedDuration().ToString())),
                                            new DiffPiece(("BPM", $"{bpm:0.#}")),
                                        }
                                    },
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Children = new Drawable[]
                                        {
                                            new Box
                                            {
                                                Colour = Color4.Black,
                                                RelativeSizeAxes = Axes.Both,
                                                Alpha = 0.1f,
                                            },
                                            new Sprite
                                            {
                                                Texture = store.Get("hsc-logo"),
                                                Scale = new Vector2(0.32f),
                                                Margin = new MarginPadding(20),
                                                Origin = Anchor.CentreRight,
                                                Anchor = Anchor.CentreRight,
                                            },
                                        }
                                    },
                                },
                            }
                        }
                    }
                },
                new TournamentBeatmapPanel(beatmap)
                {
                    RelativeSizeAxes = Axes.X,
                    Width = 0.5f,
                    Height = HEIGHT,
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                }
            };
        }

        protected void GetBeatmapInformation(out double bpm, out double length, out string srExtra, out (string heading, string content)[] stats)
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
                        ("圆圈大小", $"{cs:0.#}{hardRockExtra}"),
                        ("缩圈速度", $"{ar:0.#}"),
                    };
                    break;
            }
        }

        public partial class DiffPiece : TextFlowContainer
        {
            public DiffPiece(params (string heading, string content)[] tuples)
            {
                Margin = new MarginPadding { Horizontal = 15, Vertical = 1 };
                AutoSizeAxes = Axes.Both;

                static void cp(SpriteText s, bool bold)
                {
                    s.Font = OsuFont.Torus.With(weight: bold ? FontWeight.Bold : FontWeight.Regular, size: 15);
                }

                for (int i = 0; i < tuples.Length; i++)
                {
                    (string heading, string content) = tuples[i];

                    if (i > 0)
                    {
                        AddText(" / ", s =>
                        {
                            cp(s, false);
                            s.Spacing = new Vector2(-2, 0);
                        });
                    }

                    AddText(new TournamentSpriteText { Text = heading }, s => cp(s, false));
                    AddText(" ", s => cp(s, false));
                    AddText(new TournamentSpriteText { Text = content }, s => cp(s, true));
                }
            }
        }
    }
}
