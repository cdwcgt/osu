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
        private TeamColour team = TeamColour.Red;
        private Sprite sprite = null!;

        [Resolved]
        private TextureStore textures { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            AddInternal(sprite = new Sprite
            {
                FillMode = FillMode.Fit,
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.CentreRight,
                Origin = Anchor.CentreRight,
                Texture = textures.Get("Protect/team-red"),
            });
        }

        public TeamColour Team
        {
            get => team;
            set
            {
                if (team == value)
                    return;

                team = value;
                sprite.Texture = textures.Get($"Protect/team-{team.ToString().ToLowerInvariant()}");
            }
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
