// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;

namespace osu.Game.Overlays.Profile.Sections.AccountStanding;

public partial class AccountStandingInfo : CircularContainer
{
    private readonly Box blackground;
    private readonly OsuSpriteText username;
    private readonly OsuSpriteText extraText;
    private readonly Bindable<UserProfileData?> user = new Bindable<UserProfileData?>();
    private readonly AccountStandingInfoType type;

    public AccountStandingInfo(Bindable<UserProfileData?> user, LocalisableString text, AccountStandingInfoType type)
    {
        this.user.BindTo(user);
        this.type = type;
        RelativeSizeAxes = Axes.X;
        Height = 30;
        Masking = true;
        CornerRadius = 3;
        Margin = new MarginPadding { Bottom = 5 };
        Children = new Drawable[]
        {
            blackground = new Box
            {
                RelativeSizeAxes = Axes.Both,
            },
            new FillFlowContainer
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                Direction = FillDirection.Horizontal,
                Padding = new MarginPadding { Left = 20, },
                Children = new[]
                {
                    username = new OsuSpriteText
                    {
                        Text = user.Value?.User.Username ?? string.Empty,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Font = OsuFont.GetFont(weight: FontWeight.Bold),
                    },
                    extraText = new OsuSpriteText
                    {
                        Text = text,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                    }
                }
            }
        };
    }

    [BackgroundDependencyLoader]
    private void load(OsuColour colour)
    {
        blackground.Colour = type == AccountStandingInfoType.Danger ? colour.RedDark : colour.Yellow;
        username.Colour = type == AccountStandingInfoType.Danger ? Colour4.White : Colour4.Black;
        extraText.Colour = type == AccountStandingInfoType.Danger ? Colour4.White : Colour4.Black;
    }
}
public enum AccountStandingInfoType
{
    Danger,
    Warning,
}
