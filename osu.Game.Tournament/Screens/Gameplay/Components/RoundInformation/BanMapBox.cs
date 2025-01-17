// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Tournament.Models;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Gameplay.Components.RoundInformation
{
    public partial class BanMapBox : MapBox
    {
        public BanMapBox(BeatmapChoice pickColor)
            : base(pickColor)
        {
        }

        protected override void UpdateStatus() => AddModContent(Color4Extensions.FromHex("#525252"), new Color4(229, 229, 229, 255));

        protected override Drawable CreateCenterLine(TeamColour color)
        {
            var banIcon = textures.Get("round-information/ban-icon");

            return new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 4f,
                Children = new Drawable[]
                {
                    CreateCenterLineBox(color),
                    new Circle
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(14f),
                        Blending = new BlendingParameters
                        {
                            RGBEquation = BlendingEquation.Add,
                            Source = BlendingType.Zero,
                            Destination = BlendingType.One,
                            AlphaEquation = BlendingEquation.Add,
                            SourceAlpha = BlendingType.Zero,
                            DestinationAlpha = BlendingType.OneMinusSrcAlpha
                        },
                    },
                    new Sprite
                    {
                        Texture = banIcon,
                        Colour = GetColorFromTeamColor(color),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        EdgeSmoothness = Vector2.Zero
                    },
                }
            };
        }
    }
}
