// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Resources.Localisation.Web;

namespace osu.Game.Overlays.Profile.Sections.AccountStanding;

public partial class AccountStandingHistoryContainer : ProfileSubsection
{
    private readonly Bindable<UserProfileData?> user = new BindableWithCurrent<UserProfileData?>();

    private FillFlowContainer content = null!;

    [Resolved]
    private OsuColour colour { get; set; } = null!;

    public AccountStandingHistoryContainer(Bindable<UserProfileData?> user)
        : base(user, UsersStrings.ShowExtraAccountStandingRecentInfringementsTitle)
    {
        this.user.BindTo(user);
    }

    private List<Drawable[]> rows = new List<Drawable[]>();

    [BackgroundDependencyLoader]
    private void load()
    {
        if (user.Value == null) return;

        foreach (var accoungStanding in user.Value.User.AccoungStanding)
        {
            content.Add(getRow(getAccountStanding(accoungStanding)));
        }
    }

    protected override Drawable CreateContent() => content = new FillFlowContainer
    {
        RelativeSizeAxes = Axes.X,
        AutoSizeAxes = Axes.Y,
        Direction = FillDirection.Vertical,
        Children = new Drawable[]
        {
            getRow(getHeader())
        }
    };

    private GridContainer getRow(Drawable[] drawables) => new GridContainer
    {
        AutoSizeAxes = Axes.Y,
        RelativeSizeAxes = Axes.X,
        RowDimensions = new[] { new Dimension(GridSizeMode.Absolute, 20) },
        Margin = new MarginPadding { Bottom = 5 },
        ColumnDimensions = new[]
        {
            new Dimension(GridSizeMode.Relative, 0.2f),
            new Dimension(GridSizeMode.Relative, 0.1f),
            new Dimension(GridSizeMode.Relative, 0.2f),
            new Dimension(GridSizeMode.Relative, 0.5f)
        },
        Content = new[]
        {
            drawables
        }
    };

    private Drawable[] getHeader() => new Drawable[]
    {
        getHeaderText(UsersStrings.ShowExtraAccountStandingRecentInfringementsDate),
        getHeaderText(UsersStrings.ShowExtraAccountStandingRecentInfringementsAction),
        getHeaderText(UsersStrings.ShowExtraAccountStandingRecentInfringementsLength),
        getHeaderText(UsersStrings.ShowExtraAccountStandingRecentInfringementsDescription),
    };

    private OsuSpriteText getHeaderText(LocalisableString text) =>
        new OsuSpriteText
        {
            Anchor = Anchor.CentreLeft,
            Origin = Anchor.CentreLeft,
            Text = text,
            Colour = Colour4.White,
            Padding = new MarginPadding { Left = 10, },
            Font = OsuFont.GetFont(weight: FontWeight.Bold)
        };

    private Drawable[] getAccountStanding(APIAccoungStanding accoungStanding) => new Drawable[]
    {
        new DrawableDate(accoungStanding.TimeStamp)
        {
            Colour = colour.Gray9,
            Anchor = Anchor.CentreLeft,
            Origin = Anchor.CentreLeft,
            Padding = new MarginPadding { Left = 10, }
        },
        new CircularContainer
        {
            Anchor = Anchor.CentreLeft,
            Origin = Anchor.CentreLeft,
            RelativeSizeAxes = Axes.Both,
            CornerRadius = 3,
            Masking = true,
            Children = new Drawable[]
            {
                new Box
                {
                    Colour = colour.Yellow,
                    RelativeSizeAxes = Axes.Both
                },
                new OsuSpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Padding = new MarginPadding { Left = 10, },
                    Text = getActionNote(accoungStanding.Type)
                }
            },
        },
        new OsuSpriteText
        {
            Anchor = Anchor.CentreLeft,
            Origin = Anchor.CentreLeft,
            Text = TimeSpan.FromSeconds(accoungStanding.Length).ToString(),
            Padding = new MarginPadding { Left = 10, },
        },
        new OsuSpriteText
        {
            Anchor = Anchor.CentreLeft,
            Origin = Anchor.CentreLeft,
            Text = accoungStanding.Description,
            Padding = new MarginPadding { Left = 10, },
        }
    };

    private LocalisableString getActionNote(APIAccoungStanding.AccountHistoryType type)
    {
        switch (type)
        {
            case APIAccoungStanding.AccountHistoryType.Silence:
                return UsersStrings.ShowExtraAccountStandingRecentInfringementsActionsSilence;

            case APIAccoungStanding.AccountHistoryType.Note:
                return UsersStrings.ShowExtraAccountStandingRecentInfringementsActionsNote;

            case APIAccoungStanding.AccountHistoryType.Restriction:
                return UsersStrings.ShowExtraAccountStandingRecentInfringementsActionsRestriction;
        }

        throw new ArgumentException();
    }
}
