// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Tournament.Models;
using osu.Game.Tournament.Screens.Gameplay.Components.MatchHeader;
using osuTK;

namespace osu.Game.Tournament.Components
{
    public partial class DrawableTeamWithPlayers : CompositeDrawable
    {
        public DrawableTeamWithPlayers(TournamentTeam? team, TeamColour colour)
        {
            AutoSizeAxes = Axes.Both;

            var players = team?.Players ?? new BindableList<TournamentUser>();

            // split the players into two even columns, favouring the first column if odd.
            int split = (int)Math.Ceiling(players.Count / 2f);

            InternalChildren = new Drawable[]
            {
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(10),
                    Children = new Drawable[]
                    {
                        new DrawableTeamTitleWithHeader(team, colour),
                        new TeamDisplayNote(TeamColour.Blue)
                        {
                            Text = team?.Note.Value ?? string.Empty,
                            Scale = new Vector2(2f),
                            ShareAnchor = Anchor.TopLeft,
                        },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Padding = new MarginPadding { Left = 10 },
                            Spacing = new Vector2(30),
                            Children = new Drawable[]
                            {
                                new FillFlowContainer
                                {
                                    Direction = FillDirection.Vertical,
                                    AutoSizeAxes = Axes.Both,
                                    Spacing = new Vector2(5),
                                    ChildrenEnumerable = players.Take(split).Select(createPlayerCard),
                                },
                                new FillFlowContainer
                                {
                                    Direction = FillDirection.Vertical,
                                    AutoSizeAxes = Axes.Both,
                                    Spacing = new Vector2(5),
                                    ChildrenEnumerable = players.Skip(split).Select(createPlayerCard),
                                },
                            }
                        },
                    }
                },
            };

            TeamPlayerCard createPlayerCard(TournamentUser p) =>
                new TeamPlayerCard(p.ToAPIUser());
        }
    }
}
