// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Tournament.Models;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Gameplay.Components.MatchHeader
{
    public partial class MatchRoundDisplay : Container
    {
        private readonly Bindable<TournamentMatch?> currentMatch = new Bindable<TournamentMatch?>();
        private readonly Bindable<TournamentRound?> currentRound = new Bindable<TournamentRound?>();
        private readonly Bindable<string> currentRoundName = new Bindable<string>();
        private TournamentSpriteText roundName = null!;
        private TournamentSpriteText roundInfo = null!;

        private readonly BindableList<BeatmapChoice> banPicks = new BindableList<BeatmapChoice>();

        [BackgroundDependencyLoader]
        private void load(LadderInfo ladder)
        {
            RelativeSizeAxes = Axes.Y;
            AutoSizeAxes = Axes.X;

            InternalChildren = new Drawable[]
            {
                new MatchHeaderBackground(),
                roundName = new TournamentSpriteText
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Y = 3,
                    Font = OsuFont.Torus.With(size: 16, weight: FontWeight.Bold),
                    Colour = Color4.Black,
                },
                roundInfo = new TournamentSpriteText
                {
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.TopCentre,
                    Y = -3,
                    Font = OsuFont.Torus.With(size: 11),
                    Colour = Color4Extensions.FromHex("#808080"),
                },
            };

            roundName.Margin = new MarginPadding { Horizontal = 20f };

            currentMatch.BindValueChanged(matchChanged);
            currentMatch.BindTo(ladder.CurrentMatch);

            currentRoundName.BindValueChanged(_ => Scheduler.AddOnce(updateDisplay));
            banPicks.BindCollectionChanged((_, _) => Scheduler.AddOnce(updateDisplay));
        }

        private void matchChanged(ValueChangedEvent<TournamentMatch?> match)
        {
            banPicks.UnbindBindings();
            currentRoundName.UnbindBindings();

            Scheduler.AddOnce(updateMatch);
        }

        private void updateMatch()
        {
            var match = currentMatch.Value;

            if (match == null) return;

            match.StartMatch();

            currentRound.Value = match.Round.Value;
            if (currentRound.Value == null)
                return;

            banPicks.BindTo(match.PicksBans);
            currentRoundName.BindTo(currentRound.Value?.Name);
        }

        private void updateDisplay()
        {
            if (currentRound.Value == null) return;

            roundName.Text = currentRoundName.Value ?? "Unknown Round";

            roundInfo.Text = $"BO{currentRound.Value.BestOf} 回合{banPicks.Count(p => p.Type == ChoiceType.Pick)}";
        }

        public partial class MatchHeaderBackground : CompositeDrawable
        {
            private const float steepness = 0.6f;
            private const float side_length = 9f;

            public MatchHeaderBackground()
            {
                Width = 180;
                Height = 34;

                InternalChildren = new Drawable[]
                {
                    new Container
                    {
                        Padding = new MarginPadding { Horizontal = side_length },
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                Anchor = Anchor.TopLeft,
                                Origin = Anchor.TopLeft,
                                Width = 0.6f,
                                RelativeSizeAxes = Axes.Both,
                                Shear = new Vector2(-steepness, 0)
                            },
                            new Box
                            {
                                Anchor = Anchor.TopRight,
                                Origin = Anchor.TopRight,
                                Width = 0.6f,
                                RelativeSizeAxes = Axes.Both,
                                Shear = new Vector2(steepness, 0)
                            },
                        }
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 24,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                            },
                            new Box
                            {
                                Anchor = Anchor.TopLeft,
                                Origin = Anchor.TopLeft,
                                RelativeSizeAxes = Axes.Y,
                                Colour = TournamentGame.GetTeamColour(TeamColour.Red),
                                Width = side_length,
                                Shear = new Vector2(-steepness, 0)
                            },
                            new Box
                            {
                                Anchor = Anchor.TopRight,
                                Origin = Anchor.TopRight,
                                RelativeSizeAxes = Axes.Y,
                                Colour = TournamentGame.GetTeamColour(TeamColour.Blue),
                                Width = side_length,
                                Shear = new Vector2(steepness, 0)
                            },
                            new Circle
                            {
                                Anchor = Anchor.BottomCentre,
                                Origin = Anchor.BottomCentre,
                                Margin = new MarginPadding { Bottom = 3f },
                                Width = 0.5f,
                                Height = 1f,
                                Colour = Color4.Black.Opacity(0.15f),
                                RelativeSizeAxes = Axes.X,
                            }
                        }
                    }
                };
            }
        }
    }
}
