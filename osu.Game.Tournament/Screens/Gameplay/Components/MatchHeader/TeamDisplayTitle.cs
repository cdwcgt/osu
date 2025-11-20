// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Models;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Gameplay.Components.MatchHeader
{
    public partial class TeamDisplayTitle : CompositeDrawable
    {
        private readonly TournamentTeam? team;
        private readonly TeamColour? teamColour;
        private readonly BindableList<BeatmapChoice> picksBans = new BindableList<BeatmapChoice>();

        private TournamentSpriteText teamText = null!;
        private TeamDisplayNote teamNote = null!;
        private Container teamTextContainer = null!;
        private SpriteIcon arrowIcon = null!;

        public TeamDisplayTitle(TournamentTeam? team, TeamColour teamColour)
        {
            this.team = team;
            this.teamColour = teamColour;
        }

        [BackgroundDependencyLoader]
        private void load(LadderInfo ladder, MatchHeader header)
        {
            TournamentSpriteTextWithBackground teamIdText;
            var anchor = teamColour == TeamColour.Blue ? Anchor.CentreRight : Anchor.CentreLeft;

            AutoSizeAxes = Axes.X;
            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.White
                },
                new Box
                {
                    Anchor = anchor,
                    Origin = anchor,
                    RelativeSizeAxes = Axes.Y,
                    Width = 10,
                    Colour = teamColour != null ? TournamentGame.GetTeamColour(teamColour.Value) : Color4.Transparent
                },
                new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    AutoSizeAxes = Axes.X,
                    RelativeSizeAxes = Axes.Y,
                    Padding = new MarginPadding
                    {
                        Left = teamColour == TeamColour.Blue ? 0 : 10,
                        Right = teamColour == TeamColour.Blue ? 10 : 0,
                    },
                    Children = new Drawable[]
                    {
                        new FillFlowContainer
                        {
                            Anchor = anchor,
                            Origin = anchor,
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Margin = teamColour == TeamColour.Blue
                                ? new MarginPadding { Left = 10, Right = 10 + 5 }
                                : new MarginPadding { Left = 10 + 5, Right = 10 },
                            Spacing = new Vector2(2, 0),
                            Children = new Drawable[]
                            {
                                new Container
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Anchor = anchor,
                                    Origin = anchor,
                                    Child = arrowIcon = new SpriteIcon
                                    {
                                        Size = new Vector2(20),
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Icon = FontAwesome.Solid.Play,
                                        Colour = Color4.Black,
                                        Rotation = teamColour == TeamColour.Blue ? 180 : 0,
                                        Alpha = 0,
                                        AlwaysPresent = true,
                                    },
                                },
                                teamTextContainer = new Container
                                {
                                    Anchor = anchor,
                                    Origin = anchor,
                                    AutoSizeAxes = Axes.Both,
                                    Child = teamText = new TournamentSpriteText
                                    {
                                        Anchor = anchor,
                                        Origin = anchor,
                                        Font = OsuFont.GetFont(size: 20, weight: FontWeight.Bold),
                                        Colour = Color4.Black,
                                    },
                                },
                                teamIdText = new TournamentSpriteTextWithBackground
                                {
                                    Anchor = teamColour == TeamColour.Blue ? Anchor.BottomRight : Anchor.BottomLeft,
                                    Origin = teamColour == TeamColour.Blue ? Anchor.BottomRight : Anchor.BottomLeft,
                                    Text = { Font = OsuFont.GetFont(size: 13) },
                                    Margin = new MarginPadding { Bottom = 2f }
                                },
                            }
                        },
                    }
                },
                teamNote = new TeamDisplayNote(teamColour ?? TeamColour.Red)
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.BottomCentre
                },
            };

            teamIdText.Text.Padding = new MarginPadding { Horizontal = 5 };

            if (team != null)
            {
                team.FullName.BindValueChanged(name =>
                {
                    teamText.Text = name.NewValue;

                    if (teamColour != null)
                        Scheduler.AddOnce(() => header.TextWidthEachTeam[teamColour.Value] = teamText.Width);
                }, true);
                team.Seed.BindValueChanged(seed => teamIdText.Text.Text = seed.NewValue, true);
                team.Color.BindValueChanged(color => teamIdText.BackgroundColor = color.NewValue, true);
                team.IdTextColor.BindValueChanged(color => teamIdText.Text.Colour = color.NewValue, true);
                team.Note.BindValueChanged(note =>
                {
                    teamNote.Text = note.NewValue;
                }, true);

                if (teamColour != null)
                {
                    header.TextWidthEachTeam.BindCollectionChanged((_, _) =>
                        teamTextContainer.Padding = new MarginPadding
                        {
                            Horizontal = (header.TextWidthEachTeam.Max(w => w.Value) - teamText.Width) / 2
                        });
                }

                picksBans.BindCollectionChanged((_, _) => updatePick());

                if (ladder.CurrentMatch.Value != null)
                    picksBans.BindTo(ladder.CurrentMatch.Value.PicksBans);
            }
        }

        private void updatePick()
        {
            var lastChoice = picksBans.LastOrDefault();

            if (lastChoice?.Team == teamColour && lastChoice?.Type == ChoiceType.Pick)
            {
                arrowIcon.FadeIn(100);
            }
            else
            {
                arrowIcon.FadeOut(100);
            }
        }
    }
}
