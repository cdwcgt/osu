// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Tournament.Models;
using osu.Game.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class TeamCoinDIffDisplay : CompositeDrawable
    {
        private readonly Bindable<double?> team1TeamCoin = new Bindable<double?>();
        private readonly Bindable<double?> team2TeamCoin = new Bindable<double?>();
        private readonly RollingMultDiffNumberContainer coinDiffContainer;
        private readonly Box background;

        private readonly Container leftIconContainer;
        private readonly Container rightIconContainer;

        [Resolved]
        private TextureStore store { get; set; } = null!;

        public TeamCoinDIffDisplay()
        {
            AutoSizeAxes = Axes.Both;

            InternalChild = new Container
            {
                CornerRadius = 10f,
                Size = new Vector2(60, 15),
                Masking = true,
                Children = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4Extensions.FromHex("383838")
                    },
                    new FillFlowContainer
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        LayoutDuration = 200,
                        LayoutEasing = Easing.InOutQuint,
                        Children = new Drawable[]
                        {
                            leftIconContainer = new Container
                            {
                                Masking = true,
                                AutoSizeAxes = Axes.X,
                                RelativeSizeAxes = Axes.Y,
                                AutoSizeDuration = 200,
                                AutoSizeEasing = Easing.InOutQuint,
                                Child = new SpriteIcon
                                {
                                    Size = new Vector2(11),
                                    Icon = FontAwesome.Solid.CaretLeft
                                }
                            },
                            coinDiffContainer = new RollingMultDiffNumberContainer(),
                            rightIconContainer = new Container
                            {
                                Masking = true,
                                AutoSizeAxes = Axes.X,
                                RelativeSizeAxes = Axes.Y,
                                AutoSizeDuration = 200,
                                AutoSizeEasing = Easing.InOutQuint,
                                Child = new SpriteIcon
                                {
                                    Size = new Vector2(11),
                                    Icon = FontAwesome.Solid.CaretLeft
                                },
                            }
                        }
                    },
                }
            };
        }

        private partial class RollingMultDiffNumberContainer : RollingCounter<double>
        {
            protected override double RollingDuration => 1000;

            protected override OsuSpriteText CreateSpriteText() => new OsuSpriteText
            {
                Font = OsuFont.Torus.With(size: 20),
            };

            protected override LocalisableString FormatCount(double count)
            {
                return $"{Math.Abs(count):N2}";
            }
        }

        [BackgroundDependencyLoader]
        private void load(LadderInfo ladder)
        {
            var currentMatch = ladder.CurrentMatch.Value;

            if (currentMatch == null)
                return;

            team1TeamCoin.BindTo(currentMatch.Team1Coin);
            team2TeamCoin.BindTo(currentMatch.Team2Coin);

            team1TeamCoin.BindValueChanged(_ => updateDisplay(), true);
            team2TeamCoin.BindValueChanged(_ => updateDisplay(), true);
        }

        private const double first_warning_coin = -22.5;
        private const double second_warning_coin = -45;
        private const double third_warning_coin = -90;

        private void updateDisplay() => Scheduler.AddOnce(() =>
        {
            FinishTransforms(true);

            double diff = (team1TeamCoin.Value ?? 0) - (team2TeamCoin.Value ?? 0);

            if (diff < 0)
            {
                leftIconContainer.AutoSizeAxes = Axes.None;
                rightIconContainer.AutoSizeAxes = Axes.X;
            }
            else
            {
                leftIconContainer.AutoSizeAxes = Axes.X;
                rightIconContainer.AutoSizeAxes = Axes.None;
            }

            coinDiffContainer.Current.Value = Math.Abs(diff);
        });

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
