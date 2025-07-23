// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;

namespace osu.Game.Tournament.Components
{
    public partial class TournamentSpriteTextWithBackground : CompositeDrawable
    {
        public readonly TournamentSpriteText Text;

        protected readonly Box Background;

        public ColourInfo BackgroundColor
        {
            get => Background.Colour;
            set => Background.Colour = value;
        }

        public ColourInfo TextColor
        {
            get => Text.Colour;
            set => Text.Colour = value;
        }

        public TournamentSpriteTextWithBackground(string text = "", Action<SpriteText>? fontAdjustAction = null)
        {
            AutoSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                Background = new Box
                {
                    Colour = TournamentGame.ELEMENT_BACKGROUND_COLOUR,
                    RelativeSizeAxes = Axes.Both,
                },
                Text = new TournamentSpriteText
                {
                    Colour = TournamentGame.ELEMENT_FOREGROUND_COLOUR,
                    Font = OsuFont.Torus.With(weight: FontWeight.SemiBold, size: 50),
                    Padding = new MarginPadding { Horizontal = 10 },
                    Text = text,
                }
            };

            fontAdjustAction?.Invoke(Text);
        }
    }
}
