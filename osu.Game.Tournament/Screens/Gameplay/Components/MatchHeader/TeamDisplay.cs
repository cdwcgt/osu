// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Models;
using osuTK;

namespace osu.Game.Tournament.Screens.Gameplay.Components.MatchHeader
{
    public partial class TeamDisplay : DrawableTournamentTeam
    {
        public TeamDisplay(TournamentTeam? team, TeamColour colour)
            : base(team)
        {
            AutoSizeAxes = Axes.Both;

            Anchor = Anchor.CentreLeft;
            Origin = Anchor.CentreLeft;

            var anchor = colour == TeamColour.Red ? Anchor.CentreLeft : Anchor.CentreRight;

            Flag.RelativeSizeAxes = Axes.None;
            Flag.Scale = new Vector2(0.8f);
            Flag.Origin = anchor;
            Flag.Anchor = anchor;

            InternalChild = new Container
            {
                AutoSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(10, 0),
                        Children = new Drawable[]
                        {
                            Flag.With(c =>
                            {
                                c.Masking = true;
                                c.BorderThickness = 5;
                                c.BorderColour = TournamentGame.GetTeamColour(colour);
                            }),
                            new DrawableTeamHeader(colour)
                            {
                                Origin = anchor,
                                Anchor = anchor,
                                Text = { Font = OsuFont.Torus.With(size: 24) }
                            },
                            new FillFlowContainer
                            {
                                Height = 24f,
                                AutoSizeAxes = Axes.X,
                                Direction = FillDirection.Horizontal,
                                Origin = anchor,
                                Anchor = anchor,
                                Children = new Drawable[]
                                {
                                    new TeamDisplayTitle(team, colour)
                                    {
                                        RelativeSizeAxes = Axes.Y,
                                        Origin = anchor,
                                        Anchor = anchor,
                                    },
                                }
                            },
                        }
                    },
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            FinishTransforms(true);
        }
    }
}
