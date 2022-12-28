using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Screens.LLin.Plugins;
using osu.Game.Screens.LLin.SideBar.Settings.Sections;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.LLin.SideBar.PluginsPage
{
    internal partial class PluginsSection : Section
    {
        [Resolved]
        private LLinPluginManager manager { get; set; } = null!;

        private FillFlowContainer? placeholder;

        public PluginsSection()
        {
            Title = "插件";
            Icon = FontAwesome.Solid.Boxes;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            FillFlow.RelativeSizeAxes = Axes.None;
            FillFlow.AutoSizeAxes = Axes.Both;
            FillFlow.Direction = FillDirection.Vertical;

            AddInternal(placeholder = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Anchor = Anchor.TopRight,
                Origin = Anchor.TopRight,
                Colour = Color4.White.Opacity(0.6f),
                Margin = new MarginPadding(40),
                Children = new Drawable[]
                {
                    new SpriteIcon
                    {
                        Icon = FontAwesome.Solid.Boxes,
                        Size = new Vector2(60),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    },
                    new OsuSpriteText
                    {
                        Text = "没有插件",
                        Font = OsuFont.GetFont(size: 45, weight: FontWeight.Bold),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    }
                }
            });

            manager.OnPluginAdd += addPiece;
            manager.OnPluginUnLoad += removePiece;
        }

        protected override void LoadComplete()
        {
            foreach (var pl in manager.GetAllPlugins(false))
            {
                if (!pl.HideFromPluginManagement)
                    addPiece(pl);
            }

            FillFlow.LayoutEasing = Easing.OutQuint;
            FillFlow.LayoutDuration = 250;

            base.LoadComplete();
        }

        private void addPiece(LLinPlugin plugin)
        {
            Add(new PluginPiece(plugin));

            placeholder.FadeOut(300, Easing.OutQuint);
        }

        private void removePiece(LLinPlugin plugin)
        {
            int childrenCount = 0;

            foreach (var d in FillFlow)
            {
                childrenCount += FillFlow.Children.Count;

                if (d is PluginPiece piece && piece.Plugin == plugin)
                {
                    piece.Hide();
                    break;
                }
            }

            if (childrenCount - 1 <= 0) placeholder.FadeIn(300, Easing.OutQuint);
        }
    }
}
