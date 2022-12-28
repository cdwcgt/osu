using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Screens.LLin;
using osu.Game.Screens.LLin.Plugins.Types;
using osuTK;
using osuTK.Graphics;

#nullable disable

namespace Mvis.Plugin.BottomBar.Buttons
{
    public partial class BottomBarButton : CompositeDrawable, IHasTooltip
    {
        [Resolved]
        private CustomColourProvider colourProvider { get; set; }

        protected CustomColourProvider ColourProvider => colourProvider;
        protected FillFlowContainer ContentFillFlow;

        public IconUsage Icon
        {
            get => SpriteIcon.Icon;
            set => SpriteIcon.Icon = value;
        }

        public LocalisableString Title
        {
            get => SpriteText.Text;
            set => SpriteText.Text = value;
        }

        protected Box BgBox;
        private Box flashBox;
        private Container content;
        private IconUsage emptyIcon => new IconUsage();

        public LocalisableString TooltipText { get; set; }

        protected readonly OsuSpriteText SpriteText = new OsuSpriteText
        {
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            Font = OsuFont.GetFont(Typeface.Custom)
        };

        protected SpriteIcon SpriteIcon = new SpriteIcon
        {
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            Size = new Vector2(13),
        };

        public new Axes AutoSizeAxes
        {
            get => base.AutoSizeAxes;
            set => base.AutoSizeAxes = value;
        }

        public readonly IFunctionProvider Provider;

        public BottomBarButton(IFunctionProvider provider = null)
        {
            Size = new Vector2(30);

            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            if (provider != null)
            {
                Icon = provider.Icon;
                SpriteText.Text = provider.Title;
                SpriteIcon.Icon = provider.Icon;
                TooltipText = provider.Description;
                Size = provider.Size;
                Provider = provider;
            }
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                content = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Masking = true,
                    CornerRadius = 5,
                    EdgeEffect = new EdgeEffectParameters
                    {
                        Type = EdgeEffectType.Shadow,
                        Radius = 1.5f,
                        Colour = Color4.Black.Opacity(0.6f),
                        Offset = new Vector2(0, 1.5f)
                    },
                    Children = new Drawable[]
                    {
                        BgBox = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = ColourProvider.Background3
                        },
                        ContentFillFlow = new FillFlowContainer
                        {
                            Margin = new MarginPadding { Left = 15, Right = 15 },
                            AutoSizeAxes = Axes.X,
                            RelativeSizeAxes = Axes.Y,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Spacing = new Vector2(5),
                            Direction = FillDirection.Horizontal
                        },
                        flashBox = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.White,
                            Alpha = 0,
                        }
                    }
                },
                new HoverClickSounds()
            };

            if (!Icon.Equals(emptyIcon))
                ContentFillFlow.Add(SpriteIcon);

            if (string.IsNullOrEmpty(Title.ToString()))
                ContentFillFlow.Add(SpriteText);

            // From OsuAnimatedButton
            if (AutoSizeAxes != Axes.None)
            {
                content.RelativeSizeAxes = (Axes.Both & ~AutoSizeAxes);
                content.AutoSizeAxes = AutoSizeAxes;
            }

            colourProvider.HueColour.BindValueChanged(_ =>
            {
                BgBox.Colour = ColourProvider.Background3;
            });
        }

        [CanBeNull]
        private Drawable ripple;

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            var mouseSpace = ToLocalSpace(e.ScreenSpaceMousePosition);

            //workaround: 因为o!f的奇妙遮罩实现，内侧Masking为True的Drawable会无视其父级Drawable的Masking
            //导致了圆圈会超出按钮的bug
            ripple ??= new RippleSprite
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.X,
                Height = Width,
                Colour = Color4.White.Opacity(0.5f),
                Scale = new Vector2(0),
                Alpha = 0,
                X = mouseSpace.X - Width / 2,
                Y = mouseSpace.Y - Height / 2
            };

            if (ripple.Parent != content)
                content.Add(ripple);

            content.ScaleTo(0.9f, 2000, Easing.OutQuint);
            ripple.ScaleTo(1.2f, 3000, Easing.OutCirc).FadeIn(3000, Easing.OutCirc);
            return base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            content.ScaleTo(1f, 1000, Easing.OutElastic);
            flashBox.FadeOut(1000, Easing.OutQuint);

            ripple?.FadeOut(1000, Easing.OutQuint)
                  .ScaleTo(1.2 - ripple.Scale.X < 0.2 ? 1.5f : 1.2f, 500, Easing.OutCirc).Expire();

            ripple = null;

            base.OnMouseUp(e);
        }

        protected override bool OnClick(ClickEvent e)
        {
            OnClickAnimation();
            Provider.Active();

            return true;
        }

        protected override bool OnHover(HoverEvent e)
        {
            flashBox.FadeTo(0.1f, 300);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            base.OnHoverLost(e);
            flashBox.FadeTo(0f, 300);
        }

        protected virtual void OnClickAnimation()
        {
            flashBox?.FadeTo(0.2f).Then().FadeTo(IsHovered ? 0.1f : 0f, 300);
        }

        public void DoFlash() => OnClickAnimation();

        private partial class RippleSprite : Sprite
        {
            [BackgroundDependencyLoader]
            private void load(LargeTextureStore textureStore)
            {
                Texture = textureStore.Get("circle");
            }
        }
    }
}
