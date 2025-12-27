// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;

namespace osu.Game.Tournament.Screens.Gameplay.GameplayPlayerArea
{
    public partial class TournamentPlayerGrid : CompositeDrawable
    {
        private readonly int playerPerTeam;
        private readonly Container redTeamContainer;
        private readonly Container blueTeamContainer;

        public TournamentPlayerGrid(int playerPerTeam)
        {
            if (playerPerTeam > 4)
                throw new ArgumentException("Not Support this player count");

            this.playerPerTeam = playerPerTeam;

            InternalChildren = new Drawable[]
            {
                redTeamContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Width = 0.5f,
                },
                blueTeamContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Width = 0.5f,
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                }
            };

            setLayout(redTeamContainer);
            setLayout(blueTeamContainer);
        }

        private void setLayout(Container container)
        {
            switch (playerPerTeam)
            {
                case 1:
                    container.Children = new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Masking = true
                        }
                    };
                    break;

                case 2:
                    container.Children = new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Height = 0.5f,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Masking = true
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Height = 0.5f,
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.BottomCentre,
                            Masking = true
                        }
                    };
                    break;

                case 3:
                    container.Children = new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Width = 0.5f,
                            Height = 0.5f,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Masking = true
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Width = 0.5f,
                            Height = 0.5f,
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft,
                            Masking = true
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Width = 0.5f,
                            Height = 0.5f,
                            Anchor = Anchor.BottomRight,
                            Origin = Anchor.BottomRight,
                            Masking = true
                        },
                    };
                    break;

                case 4:
                    container.Children = new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Width = 0.5f,
                            Height = 0.5f,
                            Anchor = Anchor.TopLeft,
                            Origin = Anchor.TopLeft,
                            Masking = true
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Width = 0.5f,
                            Height = 0.5f,
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopRight,
                            Masking = true
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Width = 0.5f,
                            Height = 0.5f,
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft,
                            Masking = true
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Width = 0.5f,
                            Height = 0.5f,
                            Anchor = Anchor.BottomRight,
                            Origin = Anchor.BottomRight,
                            Masking = true
                        },
                    };
                    break;

                default:
                    throw new ArgumentException("Not Support this player count");
            }
        }

        public bool AddRedPlayer(Drawable player)
        {
            var emptyContainer = redTeamContainer.ChildrenOfType<Container>().FirstOrDefault(c => c.Children.Count == 0);
            if (emptyContainer == null)
                return false;

            emptyContainer.Add(player.With(p => p.RelativeSizeAxes = Axes.Both));
            return true;
        }

        public bool AddBluePlayer(Drawable player)
        {
            var emptyContainer = blueTeamContainer.ChildrenOfType<Container>().FirstOrDefault(c => c.Children.Count == 0);
            if (emptyContainer == null)
                return false;

            emptyContainer.Add(player.With(p => p.RelativeSizeAxes = Axes.Both));
            return true;
        }
    }
}
