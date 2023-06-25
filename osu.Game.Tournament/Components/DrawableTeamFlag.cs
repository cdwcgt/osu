// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Tournament.Models;
using osu.Game.Users;
using osu.Game.Users.Drawables;
using osuTK;

namespace osu.Game.Tournament.Components
{
    public partial class DrawableTeamFlag : Container
    {
        private readonly TournamentTeam team;

        public DrawableTeamFlag(TournamentTeam team)
        {
            this.team = team;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            if (team == null) return;

            Size = new Vector2(75, 75);
            Masking = true;
            CornerRadius = 5;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (team == null) return;

            Scheduler.AddOnce(() => Child = new UpdateableAvatar(team.Players.FirstOrDefault()?.ToAPIUser(), false)
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                FillMode = FillMode.Fill
            });
        }
    }
}
