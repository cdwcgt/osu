// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using BanchoSharp;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Overlays;

namespace osu.Game.Screens.IrcBot
{
    public partial class IrcBotScreen : OsuScreen
    {
        private BanchoClient banchoClient = new BanchoClient();
        private IrcLogin loginPanel;
        private FillFlowContainer roomTab;

        public BindableBool IsLogin = new BindableBool();

        [Cached]
        private readonly OverlayColourProvider overlayColourProvider = new OverlayColourProvider(OverlayColourScheme.Orange);

        public IrcBotScreen()
        {
            InternalChildren = new Drawable[]
            {
                new Box
                {
                    Colour = overlayColourProvider.Background6,
                    RelativeSizeAxes = Axes.Both,
                },
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Name = "Tab",
                    Direction = FillDirection.Vertical,
                    Height = 80f,
                    Children = new Drawable[]
                    {
                        roomTab = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            Name = "Room Tab",
                            Direction = FillDirection.Vertical,
                        },
                        new AddRoomButton()
                    }
                },
                new Container
                {
                    Name = "Main",
                    RelativeSizeAxes = Axes.Both,
                    Margin = new MarginPadding { Top = 80f },
                },
                loginPanel = new IrcLogin
                {
                    FireLogin = loginToBancho
                }
            };

            IsLogin.BindValueChanged(v =>
            {
                if (v.NewValue)
                {
                    loginPanel.Hide();
                }
                else
                {
                    loginPanel.Show();
                }
            });
        }

        private void loginToBancho(string username, string password)
        {
            banchoClient.ClientConfig = new BanchoClientConfig(
                new IrcCredentials(username, password));

            banchoClient.OnAuthenticated += () =>
            {
                IsLogin.Value = true;
            };

            banchoClient.OnDisconnected += () =>
            {
                IsLogin.Value = false;
            };

            Task.Run(banchoClient.ConnectAsync);
        }
    }
}
