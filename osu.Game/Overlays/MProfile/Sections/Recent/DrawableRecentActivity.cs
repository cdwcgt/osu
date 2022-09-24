﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Chat;
using osu.Game.Online.Leaderboards;
using osu.Game.Rulesets;

namespace osu.Game.Overlays.Profile.Sections.Recent
{
    public class DrawableRecentActivity : CompositeDrawable
    {
        private const int font_size = 18;//整合时更改,原为14

        [Resolved]
        private IAPIProvider api { get; set; }

        [Resolved]
        private IRulesetStore rulesets { get; set; }

        private readonly APIRecentActivity activity;

        private LinkFlowContainer content;

        public DrawableRecentActivity(APIRecentActivity activity)
        {
            this.activity = activity;
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            AddInternal(new GridContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Absolute, size: 28),
                    new Dimension(),
                    new Dimension(GridSizeMode.AutoSize)
                },
                RowDimensions = new[]
                {
                    new Dimension(GridSizeMode.AutoSize)
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Child = createIcon().With(icon =>
                            {
                                icon.Anchor = Anchor.Centre;
                                icon.Origin = Anchor.Centre;
                            })
                        },
                        content = new LinkFlowContainer(t => t.Font = OsuFont.GetFont(size: font_size))
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            AutoSizeAxes = Axes.Y,
                            RelativeSizeAxes = Axes.X,
                        },
                        new DrawableDate(activity.CreatedAt)
                        {
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreRight,
                            Colour = colourProvider.Foreground1,
                            Font = OsuFont.GetFont(size: font_size),
                        }
                    }
                }
            });

            createMessage();
        }

        private Drawable createIcon()
        {
            switch (activity.Type)
            {
                case RecentActivityType.Rank:
                    return new UpdateableRank(activity.ScoreRank)
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 11,
                        FillMode = FillMode.Fit,
                        Margin = new MarginPadding { Top = 2 }
                    };

                case RecentActivityType.Achievement:
                    return new DelayedLoadWrapper(new MedalIcon(activity.Achievement.Slug)
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.Both,
                        FillMode = FillMode.Fit,
                    })
                    {
                        RelativeSizeAxes = Axes.X,
                        Width = 0.5f,
                        Height = 18
                    };

                default:
                    return new RecentActivityIcon(activity)
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 11,
                        FillMode = FillMode.Fit,
                        Margin = new MarginPadding { Top = 2, Vertical = 2 }
                    };
            }
        }

        private void createMessage()
        {
            switch (activity.Type)
            {
                case RecentActivityType.Achievement:
                    addUserLink();
                    addText($"解锁了\"{activity.Achievement.Name}\" 奖章!");
                    break;

                case RecentActivityType.BeatmapPlaycount:
                    addBeatmapLink();
                    addText($"已被游玩 {activity.Count} 次!");
                    break;

                case RecentActivityType.BeatmapsetApprove:
                    addBeatmapsetLink();
                    addText($"已被{activity.Approval.ToString().ToLowerInvariant()}!");
                    break;

                case RecentActivityType.BeatmapsetDelete:
                    addBeatmapsetLink();
                    addText("已被删除.");
                    break;

                case RecentActivityType.BeatmapsetRevive:
                    addBeatmapsetLink();
                    addText("已被");
                    addUserLink();
                    addText("从永恒的沉睡中唤醒");
                    break;

                case RecentActivityType.BeatmapsetUpdate:
                    addUserLink();
                    addText("更新了谱面 ");
                    addBeatmapsetLink();
                    break;

                case RecentActivityType.BeatmapsetUpload:
                    addUserLink();
                    addText("上传了一张新的谱面 ");
                    addBeatmapsetLink();
                    break;

                case RecentActivityType.Medal:
                    // apparently this shouldn't exist look at achievement instead (https://github.com/ppy/osu-web/blob/master/resources/assets/coffee/react/profile-page/recent-activity.coffee#L111)
                    break;

                case RecentActivityType.Rank:
                    addUserLink();
                    addText($"在");
                    addBeatmapLink();
                    addText($" ({getRulesetName()})上获得了第{activity.Rank}名!");
                    break;

                case RecentActivityType.RankLost:
                    addUserLink();
                    addText("在");
                    addBeatmapLink();
                    addText($" ({getRulesetName()})上失去了第一名");
                    break;

                case RecentActivityType.UserSupportAgain:
                    addUserLink();
                    addText("再次选择支持osu! - 感谢您的慷慨!");
                    break;

                case RecentActivityType.UserSupportFirst:
                    addUserLink();
                    addText("成为了osu!supporter! - 感谢您的慷慨!");
                    break;

                case RecentActivityType.UserSupportGift:
                    addUserLink();
                    addText("收到了一份osu!supporter的礼物!");
                    break;

                case RecentActivityType.UsernameChange:
                    addText($"{activity.User?.PreviousUsername}更改了用户名");
                    addUserLink();
                    break;
            }
        }

        private string getRulesetName() =>
            rulesets.AvailableRulesets.FirstOrDefault(r => r.ShortName == activity.Mode)?.Name ?? activity.Mode;

        private void addUserLink()
            => content.AddLink(activity.User?.Username, LinkAction.OpenUserProfile, getLinkArgument(activity.User?.Url), creationParameters: t => t.Font = getLinkFont(FontWeight.Bold));

        private void addBeatmapLink()
            => content.AddLink(activity.Beatmap?.Title, LinkAction.OpenBeatmap, getLinkArgument(activity.Beatmap?.Url), creationParameters: t => t.Font = getLinkFont());

        private void addBeatmapsetLink()
            => content.AddLink(activity.Beatmapset?.Title, LinkAction.OpenBeatmapSet, getLinkArgument(activity.Beatmapset?.Url), creationParameters: t => t.Font = getLinkFont());

        private string getLinkArgument(string url) => MessageFormatter.GetLinkDetails($"{api.APIEndpointUrl}{url}").Argument.ToString();

        private FontUsage getLinkFont(FontWeight fontWeight = FontWeight.Regular)
            => OsuFont.GetFont(size: font_size, weight: fontWeight, italics: true);

        private void addText(string text)
            => content.AddText(text, t => t.Font = OsuFont.GetFont(size: font_size, weight: FontWeight.SemiBold));
    }
}
