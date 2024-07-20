// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class GameplaySlot : CompositeDrawable
    {
        private readonly string modAcronym;

        public GameplaySlot(string mod)
        {
            modAcronym = mod;
            Width = 100;
            Height = 50;
            Origin = Anchor.BottomLeft;
            Anchor = Anchor.BottomLeft;
        }

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            var customTexture = textures.Get($"Slots/{modAcronym}");

            if (customTexture != null)
            {
                AddInternal(new Sprite
                {
                    FillMode = FillMode.Fit,
                    RelativeSizeAxes = Axes.Both,
                    Texture = customTexture
                });
            }
        }
    }
}
