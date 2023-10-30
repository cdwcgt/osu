// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays.Settings;
using osuTK;

namespace osu.Game.Screens.IrcBot
{
    public partial class IrcLogin : Container
    {
        private TextBox username;
        private TextBox password;

        public Action<string, string>? FireLogin;

        public IrcLogin()
        {
            Height = 600;
            Width = 600;

            ErrorTextFlowContainer errorText;

            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Padding = new MarginPadding { Horizontal = 20 },
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0f, 14f),
                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Text = "Irc Login",
                        Font = OsuFont.GetFont(weight: FontWeight.Bold),
                    },
                    username = new OsuTextBox
                    {
                        PlaceholderText = "Username",
                        RelativeSizeAxes = Axes.X,
                        TabbableContentContainer = this
                    },
                    password = new OsuPasswordTextBox
                    {
                        PlaceholderText = "Password",
                        RelativeSizeAxes = Axes.X,
                        TabbableContentContainer = this,
                    },
                    errorText = new ErrorTextFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Alpha = 0,
                    },
                    new SettingsButton
                    {
                        Text = "Login",
                        Action = () => FireLogin?.Invoke(username.Text, password.Text)
                    }
                }
            };
        }
    }
}
