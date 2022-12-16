using M.Resources.Fonts;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Game.Configuration;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Overlays.Settings.Sections.Mf
{
    public partial class ExperimentalSettings : SettingsSubsection
    {
        protected override LocalisableString Header => "实验性功能";

        private readonly Bindable<string> customWindowIconPath = new Bindable<string>();
        private readonly Bindable<Font> currentFont = new Bindable<Font> { Default = fake_font };

        private static readonly Font fake_font = new FakeFont
        {
            Name = "Torus",
            Author = "Paulo Goode",
            Homepage = "https://paulogoode.com/torus/",
            FamilyName = "Torus"
        };

        [Resolved]
        private GameHost host { get; set; }

        [BackgroundDependencyLoader]
        private void load(MConfigManager mConfig, OsuGame game)
        {
            Children = new Drawable[]
            {
                new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Padding = new MarginPadding { Horizontal = 15 },
                    Child = new OsuSpriteText
                    {
                        Text = "注意! 这些设置可能会很有帮助, 但调整不好可能会影响整个游戏的稳定性!",
                        RelativeSizeAxes = Axes.X,
                        Colour = Color4.Gold
                    }
                }
            };

            if (RuntimeInfo.IsDesktop)
            {
                Add(new ExperimentalSettingsSetupContainer("自定义窗口图标", MSetting.CustomWindowIconPath));

                Add(new ExperimentalSettingsSetupContainer("加载页背景色(HEX颜色)", MSetting.LoaderBackgroundColor));

                Add(new SettingsCheckbox
                {
                    LabelText = "允许窗口淡入、淡出",
                    TooltipText = "可能在Wayland上没有效果",
                    Current = mConfig.GetBindable<bool>(MSetting.AllowWindowFadeEffect)
                });
            }

            mConfig.BindWith(MSetting.CustomWindowIconPath, customWindowIconPath);
            customWindowIconPath.BindValueChanged(v => game?.SetWindowIcon(v.NewValue));
        }

        private partial class PreferredFontSettingsDropDown : SettingsDropdown<Font>
        {
            protected override OsuDropdown<Font> CreateDropdown() => new FontDropdownControl();

            private partial class FontDropdownControl : DropdownControl
            {
                protected override LocalisableString GenerateItemText(Font font) => $"{font.Name}({font.FamilyName})";
            }
        }

        private partial class ExperimentalSettingsSetupContainer : FillFlowContainer
        {
            [Resolved]
            private MConfigManager mConfg { get; set; }

            private readonly OsuTextBox textBox;
            private readonly MSetting lookup;

            public ExperimentalSettingsSetupContainer(string description, MSetting lookup)
            {
                this.lookup = lookup;

                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;
                Padding = new MarginPadding { Horizontal = 15 };
                Spacing = new Vector2(3);

                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Text = description,
                        RelativeSizeAxes = Axes.X,
                    },
                    textBox = new OsuTextBox
                    {
                        RelativeSizeAxes = Axes.X,
                        CommitOnFocusLost = true
                    }
                };
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                string text = mConfg.Get<string>(lookup);
                textBox.Text = text ?? "没有赋值";
                textBox.OnCommit += applySetting;
            }

            private void applySetting(TextBox sender, bool newtext)
            {
                mConfg.SetValue(lookup, sender.Text);
            }
        }

        public class FakeFont : Font
        {
            public FakeFont()
            {
                Name = "Torus";
                Author = "Paulo Goode";
                Homepage = "https://paulogoode.com/torus/";
                FamilyName = "Torus";
                Description = "osu.Resources中的字体";

                LightAvaliable = true;
                SemiBoldAvaliable = true;
                BoldAvaliable = true;
            }
        }
    }
}
