// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Tournament.Models;
using osuTK;

namespace osu.Game.Tournament.Components
{
    public partial class TournamentProtectIcon : CompositeDrawable
    {
        private readonly TeamColour color;

        public TournamentProtectIcon(TeamColour colour)
        {
            this.color = colour;
        }

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            var customTexture = textures.Get($"Protect/team-{(color == TeamColour.Red ? "red" : "blue")}");

            if (customTexture != null)
            {
                AddInternal(new Sprite
                {
                    FillMode = FillMode.Fit,
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Texture = customTexture
                });
                return;
            }

            AddInternal(new SpriteIcon
            {
                Origin = Anchor.Centre,
                Anchor = Anchor.Centre,
                Size = new Vector2(45),
                Icon = FontAwesome.Solid.Question
            });
        }
    }
}
