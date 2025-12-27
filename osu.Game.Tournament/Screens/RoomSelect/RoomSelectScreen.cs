// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Cursor;
using osu.Game.Overlays;
using osu.Game.Screens;
using osu.Game.Screens.Footer;

namespace osu.Game.Tournament.Screens.RoomSelect
{
    public partial class RoomSelectScreen : TournamentScreen
    {
        [Cached]
        private OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Purple);

        private readonly OsuScreenStack screenStack;
        private readonly ScreenFooter.BackReceptor backReceptor;

        public RoomSelectScreen()
        {
            RelativeSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                backReceptor = new ScreenFooter.BackReceptor(),
                screenStack = new OsuScreenStack
                {
                    RelativeSizeAxes = Axes.Both
                },
                new PopoverContainer
                {
                    Depth = -1,
                    RelativeSizeAxes = Axes.Both,
                    Child = new ScreenStackFooter(screenStack, backReceptor)
                    {
                        BackButtonPressed = handleBackButton
                    }
                },
            };
        }

        protected override void LoadComplete()
        {
            screenStack.Push(new RoomSelectMultiplayerLoungeSubScreen());
        }

        private void handleBackButton()
        {
            if (!(screenStack.CurrentScreen is IOsuScreen currentScreen)) return;

            if (screenStack.CurrentScreen is RoomSelectMultiplayerLoungeSubScreen) return;

            if (!((Drawable)currentScreen).IsLoaded || (currentScreen.AllowUserExit && !currentScreen.OnBackButton())) screenStack.Exit();
        }
    }
}
