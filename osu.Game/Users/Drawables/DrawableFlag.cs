// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Localisation;
using osu.Game.Configuration;

namespace osu.Game.Users.Drawables
{
    public partial class DrawableFlag : Sprite, IHasTooltip
    {
        private readonly CountryCode countryCode;

        public LocalisableString TooltipText => countryCode == CountryCode.Unknown ? string.Empty : countryCode.GetDescription();

        public DrawableFlag(CountryCode countryCode)
        {
            this.countryCode = countryCode;
        }

        [BackgroundDependencyLoader]
        private void load(TextureStore ts, OsuConfigManager config)
        {
            ArgumentNullException.ThrowIfNull(ts);

            string textureName = countryCode == CountryCode.Unknown ? "__" : countryCode.ToString();
            Texture = ts.Get($@"Flags/{textureName}") ?? ts.Get(@"Flags/__");

            Bindable<bool> hide = config.GetBindable<bool>(OsuSetting.StreamMode);
            hide.BindValueChanged(s =>
            {
                Alpha = s.NewValue ? 0f : 1f;
            }, true);
        }
    }
}
