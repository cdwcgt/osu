// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Tournament.Models;
using osuTK;

namespace osu.Game.Tournament.Screens.Gameplay.Components.MatchHeader
{
    public partial class TeamDisplayNote : CompositeDrawable
    {
        private readonly TournamentSpriteText text;
        private readonly Container shareContainer;

        public LocalisableString Text
        {
            get => text.Text;
            set
            {
                text.Text = value;

                if (IsLoaded)
                {
                    this.FadeTo(LocalisableString.IsNullOrEmpty(text.Text) ? 0 : 1);
                }
            }
        }

        public Anchor ShareAnchor
        {
            get => shareContainer.Anchor;
            set
            {
                shareContainer.Anchor = value;
                shareContainer.Origin = value;
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            this.FadeTo(LocalisableString.IsNullOrEmpty(text.Text) ? 0 : 1);
        }

        public TeamDisplayNote(TeamColour colour)
        {
            AutoSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                shareContainer = new Container
                {
                    Anchor = colour == TeamColour.Red ? Anchor.BottomLeft : Anchor.BottomRight,
                    Origin = colour == TeamColour.Red ? Anchor.BottomLeft : Anchor.BottomRight,
                    Masking = true,
                    CornerRadius = 2f,
                    Shear = colour == TeamColour.Red ? new Vector2(0.3f, 1f) : new Vector2(-0.3f, -1f),
                    Size = new Vector2(8f),
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = TournamentGame.ELEMENT_BACKGROUND_COLOUR,
                    }
                },
                new Container
                {
                    Masking = true,
                    CornerRadius = 3f,
                    Anchor = colour == TeamColour.Red ? Anchor.TopRight : Anchor.TopLeft,
                    Origin = colour == TeamColour.Red ? Anchor.TopRight : Anchor.TopLeft,
                    Margin = new MarginPadding
                    {
                        Top = 4f,
                        Left = colour == TeamColour.Red ? 2f : 0f,
                        Bottom = 4f,
                        Right = colour == TeamColour.Red ? 0 : 2f,
                    },
                    AutoSizeAxes = Axes.X,
                    Height = 15f,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            Colour = TournamentGame.ELEMENT_BACKGROUND_COLOUR,
                            RelativeSizeAxes = Axes.Both,
                        },
                        text = new TournamentSpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Colour = TournamentGame.ELEMENT_FOREGROUND_COLOUR,
                            Font = OsuFont.Torus.With(weight: FontWeight.Bold, size: 12),
                            Padding = new MarginPadding { Horizontal = 10 },
                        }
                    }
                },
            };
        }
    }
}
