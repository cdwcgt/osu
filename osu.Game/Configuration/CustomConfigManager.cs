// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Configuration;
using osu.Framework.Platform;

namespace osu.Game.Configuration
{
    public class CustomConfigManager : IniConfigManager<MSetting>
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
            SetDefault(MSetting.DanceMover, OsuDanceMover.Momentum);
            SetDefault(MSetting.DanceSpinnerMover, OsuDanceSpinnerMover.Circle);
            SetDefault(MSetting.ReplayFramerate, 120.0, 15, 1000, 1);
            SetDefault(MSetting.SpinnerChangeFramerate, false);
            SetDefault(MSetting.SpinnerRadiusStart, 50, 5f, 350f, 1f);
            SetDefault(MSetting.SpinnerRadiusEnd, 50, 5f, 350f, 1f);
            SetDefault(MSetting.AngleOffset, 0.45f, 0f, 2f, 0.01f);
            SetDefault(MSetting.JumpMult, 0.6f, 0f, 2f, 0.01f);
            SetDefault(MSetting.NextJumpMult, 0.25f, 0f, 2f, 0.01f);
            SetDefault(MSetting.SkipStackAngles, false);
            SetDefault(MSetting.SliderDance, true);
            SetDefault(MSetting.BorderBounce, true);
            //Linear mover settings
            SetDefault(MSetting.WaitForPreempt, true);

            //Momentum mover settings
            SetDefault(MSetting.DurationTrigger, 500f, 0f, 5000f, 1f);
            SetDefault(MSetting.DurationMult, 2f, 0f, 50f, 0.1f);
            SetDefault(MSetting.StreamMult, 0.7f, 0f, 50f, 0.1f);
            SetDefault(MSetting.RestrictAngle, 90f, 1f, 180f);
            SetDefault(MSetting.RestrictArea, 40f, 1f, 180f);
            SetDefault(MSetting.StreamRestrict, false);
            SetDefault(MSetting.RestrictInvert, true);

            //Momentum extra
            SetDefault(MSetting.EqualPosBounce, 0f, 0, 100f, 0.1f);
            SetDefault(MSetting.RestrictAngleAdd, 90f, 0, 100f);
            SetDefault(MSetting.RestrictAngleSub, 90f, 0, 100f);
            SetDefault(MSetting.StreamArea, 40f, 0, 100);
            SetDefault(MSetting.StreamMaximum, 10000f, 0, 50000f);
            SetDefault(MSetting.StreamMinimum, 50f, 0, 1000f);
            SetDefault(MSetting.InterpolateAngles, true);
            SetDefault(MSetting.InvertAngleInterpolation, false);
            SetDefault(MSetting.SliderPredict, false);

            //Bezier mover settings
            SetDefault(MSetting.BezierAggressiveness, 60f, 1f, 180f);
            SetDefault(MSetting.BezierSliderAggressiveness, 3f, 1f, 20f);
        }
    }

    public enum MSetting
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
