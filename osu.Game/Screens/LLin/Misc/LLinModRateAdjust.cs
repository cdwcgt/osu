using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Screens.LLin.Misc
{
    internal class LLinModRateAdjust : ModRateAdjust
    {
        public override string Name => ToString();
        public override string Acronym => "RA";
        public override string Description => "no";
        public override double ScoreMultiplier => 0;
        public override bool UserPlayable => false;

        public override BindableNumber<double> SpeedChange { get; } = new BindableDouble
        {
            Value = 1,
            MaxValue = 2,
            MinValue = 0.1f
        };

        public override void ApplyToTrack(IAdjustableAudioComponent track)
        {
            //不要应用到音轨，我们只希望这个Mod影响故事版Sample
        }
    }
}
