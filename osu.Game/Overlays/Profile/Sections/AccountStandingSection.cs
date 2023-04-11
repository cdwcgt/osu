// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Overlays.Profile.Sections.AccountStanding;
using osu.Game.Resources.Localisation.Web;

namespace osu.Game.Overlays.Profile.Sections;

public partial class AccountStandingSection : ProfileSection
{
    public override LocalisableString Title => UsersStrings.ShowExtraAccountStandingTitle;

    public override string Identifier => @"accoung_standing";

    [BackgroundDependencyLoader]
    private void load()
    {
        Children = new Drawable[]
        {
            new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Children = new[]
                {
                    new AccountStandingInfo(User, UsersStrings.ShowExtraAccountStandingBadStanding(""), AccountStandingInfoType.Danger),
                    new AccountStandingInfo(User, UsersStrings.ShowExtraAccountStandingRemainingSilence("", "in 114514 minutes"), AccountStandingInfoType.Warning)
                }
            },
            new AccountStandingHistoryContainer(User)
        };
    }
}
