// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.Components
{
    public partial class DrawableTeamTitle : CompositeDrawable
    {
        private readonly TournamentTeam? team;
        private readonly TournamentSpriteTextWithBackground teamText;

        [UsedImplicitly]
        private Bindable<string>? acronym;

        public DrawableTeamTitle(TournamentTeam? team, Anchor anchor = Anchor.TopLeft)
        {
            TournamentSpriteTextWithBackground teamIdText;
            AutoSizeAxes = Axes.Both;
            InternalChild = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Children = new Drawable[]
                {
                    teamIdText = new TournamentSpriteTextWithBackground
                    {
                        Origin = anchor,
                        Anchor = anchor
                    },
                    teamText = new TournamentSpriteTextWithBackground
                    {
                        Origin = anchor,
                        Anchor = anchor
                    }
                }
            };
            teamIdText.Text.Padding = new MarginPadding { Horizontal = 15 };

            if (team != null)
            {
                team.FullName.BindValueChanged(name => teamText.Text.Text = name.NewValue, true);
                team.Seed.BindValueChanged(seed => teamIdText.Text.Text = seed.NewValue, true);
                team.Color.BindValueChanged(color => teamIdText.BackgroundColor = color.NewValue, true);
                team.IdTextColor.BindValueChanged(color => teamIdText.Text.Colour = color.NewValue, true);
                team.NameBackgroundColor.BindValueChanged(color => teamText.BackgroundColor = color.NewValue, true);
                team.NameTextColor.BindValueChanged(color => teamText.Text.Colour = color.NewValue, true);
            }

            this.team = team;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            if (team == null) return;

            (acronym = team.Acronym.GetBoundCopy()).BindValueChanged(_ => teamText.Text.Text = team?.FullName.Value ?? string.Empty, true);
        }
    }
}
