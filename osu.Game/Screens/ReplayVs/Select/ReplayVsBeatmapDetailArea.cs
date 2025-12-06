// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Screens.Select;
using osu.Game.Screens.Select.Leaderboards;

namespace osu.Game.Screens.ReplayVs.Select
{
    public partial class ReplayVsBeatmapDetailArea : BeatmapDetailArea
    {
        public readonly BeatmapLeaderboard Leaderboard;

        public override WorkingBeatmap Beatmap
        {
            get => base.Beatmap;
            set
            {
                base.Beatmap = value;

                Leaderboard.BeatmapInfo = value is DummyWorkingBeatmap ? null : value.BeatmapInfo;
            }
        }

        private Bindable<BeatmapDetailTab> selectedTab = null!;

        private Bindable<bool> selectedModsFilter = null!;

        public ReplayVsBeatmapDetailArea()
        {
            Add(Leaderboard = new BeatmapLeaderboard { RelativeSizeAxes = Axes.Both });
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            selectedTab = new Bindable<BeatmapDetailTab>(BeatmapDetailTab.Local);
            selectedModsFilter = config.GetBindable<bool>(OsuSetting.BeatmapDetailModsFilter);

            selectedTab.BindValueChanged(tab => CurrentTab.Value = getTabItemFromTabType(tab.NewValue), true);
            CurrentTab.BindValueChanged(tab => selectedTab.Value = getTabTypeFromTabItem(tab.NewValue));

            selectedModsFilter.BindValueChanged(checkbox => CurrentModsFilter.Value = checkbox.NewValue, true);
            CurrentModsFilter.BindValueChanged(checkbox => selectedModsFilter.Value = checkbox.NewValue);
        }

        public override void Refresh()
        {
            base.Refresh();

            Leaderboard.RefetchScores();
        }

        protected override void OnTabChanged(BeatmapDetailAreaTabItem tab, bool selectedMods)
        {
            base.OnTabChanged(tab, selectedMods);

            Leaderboard.FilterMods = selectedMods;

            switch (tab)
            {
                case BeatmapDetailAreaLeaderboardTabItem<BeatmapLeaderboardScope>:
                    Leaderboard.Scope = BeatmapLeaderboardScope.Local;
                    Leaderboard.Show();
                    break;

                default:
                    Leaderboard.Hide();
                    break;
            }
        }

        protected override BeatmapDetailAreaTabItem[] CreateTabItems() => base.CreateTabItems().Concat(new BeatmapDetailAreaTabItem[]
        {
            new BeatmapDetailAreaLeaderboardTabItem<BeatmapLeaderboardScope>(BeatmapLeaderboardScope.Local),
        }).ToArray();

        private BeatmapDetailAreaTabItem getTabItemFromTabType(BeatmapDetailTab type)
        {
            switch (type)
            {
                case BeatmapDetailTab.Details:
                    return new BeatmapDetailAreaDetailTabItem();

                default:
                    return new BeatmapDetailAreaLeaderboardTabItem<BeatmapLeaderboardScope>(BeatmapLeaderboardScope.Local);
            }
        }

        private BeatmapDetailTab getTabTypeFromTabItem(BeatmapDetailAreaTabItem item)
        {
            switch (item)
            {
                case BeatmapDetailAreaDetailTabItem:
                    return BeatmapDetailTab.Details;

                case BeatmapDetailAreaLeaderboardTabItem<BeatmapLeaderboardScope>:
                    return BeatmapDetailTab.Local;

                default:
                    throw new ArgumentOutOfRangeException(nameof(item));
            }
        }
    }
}
