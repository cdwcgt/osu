// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using BanchoSharp;
using osu.Framework.Allocation;
using osu.Game.Graphics.UserInterface;

namespace osu.Game.Screens.IrcBot
{
    public partial class AddMatchPopover : AddChannelPopover
    {
        private OsuCheckbox checkbox;

        [Resolved]
        private BanchoClient bancho { get; set; } = null!;

        public AddMatchPopover()
        {
            Flow.Add(checkbox = new OsuCheckbox
            {
                LabelText = "Make Private"
            });

            Title.Text = "Add Match";
            ChannelName.PlaceholderText = "Match Name";
            Button.Text = "Create";
            Button.Action = () =>
            {
                bancho.MakeTournamentLobbyAsync(ChannelName.Text, checkbox.Current.Value);
                Hide();
            };
        }
    }
}
