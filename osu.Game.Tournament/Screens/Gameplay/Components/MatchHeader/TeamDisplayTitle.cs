// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
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
        private readonly TournamentSpriteText teamText;
        private readonly TeamDisplayNote teamNote;

        [UsedImplicitly]
        private Bindable<string>? acronym;

        public TeamDisplayTitle(TournamentTeam? team, TeamColour? teamColour = null)
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
                                teamText = new TournamentSpriteText
                                {
                                    Anchor = Anchor.BottomLeft,
                                    Origin = Anchor.BottomLeft,
                                    Font = OsuFont.GetFont(size: 20, weight: FontWeight.Bold),
                                    Colour = Color4.Black,
                                },
                                teamIdText = new TournamentSpriteTextWithBackground
                                {
                                    Anchor = Anchor.BottomLeft,
                                    Origin = Anchor.BottomLeft,
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
                team.FullName.BindValueChanged(name => teamText.Text = name.NewValue, true);
                team.Seed.BindValueChanged(seed => teamIdText.Text.Text = seed.NewValue, true);
                team.Color.BindValueChanged(color => teamIdText.BackgroundColor = color.NewValue, true);
                team.IdTextColor.BindValueChanged(color => teamIdText.Text.Colour = color.NewValue, true);
                team.Note.BindValueChanged(note =>
                {
                    teamNote.Text = note.NewValue;
                }, true);
            }

            this.team = team;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            if (team == null) return;

            (acronym = team.Acronym.GetBoundCopy()).BindValueChanged(_ => teamText.Text = team?.FullName.Value ?? string.Empty, true);
        }
    }
}
