// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Graphics;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Models;
using osuTK;

namespace osu.Game.Tournament.Screens.Gameplay.Components.MatchHeader
{
    public partial class TeamDisplay : DrawableTournamentTeam
    {
        private readonly TournamentTeam? team;
        private readonly TeamColour colour;
        private Sprite disconnectIcon = null!;

        [Resolved]
        private LadderInfo ladderInfo { get; set; } = null!;

        public TeamDisplay(TournamentTeam? team, TeamColour colour)
            : base(team)
        {
            this.team = team;
            this.colour = colour;
        }

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
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
                            new Container
                            {
                                Origin = anchor,
                                Anchor = anchor,
                                AutoSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    Flag.With(c =>
                                        {
                                            c.Masking = true;
                                            c.BorderThickness = 5;
                                            c.BorderColour = TournamentGame.GetTeamColour(colour);
                                        }
                                    ),
                                    disconnectIcon = new Sprite
                                    {
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        RelativeSizeAxes = Axes.Both,
                                        FillMode = FillMode.Fit,
                                        Texture = textures.Get("disconnect"),
                                        Alpha = 0,
                                    }
                                }
                            },
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

        #region match update

        private readonly Bindable<double?> coin = new Bindable<double?>();
        private readonly Bindable<double?> oppoCoin = new Bindable<double?>();
        private readonly Bindable<TournamentMatch?> currentMatch = new Bindable<TournamentMatch?>();
        private readonly Bindable<TournamentTeam?> currentTeam = new Bindable<TournamentTeam?>();

        private void matchChanged(ValueChangedEvent<TournamentMatch?> match)
        {
            coin.UnbindBindings();
            oppoCoin.UnbindBindings();
            currentTeam.UnbindBindings();

            Scheduler.AddOnce(updateMatch);
        }

        private void updateMatch()
        {
            var match = currentMatch.Value;

            if (match != null)
            {
                coin.BindTo(colour == TeamColour.Red ? match.Team1Coin : match.Team2Coin);
                oppoCoin.BindTo(colour == TeamColour.Blue ? match.Team1Coin : match.Team2Coin);
                currentTeam.BindTo(colour == TeamColour.Red ? match.Team1 : match.Team2);
            }
        }

        #endregion

        protected override void LoadComplete()
        {
            base.LoadComplete();

            FinishTransforms(true);
            currentMatch.BindValueChanged(matchChanged);
            currentMatch.BindTo(ladderInfo.CurrentMatch);

            coin.BindValueChanged(_ => updateDisplay());
            oppoCoin.BindValueChanged(_ => updateDisplay());
            updateDisplay();
        }

        private void updateDisplay() => Scheduler.AddOnce(() =>
        {
            double diff = coin.Value - oppoCoin.Value ?? 0;

            if (diff < -50)
            {
                disconnectIcon.FadeIn();

                if (Flag.FlagSprite != null)
                {
                    Flag.FlagSprite.Colour = OsuColour.Gray(0.5f);
                }
            }
            else
            {
                disconnectIcon.FadeOut();

                if (Flag.FlagSprite != null)
                {
                    Flag.FlagSprite.Colour = OsuColour.Gray(1f);
                }
            }
        });
    }
}
