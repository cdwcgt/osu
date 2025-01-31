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
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.Components
{
    public partial class SongBarBeatmapPanel : TournamentBeatmapPanel
    {
        [Resolved]
        private SongBar songBar { get; set; } = null!;

        private Bindable<ColourInfo?> songBarColour = new Bindable<ColourInfo?>();

        public SongBarBeatmapPanel(RoundBeatmap beatmap, int? id = null, bool isMappool = false)
            : base(beatmap, id, isMappool)
        {
        }

        public SongBarBeatmapPanel(IBeatmapInfo? beatmap, string mod = "", bool isMappool = false)
            : base(beatmap, mod, isMappool)
        {
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            songBarColour.BindTo(songBar.SongBarColour);
            songBarColour.BindValueChanged(c =>
            {
                if (c.NewValue == null)
                {
                    MainContainer.BorderThickness = 0;
                    return;
                }

                MainContainer.BorderThickness = 6;
                MainContainer.BorderColour = c.NewValue.Value;
            }, true);
        }

        protected override void UpdateState()
        {
            base.UpdateState();

            if (songBarColour.Value == null)
                return;

            MainContainer.BorderThickness = 6;
            MainContainer.BorderColour = songBarColour.Value.Value;
        }

        protected override Drawable[] CreateInformation() =>
            new Drawable[]
            {
                new TournamentSpriteText
                {
                    Text = Beatmap?.GetDisplayTitleRomanisable(false, false) ?? (LocalisableString)@"未知",
                    Font = OsuFont.Torus.With(weight: FontWeight.Bold, size: 20),
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
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
