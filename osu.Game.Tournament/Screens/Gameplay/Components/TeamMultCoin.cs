// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.IPC;
using osu.Game.Tournament.Models;
using osuTK;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class TeamMultCoin : CompositeDrawable
    {
        private readonly Bindable<double?> coin = new Bindable<double?>();
        private readonly TeamColour colour;
        private readonly bool flip;
        private readonly Box multCoinBar;
        private readonly Box diffBar;
        private readonly RollingMultDiffNumberContainer diffCounter;
        private readonly RollingMultCoinContainer multCounter;
        private readonly FillFlowContainer barContainer;

        private const float bar_width_when_1000coin = 230f;

        // 当前队伍的分数
        private readonly BindableLong ourScore = new BindableLong();

        // 对方队伍的分数
        private readonly BindableLong oppoScore = new BindableLong();

        [Resolved]
        private MatchIPCInfo ipc { get; set; } = null!;

        [Resolved]
        protected LadderInfo LadderInfo { get; private set; } = null!;

        private readonly Bindable<TournamentMatch?> currentMatch = new Bindable<TournamentMatch?>();

        private bool isTB
        {
            get
            {
                var lastPick = currentMatch.Value?.PicksBans.LastOrDefault();
                var tbMap = currentMatch.Value?.Round.Value?.Beatmaps.FirstOrDefault(map => map.Mods == "TB");

                if (lastPick == null || tbMap == null)
                    return false;

                return lastPick.Type == ChoiceType.Pick && lastPick.BeatmapID == tbMap.ID;
            }
        }

        public TeamMultCoin(Bindable<double?> coin, TeamColour colour)
        {
            this.coin.BindTo(coin);
            this.colour = colour;
            flip = colour == TeamColour.Blue;
            var anchor = flip ? Anchor.BottomRight : Anchor.BottomLeft;

            Height = 22.5f;
            AutoSizeAxes = Axes.X;

            InternalChildren = new Drawable[]
            {
                new FillFlowContainer
                {
                    Height = 10f,
                    AutoSizeAxes = Axes.X,
                    Direction = FillDirection.Horizontal,
                    Anchor = anchor,
                    Origin = anchor,
                    Spacing = new Vector2(5, 0),
                    Children = new Drawable[]
                    {
                        barContainer = new FillFlowContainer
                        {
                            Name = "barContainer",
                            RelativeSizeAxes = Axes.Y,
                            AutoSizeAxes = Axes.X,
                            Anchor = anchor,
                            Origin = anchor,
                            Children = new Drawable[]
                            {
                                multCoinBar = new Box
                                {
                                    RelativeSizeAxes = Axes.Y,
                                    Anchor = anchor,
                                    Origin = anchor,
                                    Colour = TournamentGame.GetTeamColour(colour),
                                },
                                diffBar = new Box
                                {
                                    RelativeSizeAxes = Axes.Y,
                                    Anchor = anchor,
                                    Origin = anchor,
                                    Colour = Color4Extensions.FromHex("EBBC23"),
                                }
                            }
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Y,
                            AutoSizeAxes = Axes.X,
                            Anchor = anchor,
                            Origin = anchor,
                            Child = diffCounter = new RollingMultDiffNumberContainer
                            {
                                Anchor = flip ? Anchor.CentreRight : Anchor.CentreLeft,
                                Origin = Anchor = flip ? Anchor.CentreRight : Anchor.CentreLeft,
                            }
                        }
                    }
                },
                new Container
                {
                    Name = "multiCoinCounter",
                    Anchor = anchor,
                    Origin = anchor,
                    RelativeSizeAxes = Axes.X,
                    Height = 23.5f - 10f,
                    Child = multCounter = new RollingMultCoinContainer
                    {
                        Anchor = anchor,
                        Origin = anchor,
                        Margin = new MarginPadding { Bottom = 10f }
                    }
                }
            };

            coin.BindValueChanged(d =>
            {
                Scheduler.AddOnce(() => triggerAnimation(d.OldValue ?? 0, d.NewValue ?? 0));
            }, true);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            if (colour == TeamColour.Red)
            {
                ourScore.BindTo(ipc.Score1);
                oppoScore.BindTo(ipc.Score2);
            }
            else
            {
                ourScore.BindTo(ipc.Score2);
                oppoScore.BindTo(ipc.Score1);
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            currentMatch.BindTo(LadderInfo.CurrentMatch);
        }

        protected override void Update()
        {
            base.Update();

            if (ipc.State.Value == TourneyState.Playing)
                updateScore();
        }

        private void updateScore(bool keepDiff = false)
        {
            double multcoin = coin.Value ?? 0;
            double diff = 0;

            multCounter.Current.Value = multcoin;
            multCoinBar.ResizeWidthTo(calculateBarWidth(multcoin), 400, Easing.OutQuint);

            if (ipc.State.Value == TourneyState.Playing)
            {
                diff = calculateDiff();
            }

            diffBar.ResizeWidthTo(calculateBarWidth(diff), 400, Easing.OutQuint);

            if (!keepDiff)
                diffCounter.Current.Value = diff;
        }

        private static float calculateBarWidth(double coin) => (float)coin / 1000 * bar_width_when_1000coin;

        private double calculateDiff()
        {
            if (ourScore.Value > oppoScore.Value)
            {
                return GameplayScreen.WINNER_BONUS + (isTB ? GameplayScreen.EXTRA_WINNER_BONUS_TB : 0);
            }

            double diff = (double)ourScore.Value / oppoScore.Value * 100;

            return double.IsNaN(diff) ? 0 : diff;
        }

        private void triggerAnimation(double oldAmount, double newAmount)
        {
            FinishTransforms(true);
            double diff = newAmount - oldAmount;

            multCoinBar.ResizeWidthTo(calculateBarWidth(oldAmount), 400, Easing.OutQuint);
            diffBar.ResizeWidthTo(calculateBarWidth(diff), 400, Easing.OutQuint);
            multCounter.DisplayedCount = oldAmount;
            multCounter.Current.Value = oldAmount;

            using (BeginDelayedSequence(2000))
            {
                multCounter.Current.Value = newAmount;
                updateScore(true);
            }
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            multCounter.X = Math.Max(5, barContainer.DrawWidth - multCounter.DrawWidth) * (flip ? -1 : 1);
        }

        private partial class RollingMultDiffNumberContainer : RollingSignNumberContainer
        {
            protected override double RollingDuration => 1000;

            protected override OsuSpriteText CreateSpriteText() => base.CreateSpriteText().With(t =>
            {
                t.Font = t.Font.With(size: 15);
            });
        }

        private partial class RollingMultCoinContainer : RollingCounter<double>
        {
            protected override double RollingDuration => 1000;

            protected override IHasText CreateText()
            {
                return new MultCoinTextContainer();
            }

            protected override LocalisableString FormatCount(double count) => count.ToString("N2");
        }

        private partial class MultCoinTextContainer : FillFlowContainer, IHasText
        {
            public LocalisableString Text
            {
                get => wholePart.Text;
                set
                {
                    string[] split = value.ToString().Split(".");

                    wholePart.Text = split[0] + ".";
                    fractionPart.Text = split[1];
                }
            }

            private readonly SpriteText wholePart;
            private readonly SpriteText fractionPart;

            public MultCoinTextContainer()
            {
                AutoSizeAxes = Axes.X;
                RelativeSizeAxes = Axes.Y;

                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        Font = OsuFont.Torus.With(size: 27),
                        Text = "$"
                    },
                    wholePart = new OsuSpriteText
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        Font = OsuFont.Torus.With(size: 25),
                    },
                    fractionPart = new OsuSpriteText
                    {
                        Margin = new MarginPadding { Bottom = 1f },
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        Font = OsuFont.Torus.With(size: 20),
                    }
                };
            }
        }
    }
}
