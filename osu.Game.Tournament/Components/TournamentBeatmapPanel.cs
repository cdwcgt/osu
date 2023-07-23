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
using osu.Framework.Graphics.Sprites;
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
        public readonly TournamentBeatmap Beatmap;

        private readonly string? mod;

        public const float HEIGHT = 50;

        private readonly Bindable<TournamentMatch> currentMatch = new Bindable<TournamentMatch>();
        private Box? flash;
        private PadLock? padLock;

        public TournamentBeatmapPanel(TournamentBeatmap beatmap, string? mod = null)
        {
            ArgumentNullException.ThrowIfNull(beatmap);

            Beatmap = beatmap;
            this.mod = mod;

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
                new UpdateableOnlineBeatmapSetCover
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = OsuColour.Gray(0.5f),
                    OnlineInfo = Beatmap,
                },
                padLock = new PadLock
                {
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                    Alpha = 0f,
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
                            Text = Beatmap.GetDisplayTitleRomanisable(false, false),
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
                                    Text = Beatmap.Metadata.Author.Username,
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
                                    Text = Beatmap.DifficultyName,
                                    Font = OsuFont.Torus.With(weight: FontWeight.Bold, size: 14)
                                },
                            }
                        }
                    },
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

        private void matchChanged(ValueChangedEvent<TournamentMatch> match)
        {
            if (match.OldValue != null)
                match.OldValue.PicksBans.CollectionChanged -= picksBansOnCollectionChanged;
            match.NewValue.PicksBans.CollectionChanged += picksBansOnCollectionChanged;
            updateState();
        }

        private void picksBansOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            => updateState();

        private BeatmapChoice? choice;

        private void updateState()
        {
            var found = currentMatch.Value.PicksBans.Where(p => p.BeatmapID == Beatmap.OnlineID).ToList();
            var foundProtected = found.FirstOrDefault(s => s.Type == ChoiceType.Protected);
            var lastFound = found.LastOrDefault();

            bool doFlash = lastFound != choice;
            choice = lastFound;

            if (padLock != null)
            {
                if (foundProtected != null)
                {
                    padLock.Team = foundProtected.Team;
                    padLock.FadeIn();

                    if (currentMatch.Value.PicksBans.Any(p => p.Type == ChoiceType.Pick))
                    {
                        padLock.FadeTo(0.5f);
                    }
                }
                else
                {
                    padLock.FadeOut();
                }
            }

            if (lastFound != null)
            {
                if (doFlash)
                    flash?.FadeOutFromOne(500).Loop(0, 10);

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
        }

        private partial class PadLock : Container
        {
            [Resolved]
            private OsuColour osuColour { get; set; } = null!;

            public TeamColour Team
            {
                set => lockIcon.Colour = value == TeamColour.Red ? osuColour.TeamColourRed : osuColour.TeamColourBlue;
            }

            private readonly SpriteIcon background;
            private readonly SpriteIcon lockIcon;

            public PadLock()
            {
                Children = new Drawable[]
                {
                    background = new SpriteIcon
                    {
                        Origin = Anchor.Centre,
                        Anchor = Anchor.Centre,
                        Size = new Vector2(45),
                        Icon = OsuIcon.ModBg,
                        Shadow = true,
                        Colour = Color4.LightGray
                    },
                    lockIcon = new SpriteIcon
                    {
                        Origin = Anchor.Centre,
                        Anchor = Anchor.Centre,
                        Size = new Vector2(15),
                        Icon = FontAwesome.Solid.ShieldAlt,
                        Shadow = true,
                    }
                };
            }
        }
    }
}
