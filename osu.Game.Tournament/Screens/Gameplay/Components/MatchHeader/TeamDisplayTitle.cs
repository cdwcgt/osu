// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Graphics;
using osu.Game.Tournament.Models;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Gameplay.Components.MatchHeader
{
    public partial class TeamDisplayTitle : CompositeDrawable
    {
        private readonly TournamentTeam? team;
        private readonly TeamColour teamColour;
        private readonly TournamentSpriteText teamText;

        private readonly Container stateIconContainer;
        private readonly TournamentSpriteText teamIdText;
        private readonly Box teamIdBackground;

        [Resolved]
        private TextureStore store { get; set; } = null!;

        public TeamDisplayTitle(TournamentTeam? team, TeamColour teamColour)
        {
            var anchor = teamColour == TeamColour.Blue ? Anchor.CentreRight : Anchor.CentreLeft;

            AutoSizeAxes = Axes.X;

            InternalChildren = new Drawable[]
            {
                new Container
                {
                    AutoSizeAxes = Axes.X,
                    RelativeSizeAxes = Axes.Y,
                    Children = new Drawable[]
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
                            Colour = TournamentGame.GetTeamColour(teamColour)
                        },
                        new FillFlowContainer
                        {
                            Anchor = anchor,
                            Origin = anchor,
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Margin = teamColour == TeamColour.Blue
                                ? new MarginPadding { Left = 10, Right = 10 + 15 }
                                : new MarginPadding { Left = 10 + 15, Right = 10 },
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
                                new FillFlowContainer
                                {
                                    Anchor = Anchor.BottomLeft,
                                    Origin = Anchor.BottomLeft,
                                    RelativeSizeAxes = Axes.Y,
                                    AutoSizeAxes = Axes.X,
                                    Direction = FillDirection.Vertical,
                                    Margin = new MarginPadding { Bottom = 2f },
                                    Children = new Drawable[]
                                    {
                                        new Container
                                        {
                                            Anchor = Anchor.BottomLeft,
                                            Origin = Anchor.BottomLeft,
                                            Size = new Vector2(16, 11),
                                            Children = new Drawable[]
                                            {
                                                teamIdBackground = new Box
                                                {
                                                    Colour = TournamentGame.ELEMENT_BACKGROUND_COLOUR,
                                                    RelativeSizeAxes = Axes.Both,
                                                },
                                                teamIdText = new TournamentSpriteText
                                                {
                                                    Anchor = Anchor.Centre,
                                                    Origin = Anchor.Centre,
                                                    Colour = TournamentGame.ELEMENT_FOREGROUND_COLOUR,
                                                    Font = OsuFont.Torus.With(size: 11, weight: FontWeight.Bold),
                                                }
                                            }
                                        },
                                        stateIconContainer = new Container
                                        {
                                            Height = 10f,
                                            Anchor = Anchor.BottomLeft,
                                            Origin = Anchor.BottomLeft,
                                        },
                                    }
                                },
                            }
                        }
                    }
                },
                teamNote = new TeamDisplayNote(teamColour)
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.BottomLeft
                }
            };

            this.team = team;
            this.teamColour = teamColour;
        }

        private readonly Bindable<double?> currentTeamCoin = new Bindable<double?>();
        private readonly Bindable<double?> opponentTeamCoin = new Bindable<double?>();
        private readonly TeamDisplayNote teamNote;

        [BackgroundDependencyLoader]
        private void load(LadderInfo ladder)
        {
            if (team != null)
            {
                team.FullName.BindValueChanged(name => teamText.Text = name.NewValue, true);
                team.Seed.BindValueChanged(seed => teamIdText.Text = seed.NewValue, true);
                team.Color.BindValueChanged(color => teamIdBackground.Colour = color.NewValue, true);
                team.IdTextColor.BindValueChanged(color => teamIdText.Colour = color.NewValue, true);
                team.Note.BindValueChanged(note =>
                {
                    if (string.IsNullOrEmpty(note.NewValue))
                        teamNote.FadeOut();

                    teamNote.FadeIn();
                    teamNote.Text = note.NewValue;
                }, true);
            }

            var currentMatch = ladder.CurrentMatch.Value;

            if (currentMatch == null)
                return;

            currentTeamCoin.BindTo(teamColour == TeamColour.Red ? currentMatch.Team1Coin : currentMatch.Team2Coin);
            opponentTeamCoin.BindTo(teamColour == TeamColour.Blue ? currentMatch.Team1Coin : currentMatch.Team2Coin);

            currentTeamCoin.BindValueChanged(_ => updateDisplay(), true);
            opponentTeamCoin.BindValueChanged(_ => updateDisplay(), true);
        }

        private const double first_warning_coin = -22.5;
        private const double second_warning_coin = -45;
        private const double third_warning_coin = -90;

        private Drawable getIconByDiff(double diff)
        {
            return diff <= third_warning_coin ? getIcon("MC") :
                diff <= second_warning_coin ? getIcon("MB") :
                diff <= first_warning_coin ? getIcon("MA") : Empty();
        }

        private void updateDisplay() => Scheduler.AddOnce(() =>
        {
            double diff = (currentTeamCoin.Value ?? 0) - (opponentTeamCoin.Value ?? 0);
            stateIconContainer.Child = getIconByDiff(diff);
        });

        private Drawable getIcon(string icon) => new Container
        {
            Size = new Vector2(60, 20),
            Child = new Sprite
            {
                RelativeSizeAxes = Axes.Both,
                Texture = store.Get(icon)
            },
        };
    }
}
