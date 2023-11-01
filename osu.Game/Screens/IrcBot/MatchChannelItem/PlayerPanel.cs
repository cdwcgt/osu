// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;

namespace osu.Game.Screens.IrcBot.MatchChannelItem
{
    public partial class PlayerPanel : Container, IHasContextMenu
    {
        public IMultiplayerPlayer Player;
        private Box statusBox = null!;
        private OsuSpriteText slotId = null!;
        private OsuSpriteText playerName = null!;

        [Resolved]
        private OsuColour colour { get; set; } = null!;

        public PlayerPanel(IMultiplayerPlayer player)
        {
            Player = player;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Height = 20f;
            RelativeSizeAxes = Axes.X;

            CornerRadius = 5f;
            Masking = true;
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colour.Blue4
                },
                new Container
                {
                    CornerRadius = 10f,
                    Width = 5f,
                    Height = 5f,
                    Masking = true,
                    Padding = new MarginPadding { Left = 10f, Vertical = 5f },
                    Child = statusBox = new Box()
                },
                slotId = new OsuSpriteText
                {
                    Padding = new MarginPadding { Left = 30f, Vertical = 5f },
                    Text = Player.Slot.ToString()
                },
                playerName = new OsuSpriteText
                {
                    Padding = new MarginPadding { Left = 50f, Vertical = 5f },
                    Text = Player.Name
                }
            };
        }

        public void UpdatePlayerData()
        {
            slotId.Text = Player.Slot.ToString();
            playerName.Text = Player.Name;

            switch (Player.State)
            {
                case PlayerState.Ready:
                    statusBox.Colour = colour.Green;
                    break;

                case PlayerState.NotReady:
                    statusBox.Colour = colour.Yellow;
                    break;

                case PlayerState.NoMap:
                    statusBox.Colour = colour.Red;
                    break;

                default:
                    statusBox.Colour = colour.RedDark;
                    break;
            }
        }

        public MenuItem[]? ContextMenuItems => new[]
        {
            new MenuItem("Give Host"),
            new MenuItem("Kick"),
        };
    }
}
