// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Graphics;
using osu.Game.Online.Multiplayer;
using osu.Game.Screens;
using osu.Game.Screens.Backgrounds;
using osu.Game.Screens.Menu;
using osu.Game.Tournament.IPC;
using osu.Game.Tournament.Models;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Gameplay.GameplayPlayerArea
{
    public partial class IdlePlayerScreen : OsuScreen
    {
        private readonly int index;
        private readonly TeamColour colour;
        private readonly TournamentSpriteText userText;

        protected override BackgroundScreen CreateBackground() => new BackgroundScreenDefault();

        private readonly IBindableList<MultiplayerRoomUser> teamUser = new BindableList<MultiplayerRoomUser>();

        [Resolved]
        private LazerRoomMatchInfo lazerRoomMatchInfo { get; set; } = null!;

        public IdlePlayerScreen(int index, TeamColour colour)
        {
            this.index = index;
            this.colour = colour;
            RelativeSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                new OsuLogo
                {
                    Scale = new Vector2(0.5f),
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre
                },
                new MenuSideFlashes(),
                new KiaiMenuFountains(),
                userText = new TournamentSpriteText
                {
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                    Font = OsuFont.Default.With(size: 30),
                    Colour = Color4.Pink
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            teamUser.BindCollectionChanged((_, _) => updateUsername());

            switch (colour)
            {
                case TeamColour.Red:
                    teamUser.BindTo(lazerRoomMatchInfo.RedTeamUser);
                    break;

                case TeamColour.Blue:
                    teamUser.BindTo(lazerRoomMatchInfo.BlueTeamUser);
                    break;
            }
        }

        private void updateUsername()
        {
            string username = string.Empty;

            if (teamUser.Count > index)
            {
                username = teamUser[index].User?.Username ?? string.Empty;
            }

            userText.Text = username;
        }
    }
}
