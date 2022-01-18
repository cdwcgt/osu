// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;

namespace osu.Game.Overlays.Settings.Sections.Mf
{
    public class MfMvisSection : SettingsSection
    {
        public override Drawable CreateIcon() => new SpriteIcon
        {
            Icon = FontAwesome.Regular.PlayCircle
        };

        public override LocalisableString Header => "LLin";

        public MfMvisSection()
        {
            Add(new MvisUISettings());
            Add(new MvisAudioSettings());
        }
    }
}
