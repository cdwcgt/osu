// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Tournament.Models;
using osuTK;

namespace osu.Game.Tournament.Components
{
    public partial class DrawableTeamFlag : Container
    {
        private readonly TournamentTeam? team;
        private readonly TeamColour? colour;

        [UsedImplicitly]
        private Bindable<string>? flag;

        private Sprite? flagSprite;

        public DrawableTeamFlag(TournamentTeam? team, TeamColour? colour = null)
        {
            this.team = team;
            this.colour = colour;
        }

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            if (team == null) return;

            Size = new Vector2(75, 54);
            Masking = true;
            //CornerRadius = 5;
            Child = flagSprite = new Sprite
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                FillMode = FillMode.Stretch
            };

            if (colour != null)
            {
                Masking = true;
                BorderThickness = 5;
                BorderColour = TournamentGame.GetTeamColour(colour.Value);
            }

            (flag = team.FlagName.GetBoundCopy()).BindValueChanged(_ =>
            {
                var texture = textures.Get($@"Flags/{team.FlagName}");
                Size = new Vector2(75, 54);

                if (texture == null && team.Players.Count > 0)
                {
                    var player = team.Players[0];
                    texture = textures.Get($"https://a.ppy.sh/{player.OnlineID}");
                    Size = new Vector2(75, 75);
                }

                flagSprite.Texture = texture;
            }, true);
        }
    }
}
