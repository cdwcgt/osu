// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Play;
using osuTK;

namespace osu.Game.Rulesets.Osu.Mods
{
    public class OsuModKeepInScope : ModFailCondition, IUpdatableByPlayfield, IApplicableToPlayer, IApplicableToBeatmap
    {
        public override string Name => "Keep in Scope";
        public override string Acronym => "KS";
        public override ModType Type => ModType.Fun;
        public override LocalisableString Description => "Don't move out screen";
        public override double ScoreMultiplier => 1;
        public override Type[] IncompatibleMods => new[] { typeof(OsuModAutopilot), typeof(ModAutoplay), typeof(ModNoFail) };

        private readonly IBindable<bool> isBreakTime = new Bindable<bool>();
        private Vector2 playfieldPosition;
        private Vector2 playfieldSize;

        public void ApplyToPlayer(Player player)
        {
            isBreakTime.BindTo(player.IsBreakTime);
        }

        public void ApplyToBeatmap(IBeatmap beatmap)
        {
            float borderPadding = OsuHitObject.OBJECT_RADIUS * LegacyRulesetExtensions.CalculateScaleFromCircleSize(beatmap.Difficulty.CircleSize, true);
            playfieldSize = OsuPlayfield.BASE_SIZE + new Vector2(borderPadding * 2);

            // Relative to OsuCursor's Anchor
            playfieldPosition = new Vector2(-borderPadding);
        }

        public void Update(Playfield playfield)
        {
            var cursorPos = playfield.Cursor.AsNonNull().ActiveCursor.DrawPosition;

            if (!isBreakTime.Value && !isWithinBounds(cursorPos, playfieldPosition, playfieldSize))
                TriggerFailure();
        }

        private static bool isWithinBounds(Vector2 point, Vector2 containerPosition, Vector2 containerSize)
        {
            return point.X >= containerPosition.X &&
                   point.X <= containerPosition.X + containerSize.X &&
                   point.Y >= containerPosition.Y &&
                   point.Y <= containerPosition.Y + containerSize.Y;
        }

        protected override bool FailCondition(HealthProcessor healthProcessor, JudgementResult result) => false;
    }
}
