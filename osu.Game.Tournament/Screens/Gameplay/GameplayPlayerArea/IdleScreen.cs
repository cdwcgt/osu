// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Online.Multiplayer;
using osu.Game.Screens;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.Screens.Gameplay.GameplayPlayerArea
{
    public partial class IdleScreen : OsuScreen
    {
        [Resolved]
        private LadderInfo ladder { get; set; } = null!;

        [Resolved]
        private MultiplayerClient client { get; set; } = null!;

        private Bindable<int> playerPerTeam = new Bindable<int>();
        private readonly Container redTeamContainer;
        private readonly Container blueTeamContainer;

        public IdleScreen()
        {
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
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            playerPerTeam.BindValueChanged(_ =>
            {
                setLayout(redTeamContainer);
                setLayout(blueTeamContainer);

                int redIndex = 0;
                int blueIndex = 0;

                foreach (var container in redTeamContainer.Children.OfType<Container>())
                {
                    var stack = new OsuScreenStack();
                    container.Child = stack;
                    stack.Push(new IdlePlayerScreen(redIndex++, TeamColour.Red));
                }

                foreach (var container in blueTeamContainer.Children.OfType<Container>())
                {
                    var stack = new OsuScreenStack();
                    container.Child = stack;
                    stack.Push(new IdlePlayerScreen(blueIndex++, TeamColour.Blue));
                }
            });
            playerPerTeam.BindTo(ladder.PlayersPerTeam);
        }

        private void setLayout(Container container)
        {
            switch (playerPerTeam.Value)
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
    }
}
