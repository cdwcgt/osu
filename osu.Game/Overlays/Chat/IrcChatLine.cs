// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BanchoSharp.Interfaces;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Chat;

namespace osu.Game.Overlays.Chat
{
    public partial class IrcChatLine : CompositeDrawable
    {
        protected virtual float FontSize => 20;

        protected virtual float Spacing => 15;

        protected virtual float UsernameWidth => 130;

        [Resolved]
        private OverlayColourProvider? colourProvider { get; set; }

        public IPrivateIrcMessage IrcMessage { get; }

        private OsuSpriteText drawableTimestamp = null!;

        private DrawableChatUsername drawableUsername = null!;

        private TextFlowContainer drawableContentFlow = null!;

        private readonly Bindable<bool> prefer24HourTime = new Bindable<bool>();

        public IrcChatLine(IPrivateIrcMessage message)
        {
            IrcMessage = message;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager configManager)
        {
            configManager.BindWith(OsuSetting.Prefer24HourTime, prefer24HourTime);
            prefer24HourTime.BindValueChanged(_ => updateTimestamp());

            InternalChild = new GridContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.AutoSize),
                    new Dimension(GridSizeMode.Absolute, Spacing + UsernameWidth + Spacing),
                    new Dimension(),
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        drawableTimestamp = new OsuSpriteText
                        {
                            Shadow = false,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Font = OsuFont.GetFont(size: FontSize * 0.75f, weight: FontWeight.SemiBold, fixedWidth: true),
                            AlwaysPresent = true,
                        },
                        drawableUsername = new DrawableChatUsername(new APIUser
                        {
                            Username = IrcMessage.Sender,
                        })
                        {
                            AccentColour = Color4Extensions.FromHex("588c7e"),
                            Width = UsernameWidth,
                            FontSize = FontSize,
                            AutoSizeAxes = Axes.Y,
                            Origin = Anchor.TopRight,
                            Anchor = Anchor.TopRight,
                            Margin = new MarginPadding { Horizontal = Spacing },
                            Text = IrcMessage.Sender,
                        },
                        drawableContentFlow = new TextFlowContainer(f =>
                        {
                            f.Shadow = false;
                            f.Font = f.Font.With(size: FontSize);
                        })
                        {
                            AutoSizeAxes = Axes.Y,
                            RelativeSizeAxes = Axes.X,
                        }
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            drawableTimestamp.Colour = colourProvider?.Background1 ?? Colour4.White;

            updateMessageContent();
            FinishTransforms(true);
        }

        private void updateMessageContent()
        {
            this.FadeTo(1.0f, 500, Easing.OutQuint);
            drawableTimestamp.FadeTo(1, 500, Easing.OutQuint);

            updateTimestamp();
            drawableUsername.Text = IrcMessage.Sender;

            drawableContentFlow.Clear();
            drawableContentFlow.AddText(IrcMessage.Content);
        }

        private void updateTimestamp()
        {
            drawableTimestamp.Text = IrcMessage.Timestamp.ToLocalisableString(prefer24HourTime.Value ? @"HH:mm:ss" : @"hh:mm:ss tt");
        }
    }
}
