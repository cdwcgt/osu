// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Sprites;

namespace osu.Game.Tournament.Components
{
    public partial class TournamentSpriteTextWithMaxWidthBackground : TournamentSpriteTextWithBackground
    {
        public float MaxWidth { get; set; } = 200;

        public TournamentSpriteTextWithMaxWidthBackground()
        {
            ((SpriteText)Text).Truncate = true;
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (Text.DrawWidth > MaxWidth)
            {
                Text.Width = MaxWidth;
            }
        }
    }
}
