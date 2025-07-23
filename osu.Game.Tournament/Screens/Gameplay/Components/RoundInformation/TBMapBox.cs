// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Tournament.Models;
using osuTK;

namespace osu.Game.Tournament.Screens.Gameplay.Components.RoundInformation
{
    public partial class TBMapBox : MapBox
    {
        public TBMapBox()
            : base(new BeatmapChoice())
        {
        }

        protected override Drawable CreateCenterLine(TeamColour _)
        {
            var TBMap = Ladder.CurrentMatch.Value?.Round.Value?.Beatmaps.FirstOrDefault(map => map.Mods == "TB");
            bool isTBSelected = Ladder.CurrentMatch.Value?.PicksBans.Any(p => p?.BeatmapID == TBMap.ID) == true;

            return new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Children = new Drawable[]
                {
                    new Circle
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(42, 18),
                        Colour = isTBSelected ? Color4Extensions.FromHex("#C53C64") : Color4Extensions.FromHex("#383838")
                    },
                    new SpriteIcon
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Icon = FontAwesome.Solid.Trophy,
                        Size = new Vector2(14, 12)
                    }
                }
            };
        }

        protected override void UpdateStatus()
        {
        }
    }
}
