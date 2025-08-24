// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Localisation;
using osu.Framework.Utils;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Tournament.IPC;
using osu.Game.Tournament.IPC.MemoryIPC;
using osu.Game.Tournament.Models;
using osuTK;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class TeamMultCoin : CompositeDrawable
    {
        private readonly TeamColour colour;
        private readonly bool flip;
        private Box multCoinBar = null!;
        private Box diffBar = null!;
        private RollingMultDiffNumberContainer diffCounter = null!;
        private RollingMultCoinContainer multCounter = null!;
        private FillFlowContainer barContainer = null!;
        private Sprite pigIcon = null!;

        private const float bar_width_when_1000_coin = 230f;
        private const float bar_steepness = 0.6f;

        // 当前队伍的分数
        private readonly BindableLong ourScore = new BindableLong();

        // 对方队伍的分数
        private readonly BindableLong oppoScore = new BindableLong();

        [Resolved]
        private MatchIPCInfo matchIpc { get; set; } = null!;

        private MemoryBasedIPCWithMatchListener ipc => (matchIpc as MemoryBasedIPCWithMatchListener)!;

        [Resolved]
        private LadderInfo ladderInfo { get; set; } = null!;

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

        public TeamMultCoin(TeamColour colour)
        {
            this.colour = colour;
            flip = colour == TeamColour.Blue;

            coin.BindValueChanged(d =>
            {
                updatePigIconState();
                triggerAnimationWhenMatchFinished(d.OldValue ?? 0, d.NewValue ?? 0);
            }, true);
            oppoCoin.BindValueChanged(_ =>
            {
                updatePigIconState();
            });
        }

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            var anchor = flip ? Anchor.BottomLeft : Anchor.BottomRight;

            Height = 22.5f;
            AutoSizeAxes = Axes.X;

            InternalChildren = new Drawable[]
            {
                new FillFlowContainer
                {
                    Height = 5f,
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
                            Spacing = new Vector2(-3, 0),
                            Children = new Drawable[]
                            {
                                multCoinBar = new Box
                                {
                                    RelativeSizeAxes = Axes.Y,
                                    Anchor = anchor,
                                    Origin = anchor,
                                    Shear = new Vector2((flip ? 1 : -1) * bar_steepness, 0),
                                    Colour = TournamentGame.GetTeamColour(colour),
                                },
                                diffBar = new Box
                                {
                                    RelativeSizeAxes = Axes.Y,
                                    Anchor = anchor,
                                    Origin = anchor,
                                    Shear = new Vector2((flip ? 1 : -1) * bar_steepness, 0),
                                    Colour = TournamentGame.GetTeamColour(colour).Lighten(0.3f),
                                }
                            }
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Y,
                            AutoSizeAxes = Axes.X,
                            Anchor = anchor,
                            Origin = anchor,
                            Children = new Drawable[]
                            {
                                new FillFlowContainer
                                {
                                    Anchor = flip ? Anchor.TopLeft : Anchor.TopRight,
                                    Origin = flip ? Anchor.TopLeft : Anchor.TopRight,
                                    AutoSizeAxes = Axes.Both,
                                    Y = -2,
                                    Direction = FillDirection.Horizontal,
                                    Children = new Drawable[]
                                    {
                                        multCounter = new RollingMultCoinContainer
                                        {
                                            Anchor = flip ? Anchor.CentreLeft : Anchor.CentreRight,
                                            Origin = flip ? Anchor.CentreLeft : Anchor.CentreRight,
                                            Margin = new MarginPadding { Horizontal = 3f },
                                        },
                                        pigIcon = new Sprite
                                        {
                                            Anchor = flip ? Anchor.CentreLeft : Anchor.CentreRight,
                                            Origin = flip ? Anchor.CentreLeft : Anchor.CentreRight,
                                            Texture = textures.Get("pig"),
                                            Size = new Vector2(13),
                                            Alpha = 0,
                                        }
                                    }
                                }
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
                    AutoSizeAxes = Axes.Y,
                    Child = diffCounter = new RollingMultDiffNumberContainer
                    {
                        Y = 20,
                        Anchor = anchor,
                        Origin = anchor,
                        Colour = TournamentGame.GetTeamColour(colour).Lighten(0.3f),
                    }
                }
            };

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

            ipc.MatchAborted += () => triggerAnimationWhenMatchFinished(coin.Value ?? 0, coin.Value ?? 0);
            ipc.MatchFinished += _ => triggerAnimationWhenMatchFinished(coin.Value ?? 0, coin.Value ?? 0);

            Scheduler.AddDelayed(() =>
            {
                var leftColor = Color4Extensions.FromHSV(RNG.NextSingle(0, 360), 1, 1);
                var rightColor = Color4Extensions.FromHSV(RNG.NextSingle(0, 360), 1, 1);

                diffBar.FadeColour(ColourInfo.GradientHorizontal(leftColor, rightColor), 1000);
            }, 1000, true);
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

            currentMatch.BindValueChanged(matchChanged);
            currentMatch.BindTo(ladderInfo.CurrentMatch);
        }

        protected override void Update()
        {
            base.Update();

            if (ipc.State.Value == TourneyState.Playing)
                updateDiff(true);
        }

        private void updateScore(bool animate, double? score = null)
        {
            score ??= coin.Value ?? 0;

            if (animate)
            {
                multCounter.Current.Value = score.Value;
                multCoinBar.ResizeWidthTo(calculateBarWidth(score.Value), 400, Easing.OutQuint);
                return;
            }

            multCounter.DisplayedCount = score.Value;
            multCounter.Current.Value = score.Value;
            multCoinBar.ResizeWidthTo(calculateBarWidth(score.Value));
        }

        private void updateDiff(bool animate, double? diff = null)
        {
            diff ??= calculateDiffFromIpc();

            if (animate)
            {
                diffCounter.Current.Value = diff.Value;
                diffBar.ResizeWidthTo(calculateBarWidth(diff.Value), 400, Easing.OutQuint);
                return;
            }

            diffCounter.DisplayedCount = diff.Value;
            diffCounter.Current.Value = diff.Value;
            diffBar.ResizeWidthTo(calculateBarWidth(diff.Value));
        }

        private static float calculateBarWidth(double coin) => (float)coin / 1000 * bar_width_when_1000_coin;

        private double calculateDiffFromIpc()
        {
            if (ourScore.Value > oppoScore.Value)
            {
                return TournamentGame.WINNER_BONUS + (isTB ? TournamentGame.EXTRA_WINNER_BONUS_TB : 0);
            }

            double diff =
                Math.Min(Math.Round((double)ourScore.Value / oppoScore.Value * 100, 2, MidpointRounding.AwayFromZero),
                    93.5);

            return double.IsNaN(diff) ? 0 : diff;
        }

        private void triggerAnimationWhenMatchFinished(double oldAmount, double newAmount) => Scheduler.AddOnce(() =>
        {
            FinishTransforms(true);

            double diff = newAmount - oldAmount;

            updateScore(false, oldAmount);
            updateDiff(true, diff);

            using (BeginDelayedSequence(2000))
            {
                updateScore(true);
                updateDiff(true, 0);
            }
        });

        private void updatePigIconState() => Scheduler.AddOnce(() =>
        {
            double diff = coin.Value - oppoCoin.Value ?? 0;

            if (diff < -35)
            {
                pigIcon.FadeIn();
            }
            else
            {
                pigIcon.FadeOut();
            }
        });

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            diffCounter.MoveToY(diffCounter.DrawWidth > barContainer.DrawWidth - 5 ? 20 : 12, 60);

            diffCounter.X = Math.Abs(Math.Max(10, barContainer.DrawWidth - diffCounter.DrawWidth)) * (flip ? 1 : -1);
        }

        private partial class RollingMultDiffNumberContainer : RollingCounter<double>
        {
            protected override double RollingDuration => 1000;

            protected override Easing RollingEasing => Easing.Out;

            protected override LocalisableString FormatCount(double count)
            {
                char sign = Math.Sign(count) == -1 ? '-' : '+';

                return $"{sign}${Math.Abs(count):N2}";
            }

            protected override OsuSpriteText CreateSpriteText() => base.CreateSpriteText().With(t =>
            {
                t.Font = OsuFont.Torus.With(size: 10, weight: FontWeight.Regular);
            });
        }

        private partial class RollingMultCoinContainer : RollingCounter<double>
        {
            protected override double RollingDuration => 1000;

            protected override Easing RollingEasing => Easing.Out;

            protected override OsuSpriteText CreateSpriteText() => base.CreateSpriteText()
                                                                       .With(s => s.Font = OsuFont.Torus.With(size: 20));

            protected override LocalisableString FormatCount(double count) => $"${count:N2}";
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
