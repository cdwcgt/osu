// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Overlays;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.Components
{
    public partial class SongBarBeatmapPanel : TournamentBeatmapPanel
    {
        [Resolved]
        private SongBar songBar { get; set; } = null!;

        private readonly Bindable<ColourInfo?> songBarColour = new Bindable<ColourInfo?>();

        public SongBarBeatmapPanel(RoundBeatmap beatmap, int? id = null)
            : base(beatmap, id)
        {
        }

        public SongBarBeatmapPanel(IBeatmapInfo? beatmap, string mod = "")
            : base(beatmap, mod)
        {
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            songBarColour.BindTo(songBar.SongBarColour);
            songBarColour.BindValueChanged(_ => updateColor(), true);
        }

        private void updateColor()
        {
            // 如果没有自定义的 songBar 颜色 则使用 beatmapPanel 自己的颜色
            // 不要将 BorderThickness 设为 0
            if (songBarColour.Value == null)
            {
                return;
            }

            MainContainer.BorderThickness = 6;
            MainContainer.BorderColour = songBarColour.Value.Value;
        }

        protected override void UpdateState()
        {
            base.UpdateState();
            updateColor();
        }

        protected override Drawable[] CreateInformation() =>
            new Drawable[]
            {
                new Container
                {
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Padding = new MarginPadding { Horizontal = 10 },
                    Masking = true,
                    Child = new MarqueeContainer
                    {
                        OverflowSpacing = 30,
                        NonOverflowingContentAnchor = Anchor.Centre,
                        CreateContent = () => new TournamentSpriteText
                        {
                            Text = Beatmap?.GetDisplayTitleRomanisable(false, false) ?? (LocalisableString)@"未知",
                            Font = OsuFont.Torus.With(weight: FontWeight.Bold, size: 20),
                        }
                    },
                },
                new Container
                {
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                    AutoSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new TournamentSpriteText
                        {
                            Text = "|",
                            Font = OsuFont.Torus.With(weight: FontWeight.Regular, size: 15)
                        },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Origin = Anchor.CentreRight,
                            Anchor = Anchor.CentreLeft,
                            Direction = FillDirection.Horizontal,
                            Children = new Drawable[]
                            {
                                new TournamentSpriteText
                                {
                                    Text = "谱师",
                                    Padding = new MarginPadding { Right = 5 },
                                    Font = OsuFont.Torus.With(weight: FontWeight.Regular, size: 15)
                                },
                                new TournamentSpriteText
                                {
                                    Text = Beatmap?.Metadata.Author.Username ?? "未知",
                                    Padding = new MarginPadding { Right = 5 },
                                    Font = OsuFont.Torus.With(weight: FontWeight.Bold, size: 15)
                                },
                            }
                        },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Origin = Anchor.CentreLeft,
                            Anchor = Anchor.CentreRight,
                            Direction = FillDirection.Horizontal,
                            Children = new Drawable[]
                            {
                                new TournamentSpriteText
                                {
                                    Text = "难度",
                                    Padding = new MarginPadding { Left = 5 },
                                    Font = OsuFont.Torus.With(weight: FontWeight.Regular, size: 15)
                                },
                                new TournamentSpriteText
                                {
                                    Text = Beatmap?.DifficultyName ?? "未知",
                                    Padding = new MarginPadding { Left = 5 },
                                    Font = OsuFont.Torus.With(weight: FontWeight.Bold, size: 15)
                                },
                            }
                        }
                    },
                },
            };
    }
}
