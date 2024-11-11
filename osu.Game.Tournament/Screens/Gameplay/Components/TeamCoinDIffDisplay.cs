// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Threading;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Models;
using osu.Game.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class TeamCoinDIffDisplay : CompositeDrawable
    {
        private readonly TeamColour teamColour;

        private readonly Bindable<double?> currentTeamCoin = new Bindable<double?>();
        private readonly Bindable<double?> opponentTeamCoin = new Bindable<double?>();
        private readonly RollingSignNumberContainer coinDiffContainer;
        private readonly Box background;
        private readonly Container iconContainer;

        [Resolved]
        private TextureStore store { get; set; } = null!;

        public TeamCoinDIffDisplay(TeamColour colour)
        {
            teamColour = colour;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            InternalChild = new FillFlowContainer
            {
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Children = new Drawable[]
                {
                    new Container
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        CornerRadius = 10f,
                        Size = new Vector2(60, 20),
                        Masking = true,
                        Children = new Drawable[]
                        {
                            background = new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4Extensions.FromHex("919191")
                            },
                            coinDiffContainer = new RollingMultDiffNumberContainer
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre
                            }
                        }
                    },
                    iconContainer = new Container
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Height = 25f,
                        AutoSizeAxes = Axes.Both,
                    }
                }
            };
        }

        private partial class RollingMultDiffNumberContainer : RollingSignNumberContainer
        {
            protected override double RollingDuration => 1000;
        }

        [BackgroundDependencyLoader]
        private void load(LadderInfo ladder)
        {
            var currentMatch = ladder.CurrentMatch.Value;

            if (currentMatch == null)
                return;

            currentTeamCoin.BindTo(teamColour == TeamColour.Red ? currentMatch.Team1Coin : currentMatch.Team2Coin);
            opponentTeamCoin.BindTo(teamColour == TeamColour.Blue ? currentMatch.Team1Coin : currentMatch.Team2Coin);

            currentTeamCoin.BindValueChanged(_ => updateDisplay(), true);
            opponentTeamCoin.BindValueChanged(_ => updateDisplay(), true);
        }

        private ScheduledDelegate? blinkScheduledDelegate;
        private ScheduledDelegate? changeIconScheduledDelegate;

        private const double first_warning_coin = -22.5;
        private const double second_warning_coin = -45;
        private const double third_warning_coin = -90;

        private void updateDisplay() => Scheduler.AddOnce(() =>
        {
            coinDiffContainer.FinishTransforms();

            double diff = (currentTeamCoin.Value ?? 0) - (opponentTeamCoin.Value ?? 0);

            using (BeginDelayedSequence(2000))
            {
                coinDiffContainer.Current.Value = diff;
                background.FadeColour(getColor(diff), 500, Easing.OutQuint);
            }

            changeIconScheduledDelegate?.Cancel();
            changeIconScheduledDelegate = Scheduler.AddDelayed(() =>
            {
                if (diff > first_warning_coin)
                {
                    iconContainer.FadeOut(200, Easing.OutQuint);
                }

                iconContainer.Clear();
                iconContainer.Child = getIconByDiff(diff);

                blinkScheduledDelegate?.Cancel();

                if (diff > first_warning_coin)
                {
                    return;
                }

                int blinkTime = getBlinkTime(diff);

                blinkOnce(blinkTime);

                blinkScheduledDelegate = Scheduler.AddDelayed(() =>
                {
                    blinkOnce(blinkTime);
                }, blinkTime * 2, true);
            }, 2500);
        });

        private void blinkOnce(int timeInMs)
        {
            using (BeginDelayedSequence(0))
            {
                iconContainer.FadeOut(timeInMs, Easing.OutQuint);
            }

            using (BeginDelayedSequence(timeInMs))
            {
                iconContainer.FadeIn(timeInMs, Easing.OutQuint);
            }
        }

        private int getBlinkTime(double diff)
        {
            return diff <= third_warning_coin ? 500 :
                diff <= second_warning_coin ? 650 :
                diff <= first_warning_coin ? 800 : 0;
        }

        private Color4 getColor(double diff) => ColourUtils.SampleFromLinearGradient(new[]
        {
            (-90f, Color4Extensions.FromHex("383838")),
            (-45f, Color4Extensions.FromHex("5E5E5E")),
            (0f, Color4Extensions.FromHex("919191")),
            (20f, Color4Extensions.FromHex("cc9f0c")),
            (90f, Color4Extensions.FromHex("FFC300")),
        }, (float)diff);

        private Drawable getIconByDiff(double diff)
        {
            return diff <= third_warning_coin ? getIcon("MC") :
                diff <= second_warning_coin ? getIcon("MB") :
                diff <= first_warning_coin ? getIcon("MA") : new Container();
        }

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
