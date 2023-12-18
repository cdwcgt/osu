// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.Leaderboards;
using osu.Game.Scoring;

namespace osu.Game.Screens.ReplayVs
{
    public partial class ScoreList : OsuRearrangeableListContainer<ScoreInfo>
    {
        protected override OsuRearrangeableListItem<ScoreInfo> CreateOsuDrawable(ScoreInfo item) => new ScoreInfoListItem(item)
        {
            OnDelete = () => Items.Remove(item),
        };
    }

    public partial class ScoreInfoListItem : OsuRearrangeableListItem<ScoreInfo>
    {
        private readonly ScoreInfo scoreInfo;
        public Action OnDelete = null!;

        public ScoreInfoListItem(ScoreInfo item)
            : base(item)
        {
            scoreInfo = item;
        }

        protected override Drawable CreateContent()
        {
            return new DrawableScoreInfo(scoreInfo)
            {
                OnDelete = OnDelete,
                RelativeSizeAxes = Axes.X,
            };
        }
    }

    public partial class DrawableScoreInfo : CompositeDrawable
    {
        private readonly ScoreInfo score;
        public Action OnDelete = null!;

        public DrawableScoreInfo(ScoreInfo score)
        {
            this.score = score;
            AutoSizeAxes = Axes.Y;
            Margin = new MarginPadding { Bottom = 5 };
            Padding = new MarginPadding { Right = 10 };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Y,
                RelativeSizeAxes = Axes.X,
                Direction = FillDirection.Horizontal,
                Children = new Drawable[]
                {
                    new LeaderboardScore(score, null)
                    {
                        RelativeSizeAxes = Axes.X,
                        Width = 0.9f,
                    },
                    new IconButton
                    {
                        Margin = new MarginPadding { Top = 10 },
                        Icon = FontAwesome.Solid.Trash,
                        Action = OnDelete
                    }
                }
            };
        }
    }
}
