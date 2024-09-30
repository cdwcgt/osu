// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Configuration;
using osu.Framework.Platform;

namespace osu.Game.Configuration
{
    public class CustomConfigManager : IniConfigManager<CustomSetting>
    {
        protected override string Filename => "custom.ini";
        public static CustomConfigManager Instance { get; private set; }

        public CustomConfigManager(Storage storage)
            : base(storage)
        {
            Instance = this;
        }

        protected override void InitialiseDefaults()
        {
            base.InitialiseDefaults();

            //Dance settings
            SetDefault(CustomSetting.DanceMover, OsuDanceMover.Momentum);
            SetDefault(CustomSetting.DanceSpinnerMover, OsuDanceSpinnerMover.Circle);
            SetDefault(CustomSetting.ReplayFramerate, 120.0, 15, 1000, 1);
            SetDefault(CustomSetting.SpinnerChangeFramerate, false);
            SetDefault(CustomSetting.SpinnerRadiusStart, 50, 5f, 350f, 1f);
            SetDefault(CustomSetting.SpinnerRadiusEnd, 50, 5f, 350f, 1f);
            SetDefault(CustomSetting.AngleOffset, 0.45f, 0f, 2f, 0.01f);
            SetDefault(CustomSetting.JumpMult, 0.6f, 0f, 2f, 0.01f);
            SetDefault(CustomSetting.NextJumpMult, 0.25f, 0f, 2f, 0.01f);
            SetDefault(CustomSetting.SkipStackAngles, false);
            SetDefault(CustomSetting.SliderDance, true);
            SetDefault(CustomSetting.BorderBounce, true);
            //Linear mover settings
            SetDefault(CustomSetting.WaitForPreempt, true);

            //Momentum mover settings
            SetDefault(CustomSetting.DurationTrigger, 500f, 0f, 5000f, 1f);
            SetDefault(CustomSetting.DurationMult, 2f, 0f, 50f, 0.1f);
            SetDefault(CustomSetting.StreamMult, 0.7f, 0f, 50f, 0.1f);
            SetDefault(CustomSetting.RestrictAngle, 90f, 1f, 180f);
            SetDefault(CustomSetting.RestrictArea, 40f, 1f, 180f);
            SetDefault(CustomSetting.StreamRestrict, false);
            SetDefault(CustomSetting.RestrictInvert, true);

            //Momentum extra
            SetDefault(CustomSetting.EqualPosBounce, 0f, 0, 100f, 0.1f);
            SetDefault(CustomSetting.RestrictAngleAdd, 90f, 0, 100f);
            SetDefault(CustomSetting.RestrictAngleSub, 90f, 0, 100f);
            SetDefault(CustomSetting.StreamArea, 40f, 0, 100);
            SetDefault(CustomSetting.StreamMaximum, 10000f, 0, 50000f);
            SetDefault(CustomSetting.StreamMinimum, 50f, 0, 1000f);
            SetDefault(CustomSetting.InterpolateAngles, true);
            SetDefault(CustomSetting.InvertAngleInterpolation, false);
            SetDefault(CustomSetting.SliderPredict, false);

            //Bezier mover settings
            SetDefault(CustomSetting.BezierAggressiveness, 60f, 1f, 180f);
            SetDefault(CustomSetting.BezierSliderAggressiveness, 3f, 1f, 20f);
        }
    }

    public enum CustomSetting
    {
        //Dance settings
        DanceMover,
        DanceSpinnerMover,
        ReplayFramerate,
        SpinnerChangeFramerate,
        SpinnerRadiusStart,
        SpinnerRadiusEnd,
        AngleOffset,
        JumpMult,
        NextJumpMult,
        BorderBounce,
        SkipStackAngles,
        SliderDance,

        //Linear mover settings
        WaitForPreempt,

        //Momentum mover settings
        StreamMult,
        RestrictInvert,
        RestrictArea,
        RestrictAngle,
        StreamRestrict,
        DurationTrigger,
        DurationMult,

        //Momentum extra
        EqualPosBounce,
        SliderPredict,
        InterpolateAngles,
        InvertAngleInterpolation,
        RestrictAngleAdd,
        RestrictAngleSub,
        StreamArea,
        StreamMinimum,
        StreamMaximum,

        //Bezier mover settings
        BezierAggressiveness,
        BezierSliderAggressiveness
    }

    public enum OsuDanceMover
    {
        AxisAligned,
        Aggresive,
        Bezier,
        Flower,
        HalfCircle,
        Pippi,
        Linear,
        Momentum
    }

    public enum OsuDanceSpinnerMover
    {
        Circle,
        Pippi,
        Heart,
        Square,
        Triangle,
        Cube,
    }
}
