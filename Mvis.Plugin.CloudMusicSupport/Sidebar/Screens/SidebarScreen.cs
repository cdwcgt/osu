using osu.Framework.Screens;
using osu.Game.Graphics.UserInterface;

namespace Mvis.Plugin.CloudMusicSupport.Sidebar.Screens
{
    public abstract partial class SidebarScreen : Screen
    {
        public virtual IconButton[] Entries => new IconButton[] { };
    }
}
