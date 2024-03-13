// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Tournament.Models;

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
                    Texture = customTexture,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft
                });
            }

            //AddInternal(new PadLock(color)
            //{
            //    Origin = Anchor.Centre,
            //    Anchor = Anchor.Centre,
            //    Size = new Vector2(45),
            //});
        }

        //private partial class PadLock : Container
        //{
        //    [Resolved]
        //    private OsuColour osuColour { get; set; } = null!;
        //
        //    private TeamColour team;
        //
        //    public TeamColour Team
        //    {
        //        set
        //        {
        //            if (!IsLoaded)
        //                return;
        //
        //            team = value;
        //
        //            lockIcon.Colour = value == TeamColour.Red ? osuColour.TeamColourRed : osuColour.TeamColourBlue;
        //        }
        //    }
        //
        //    private Sprite background = null!;
        //    private SpriteIcon lockIcon = null!;
        //
        //    public PadLock(TeamColour color)
        //    {
        //        team = color;
        //    }
        //
        //    [BackgroundDependencyLoader]
        //    private void load(TextureStore textures)
        //    {
        //        Children = new Drawable[]
        //        {
        //            background = new Sprite
        //            {
        //                RelativeSizeAxes = Axes.Both,
        //                FillMode = FillMode.Fit,
        //                Texture = textures.Get("Icons/BeatmapDetails/mod-icon"),
        //                Anchor = Anchor.Centre,
        //                Origin = Anchor.Centre,
        //            },
        //            lockIcon = new SpriteIcon
        //            {
        //                Origin = Anchor.Centre,
        //                Anchor = Anchor.Centre,
        //                Size = new Vector2(15),
        //                Icon = FontAwesome.Solid.ShieldAlt,
        //                Shadow = true,
        //            }
        //        };
        //
        //        lockIcon.Colour = team == TeamColour.Red ? osuColour.TeamColourRed : osuColour.TeamColourBlue;
        //    }
        //}
    }
}
