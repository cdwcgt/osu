// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;

namespace osu.Game.Overlays.Settings.Sections.Custom
{
    public partial class DanceSettings : SettingsSubsection
    {
        protected override LocalisableString Header => "osu! cursor dance settings";

        [BackgroundDependencyLoader]
        private void load(CustomConfigManager config)
        {
            Children = new Drawable[]
            {
                new SettingsEnumDropdown<OsuDanceMover>
                {
                    LabelText = "Dance Mover",
                    Current = config.GetBindable<OsuDanceMover>(CustomSetting.DanceMover)
                },
                new SettingsEnumDropdown<OsuDanceSpinnerMover>
                {
                    LabelText = "Dance Spinner Mover",
                    Current = config.GetBindable<OsuDanceSpinnerMover>(CustomSetting.DanceSpinnerMover)
                },
                new SettingsSlider<double, FramerateSlider>
                {
                    LabelText = "Replay Framerate",
                    Current = config.GetBindable<double>(CustomSetting.ReplayFramerate),
                    KeyboardStep = 10
                },
                new SettingsCheckbox
                {
                    LabelText = "Change replay framerate for spinners",
                    TooltipText = "Makes spinner movements smoother, but may not be played back on Stable",
                    Current = config.GetBindable<bool>(CustomSetting.SpinnerChangeFramerate)
                },
                new SettingsSlider<float>
                {
                    LabelText = "Spinner start radius",
                    Current = config.GetBindable<float>(CustomSetting.SpinnerRadiusStart),
                    KeyboardStep = 5f,
                },
                new SettingsSlider<float>
                {
                    LabelText = "Spinner end radius",
                    Current = config.GetBindable<float>(CustomSetting.SpinnerRadiusEnd),
                    KeyboardStep = 5f,
                },
                new SettingsSlider<float, AngleSlider>
                {
                    LabelText = "Angle offset",
                    Current = config.GetBindable<float>(CustomSetting.AngleOffset),
                    KeyboardStep = 1f / 18f
                },
                new SettingsSlider<float, MultiplierSlider>
                {
                    LabelText = "Jump multiplier",
                    Current = config.GetBindable<float>(CustomSetting.JumpMult),
                    KeyboardStep = 0.01f
                },
                new SettingsSlider<float, MultiplierSlider>
                {
                    LabelText = "Next jump multiplier",
                    Current = config.GetBindable<float>(CustomSetting.NextJumpMult),
                    KeyboardStep = 0.01f
                },
                new SettingsCheckbox
                {
                    LabelText = "Slider dance",
                    Current = config.GetBindable<bool>(CustomSetting.SliderDance)
                },
                new SettingsCheckbox
                {
                    LabelText = "Skip stack angles",
                    Current = config.GetBindable<bool>(CustomSetting.SkipStackAngles)
                },
                new SettingsCheckbox
                {
                    LabelText = "Bounce on edges",
                    Current = config.GetBindable<bool>(CustomSetting.BorderBounce)
                },
                new OsuSpriteText
                {
                    Text = "Linear mover settings",
                    Margin = new MarginPadding { Vertical = 15, Left = SettingsPanel.CONTENT_MARGINS },
                    Font = OsuFont.GetFont(weight: FontWeight.Bold)
                },
                new SettingsCheckbox
                {
                    LabelText = "Wait for preempt",
                    Current = config.GetBindable<bool>(CustomSetting.WaitForPreempt)
                },
                new OsuSpriteText
                {
                    Text = "Momentum mover settings",
                    Margin = new MarginPadding { Vertical = 15, Left = SettingsPanel.CONTENT_MARGINS },
                    Font = OsuFont.GetFont(weight: FontWeight.Bold)
                },
                new SettingsSlider<float>
                {
                    LabelText = "Restrict angle",
                    Current = config.GetBindable<float>(CustomSetting.RestrictAngle),
                    KeyboardStep = 1
                },
                new SettingsSlider<float>
                {
                    LabelText = "Restrict angle add",
                    Current = config.GetBindable<float>(CustomSetting.RestrictAngleAdd),
                    KeyboardStep = 100f
                },
                new SettingsSlider<float>
                {
                    LabelText = "Restrict angle sub",
                    Current = config.GetBindable<float>(CustomSetting.RestrictAngleSub),
                    KeyboardStep = 100f
                },
                new SettingsSlider<float, MultiplierSlider>
                {
                    LabelText = "Stream multiplier",
                    Current = config.GetBindable<float>(CustomSetting.StreamMult),
                    KeyboardStep = 0.1f
                },
                new SettingsSlider<float, MultiplierSlider>
                {
                    LabelText = "Duration multiplier",
                    Current = config.GetBindable<float>(CustomSetting.DurationMult),
                    KeyboardStep = 0.05f
                },
                new SettingsSlider<float>
                {
                    LabelText = "Duration multiplier trigger",
                    Current = config.GetBindable<float>(CustomSetting.DurationTrigger),
                    KeyboardStep = 100f
                },
                new SettingsSlider<float>
                {
                    LabelText = "Stream area",
                    Current = config.GetBindable<float>(CustomSetting.StreamArea),
                    KeyboardStep = 100f
                },
                new SettingsSlider<float>
                {
                    LabelText = "Minimum stream distance",
                    Current = config.GetBindable<float>(CustomSetting.StreamMinimum)
                },
                new SettingsSlider<float>
                {
                    LabelText = "Maximum stream distance",
                    Current = config.GetBindable<float>(CustomSetting.StreamMaximum)
                },
                new SettingsSlider<float>
                {
                    LabelText = "Bounce on equal pos",
                    Current = config.GetBindable<float>(CustomSetting.EqualPosBounce)
                },
                new SettingsCheckbox
                {
                    LabelText = "Restrict invert",
                    Current = config.GetBindable<bool>(CustomSetting.RestrictInvert)
                },
                new SettingsCheckbox
                {
                    LabelText = "Stream restrict",
                    Current = config.GetBindable<bool>(CustomSetting.StreamRestrict)
                },
                new SettingsCheckbox
                {
                    LabelText = "Slider predict",
                    Current = config.GetBindable<bool>(CustomSetting.SliderPredict)
                },
                new SettingsCheckbox
                {
                    LabelText = "Interpolate angles",
                    Current = config.GetBindable<bool>(CustomSetting.InterpolateAngles)
                },
                new SettingsCheckbox
                {
                    LabelText = "Invert angle interpolation",
                    Current = config.GetBindable<bool>(CustomSetting.InvertAngleInterpolation)
                },
                new OsuSpriteText
                {
                    Text = "Bezier mover settings",
                    Margin = new MarginPadding { Vertical = 15, Left = SettingsPanel.CONTENT_MARGINS },
                    Font = OsuFont.GetFont(weight: FontWeight.Bold)
                },
                new SettingsSlider<float>
                {
                    LabelText = "Aggressiveness",
                    Current = config.GetBindable<float>(CustomSetting.BezierAggressiveness),
                    KeyboardStep = 0.1f
                },
                new SettingsSlider<float>
                {
                    LabelText = "Slider aggressiveness",
                    Current = config.GetBindable<float>(CustomSetting.BezierSliderAggressiveness),
                    KeyboardStep = 0.5f
                },
            };
        }

        public partial class MultiplierSlider : RoundedSliderBar<float>
        {
            public override LocalisableString TooltipText => Current.Value.ToString("g2") + "x";
        }

        private partial class AngleSlider : RoundedSliderBar<float>
        {
            public override LocalisableString TooltipText => (Current.Value * 180).ToString("g2") + "deg";
        }

        private partial class FramerateSlider : RoundedSliderBar<double>
        {
            public override LocalisableString TooltipText => Current.Value.ToString("g0") + "fps";
        }
    }
}
