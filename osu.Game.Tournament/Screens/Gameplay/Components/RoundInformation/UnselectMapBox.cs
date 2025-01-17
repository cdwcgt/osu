// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Tournament.Models;
using osuTK;

namespace osu.Game.Tournament.Screens.Gameplay.Components.RoundInformation
{
    public partial class UnselectMapBox : MapBox
    {
        public UnselectMapBox()
            : base(new BeatmapChoice())
        {
        }

        protected override Drawable CreateCenterLine(TeamColour color)
        {
            var lineContainer = new Container
            {
                RelativeSizeAxes = Axes.X,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Height = 4,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4Extensions.FromHex("#525252")
                    },
                    new Circle
                    {
                        Size = new Vector2(12),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        BorderColour = Colour4.White,
                        Blending = new BlendingParameters
                        {
                            RGBEquation = BlendingEquation.Add,
                            Source = BlendingType.Zero,
                            Destination = BlendingType.One,
                            AlphaEquation = BlendingEquation.Add,
                            SourceAlpha = BlendingType.Zero,
                            DestinationAlpha = BlendingType.OneMinusSrcAlpha
                        },
                    }
                }
            };

            return new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    lineContainer,
                    new Circle
                    {
                        Size = new Vector2(8),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Colour = Color4Extensions.FromHex("#525252")
                    }
                }
            };
        }

        protected override void UpdateStatus()
        {
        }
    }
}
