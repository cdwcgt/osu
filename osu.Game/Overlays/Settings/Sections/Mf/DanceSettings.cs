// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;

namespace osu.Game.Overlays.Settings.Sections.Mf
{
    public partial class DanceSettings : SettingsSubsection
    {
        protected override LocalisableString Header => "osu! cursor dance settings";

        [BackgroundDependencyLoader]
        private void load(MConfigManager config)
        {
            Children = new Drawable[]
            {
                new SettingsEnumDropdown<OsuDanceMover>
                {
                    LabelText = "Dance mover",
                    Current = config.GetBindable<OsuDanceMover>(MSetting.DanceMover)
                },
                new SettingsSlider<float, FramerateSlider>
                {
                    LabelText = "Replay framerate",
                    Current = config.GetBindable<float>(MSetting.ReplayFramerate),
                    KeyboardStep = 10f
                },
                new SettingsCheckbox
                {
                    LabelText = "Change replay framerate for spinners",
                    TooltipText = "Makes spinner movements smoother, but may not be played back on Stable",
                    Current = config.GetBindable<bool>(MSetting.SpinnerChangeFramerate)
                },
                new SettingsSlider<float>
                {
                    LabelText = "Spinner start radius",
                    Current = config.GetBindable<float>(MSetting.SpinnerRadiusStart),
                    KeyboardStep = 5f,
                },
                new SettingsSlider<float>
                {
                    LabelText = "Spinner end radius",
                    Current = config.GetBindable<float>(MSetting.SpinnerRadiusEnd),
                    KeyboardStep = 5f,
                },
                new SettingsSlider<float, AngleSlider>
                {
                    LabelText = "Angle offset",
                    Current = config.GetBindable<float>(MSetting.AngleOffset),
                    KeyboardStep = 1f / 18f
                },
                new SettingsSlider<float, MultiplierSlider>
                {
                    LabelText = "Jump multiplier",
                    Current = config.GetBindable<float>(MSetting.JumpMult),
                    KeyboardStep = 0.01f
                },
                new SettingsSlider<float, MultiplierSlider>
                {
                    LabelText = "Next jump multiplier",
                    Current = config.GetBindable<float>(MSetting.NextJumpMult),
                    KeyboardStep = 0.01f
                },
                new SettingsCheckbox
                {
                    LabelText = "Slider dance",
                    Current = config.GetBindable<bool>(MSetting.SliderDance)
                },
                new SettingsCheckbox
                {
                    LabelText = "Skip short sliders",
                    Current = config.GetBindable<bool>(MSetting.SkipShortSlider)
                },
                new SettingsCheckbox
                {
                    LabelText = "Skip stack angles",
                    Current = config.GetBindable<bool>(MSetting.SkipStackAngles)
                },
                new SettingsCheckbox
                {
                    LabelText = "Bounce on edges",
                    Current = config.GetBindable<bool>(MSetting.BorderBounce)
                },
                new SettingsCheckbox
                {
                    LabelText = "Force pippi mover for spinners",
                    Current = config.GetBindable<bool>(MSetting.PippiSpinner)
                },
                new SettingsCheckbox
                {
                    LabelText = "Force pippi mover for streams",
                    Current = config.GetBindable<bool>(MSetting.PippiStream)
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
                    Current = config.GetBindable<bool>(MSetting.WaitForPreempt)
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
                    Current = config.GetBindable<float>(MSetting.RestrictAngle),
                    KeyboardStep = 1
                },
                new SettingsSlider<float>
                {
                    LabelText = "Restrict angle add",
                    Current = config.GetBindable<float>(MSetting.RestrictAngleAdd),
                    KeyboardStep = 100f
                },
                new SettingsSlider<float>
                {
                    LabelText = "Restrict angle sub",
                    Current = config.GetBindable<float>(MSetting.RestrictAngleSub),
                    KeyboardStep = 100f
                },
                new SettingsSlider<float, MultiplierSlider>
                {
                    LabelText = "Stream multiplier",
                    Current = config.GetBindable<float>(MSetting.StreamMult),
                    KeyboardStep = 0.1f
                },
                new SettingsSlider<float, MultiplierSlider>
                {
                    LabelText = "Duration multiplier",
                    Current = config.GetBindable<float>(MSetting.DurationMult),
                    KeyboardStep = 0.05f
                },
                new SettingsSlider<float>
                {
                    LabelText = "Duration multiplier trigger",
                    Current = config.GetBindable<float>(MSetting.DurationTrigger),
                    KeyboardStep = 100f
                },
                new SettingsSlider<float>
                {
                    LabelText = "Stream area",
                    Current = config.GetBindable<float>(MSetting.StreamArea),
                    KeyboardStep = 100f
                },
                new SettingsSlider<float>
                {
                    LabelText = "Minimum stream distance",
                    Current = config.GetBindable<float>(MSetting.StreamMinimum)
                },
                new SettingsSlider<float>
                {
                    LabelText = "Maximum stream distance",
                    Current = config.GetBindable<float>(MSetting.StreamMaximum)
                },
                new SettingsSlider<float>
                {
                    LabelText = "Bounce on equal pos",
                    Current = config.GetBindable<float>(MSetting.EqualPosBounce)
                },
                new SettingsCheckbox
                {
                    LabelText = "Restrict invert",
                    Current = config.GetBindable<bool>(MSetting.RestrictInvert)
                },
                new SettingsCheckbox
                {
                    LabelText = "Stream restrict",
                    Current = config.GetBindable<bool>(MSetting.StreamRestrict)
                },
                new SettingsCheckbox
                {
                    LabelText = "Slider predict",
                    Current = config.GetBindable<bool>(MSetting.SliderPredict)
                },
                new SettingsCheckbox
                {
                    LabelText = "Interpolate angles",
                    Current = config.GetBindable<bool>(MSetting.InterpolateAngles)
                },
                new SettingsCheckbox
                {
                    LabelText = "Invert angle interpolation",
                    Current = config.GetBindable<bool>(MSetting.InvertAngleInterpolation)
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
                    Current = config.GetBindable<float>(MSetting.BezierAggressiveness),
                    KeyboardStep = 0.1f
                },
                new SettingsSlider<float>
                {
                    LabelText = "Slider aggressiveness",
                    Current = config.GetBindable<float>(MSetting.BezierSliderAggressiveness),
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

        private partial class FramerateSlider : RoundedSliderBar<float>
        {
            public override LocalisableString TooltipText => Current.Value.ToString("g0") + "fps";
        }
    }
}
