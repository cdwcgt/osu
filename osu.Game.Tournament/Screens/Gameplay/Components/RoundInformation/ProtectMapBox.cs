// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Tournament.Models;
using osuTK;

namespace osu.Game.Tournament.Screens.Gameplay.Components.RoundInformation
{
    public partial class ProtectMapBox : MapBox
    {
        public ProtectMapBox(BeatmapChoice pickColor)
            : base(pickColor)
        {
        }

        protected override Drawable CreateCenterLine(TeamColour color)
        {
            var protectedIcon = textures.Get("round-information/protect-icon");

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
                        Texture = protectedIcon,
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
