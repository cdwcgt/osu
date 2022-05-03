// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Localisation;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Resources.Localisation.Web;

namespace osu.Game.Overlays.Profile.Header.Components
{
    public class LevelProgressBar : CompositeDrawable, IHasTooltip
    {
        public readonly Bindable<APIUser> User = new Bindable<APIUser>();

        public LocalisableString TooltipText { get; }

        private Bar levelProgressBar;

        public LevelProgressBar()
        {
            TooltipText = UsersStrings.ShowStatsLevelProgress;
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            InternalChildren = new Drawable[]
            {
                levelProgressBar = new Bar
                {
                    RelativeSizeAxes = Axes.Both,
                    BackgroundColour = colourProvider.Background6,
                    Direction = BarDirection.LeftToRight,
                    AccentColour = colourProvider.Highlight1
                },
            };

            User.BindValueChanged(user => updateProgress(user.NewValue));
        }

        private void updateProgress(APIUser user)
        {
            levelProgressBar.Length = user?.Statistics?.Level.Progress / 100f ?? 0;
            levelProgressText.Text = user?.Statistics?.Level.Progress.ToLocalisableString("0'%'") ?? default(LocalisableString);
        }
    }
}
