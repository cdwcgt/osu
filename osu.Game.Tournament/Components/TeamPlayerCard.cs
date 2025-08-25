// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Users;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Components
{
    public partial class TeamPlayerCard : UserPanel
    {
        public TeamPlayerCard(APIUser user)
            : base(user)
        {
            Masking = true;
            CornerRadius = 5;
            Size = new Vector2(190, 40);
        }

        protected override Drawable CreateLayout()
        {
            return new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new FillFlowContainer
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Horizontal,
                        Margin = new MarginPadding { Left = 8 },
                        Spacing = new Vector2(5, 0),
                        Children = new Drawable[]
                        {
                            new Container
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                BorderThickness = 2,
                                BorderColour = Color4.Black,
                                Masking = true,
                                CornerRadius = 3,
                                AutoSizeAxes = Axes.Both,
                                Child = CreateAvatar().With(a =>
                                {
                                    a.Size = new Vector2(26);
                                })
                            },
                            new Container
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                RelativeSizeAxes = Axes.Y,
                                AutoSizeAxes = Axes.X,
                                Children = new Drawable[]
                                {
                                    CreateUsername().With(f =>
                                    {
                                        f.Font = OsuFont.Torus.With(weight: FontWeight.Bold, size: 15f);
                                    }),
                                    new TournamentSpriteText
                                    {
                                        Anchor = Anchor.BottomLeft,
                                        Origin = Anchor.BottomLeft,
                                        Font = OsuFont.Torus.With(weight: FontWeight.Regular, size: 12f),
                                        Text = $"{User.Statistics.PP ?? 0}pp"
                                    }
                                }
                            }
                        },
                    },
                    new TournamentSpriteText
                    {
                        Anchor = Anchor.BottomRight,
                        Origin = Anchor.BottomRight,
                        Margin = new MarginPadding { Right = 7 },
                        Font = OsuFont.Torus.With(weight: FontWeight.Bold, size: 12f),
                        Text = $"{User.Statistics.GlobalRank?.ToLocalisableString("\\##,##0")}"
                    }
                }
            };
        }

        protected override Drawable? CreateBackground() => base.CreateBackground()?.With(b =>
        {
            b.Colour = OsuColour.Gray(0.5f);
        });
    }
}
