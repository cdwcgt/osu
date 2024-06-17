// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Extensions;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Localisation.SkinComponents;
using osu.Game.Resources.Localisation.Web;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Skinning.Components
{
    [UsedImplicitly]
    public partial class BeatmapAttributeText : FontAdjustableSkinComponent, ISpinnerAware
    {
        [SettingSource("Hide when spinning")]
        public Bindable<bool> HideWhenSpinning { get; } = new Bindable<bool>();

        [SettingSource(typeof(BeatmapAttributeTextStrings), nameof(BeatmapAttributeTextStrings.Attribute), nameof(BeatmapAttributeTextStrings.AttributeDescription))]
        public Bindable<BeatmapAttribute> Attribute { get; } = new Bindable<BeatmapAttribute>(BeatmapAttribute.StarRating);

        [SettingSource(typeof(BeatmapAttributeTextStrings), nameof(BeatmapAttributeTextStrings.Template), nameof(BeatmapAttributeTextStrings.TemplateDescription))]
        public Bindable<string> Template { get; } = new Bindable<string>("{Label}: {Value}");

        [Resolved]
        private IBindable<WorkingBeatmap> beatmap { get; set; } = null!;

        [Resolved]
        private Bindable<IReadOnlyList<Mod>> mods { get; set; } = null!;

        [Resolved]
        private BeatmapDifficultyCache difficultyCache { get; set; } = null!;

        [Resolved]
        private OsuGameBase game { get; set; } = null!;

        private IBindable<RulesetInfo> gameRuleset = null!;

        private IBindable<StarDifficulty?> starDifficulty = null!;

        private readonly Dictionary<BeatmapAttribute, LocalisableString> valueDictionary = new Dictionary<BeatmapAttribute, LocalisableString>();

        private static readonly ImmutableDictionary<BeatmapAttribute, LocalisableString> label_dictionary = new Dictionary<BeatmapAttribute, LocalisableString>
        {
            [BeatmapAttribute.CircleSize] = BeatmapsetsStrings.ShowStatsCs,
            [BeatmapAttribute.Accuracy] = BeatmapsetsStrings.ShowStatsAccuracy,
            [BeatmapAttribute.HPDrain] = BeatmapsetsStrings.ShowStatsDrain,
            [BeatmapAttribute.ApproachRate] = BeatmapsetsStrings.ShowStatsAr,
            [BeatmapAttribute.StarRating] = BeatmapsetsStrings.ShowStatsStars,
            [BeatmapAttribute.Title] = EditorSetupStrings.Title,
            [BeatmapAttribute.Artist] = EditorSetupStrings.Artist,
            [BeatmapAttribute.DifficultyName] = EditorSetupStrings.DifficultyHeader,
            [BeatmapAttribute.Creator] = EditorSetupStrings.Creator,
            [BeatmapAttribute.Source] = EditorSetupStrings.Source,
            [BeatmapAttribute.Length] = ArtistStrings.TracklistLength.ToTitle(),
            [BeatmapAttribute.RankedStatus] = BeatmapDiscussionsStrings.IndexFormBeatmapsetStatusDefault,
            [BeatmapAttribute.BPM] = BeatmapsetsStrings.ShowStatsBpm,
        }.ToImmutableDictionary();

        private readonly OsuSpriteText text;

        public BeatmapAttributeText()
        {
            AutoSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                text = new OsuSpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Attribute.BindValueChanged(_ => updateLabel());
            Template.BindValueChanged(_ => updateLabel());
            gameRuleset = game.Ruleset.GetBoundCopy();
            beatmap.BindValueChanged(b =>
            {
                updateBeatmapContent(b.NewValue);
                updateLabel();
            }, true);
        }

        private void updateBeatmapContent(WorkingBeatmap workingBeatmap)
        {
            BeatmapDifficulty originalDifficulty = new BeatmapDifficulty(workingBeatmap.BeatmapInfo.Difficulty);

            foreach (var mod in mods.Value.OfType<IApplicableToDifficulty>())
                mod.ApplyToDifficulty(originalDifficulty);

            Ruleset ruleset = gameRuleset.Value.CreateInstance();

            double rate = 1;
            foreach (var mod in mods.Value.OfType<IApplicableToRate>())
                rate = mod.ApplyToRate(0, rate);

            BeatmapDifficulty adjustedDifficulty = ruleset.GetRateAdjustedDisplayDifficulty(originalDifficulty, rate);

            valueDictionary[BeatmapAttribute.Title] = new RomanisableString(workingBeatmap.BeatmapInfo.Metadata.TitleUnicode, workingBeatmap.BeatmapInfo.Metadata.Title);
            valueDictionary[BeatmapAttribute.Artist] = new RomanisableString(workingBeatmap.BeatmapInfo.Metadata.ArtistUnicode, workingBeatmap.BeatmapInfo.Metadata.Artist);
            valueDictionary[BeatmapAttribute.DifficultyName] = workingBeatmap.BeatmapInfo.DifficultyName;
            valueDictionary[BeatmapAttribute.Creator] = workingBeatmap.BeatmapInfo.Metadata.Author.Username;
            valueDictionary[BeatmapAttribute.Source] = workingBeatmap.BeatmapInfo.Metadata.Source;
            valueDictionary[BeatmapAttribute.Length] = TimeSpan.FromMilliseconds(workingBeatmap.BeatmapInfo.Length / rate).ToFormattedDuration();
            valueDictionary[BeatmapAttribute.RankedStatus] = workingBeatmap.BeatmapInfo.Status.GetLocalisableDescription();
            valueDictionary[BeatmapAttribute.BPM] = (workingBeatmap.BeatmapInfo.BPM * rate).ToLocalisableString(@"0.##");
            valueDictionary[BeatmapAttribute.CircleSize] = ((double)adjustedDifficulty.CircleSize).ToLocalisableString(@"0.##");
            valueDictionary[BeatmapAttribute.HPDrain] = ((double)adjustedDifficulty.DrainRate).ToLocalisableString(@"0.##");
            valueDictionary[BeatmapAttribute.Accuracy] = ((double)adjustedDifficulty.OverallDifficulty).ToLocalisableString(@"0.##");
            valueDictionary[BeatmapAttribute.ApproachRate] = ((double)adjustedDifficulty.ApproachRate).ToLocalisableString(@"0.##");
            valueDictionary[BeatmapAttribute.StarRating] = workingBeatmap.BeatmapInfo.StarRating.ToLocalisableString(@"0.##");

            starDifficulty = difficultyCache.GetBindableDifficulty(workingBeatmap.BeatmapInfo);
            starDifficulty.BindValueChanged(s =>
            {
                valueDictionary[BeatmapAttribute.StarRating] = (s.NewValue ?? default).Stars.ToLocalisableString("0.##");

                updateLabel();
            });
        }

        private void updateLabel()
        {
            string numberedTemplate = Template.Value
                                              .Replace("{", "{{")
                                              .Replace("}", "}}")
                                              .Replace(@"{{Label}}", "{0}")
                                              .Replace(@"{{Value}}", $"{{{1 + (int)Attribute.Value}}}");

            object?[] args = valueDictionary.OrderBy(pair => pair.Key)
                                            .Select(pair => pair.Value)
                                            .Prepend(label_dictionary[Attribute.Value])
                                            .Cast<object?>()
                                            .ToArray();

            foreach (var type in Enum.GetValues<BeatmapAttribute>())
            {
                numberedTemplate = numberedTemplate.Replace($"{{{{{type}}}}}", $"{{{1 + (int)type}}}");
            }

            text.Text = LocalisableString.Format(numberedTemplate, args);
        }

        protected override void SetFont(FontUsage font) => text.Font = font.With(size: 40);
    }

    // WARNING: DO NOT ADD ANY VALUES TO THIS ENUM ANYWHERE ELSE THAN AT THE END.
    // Doing so will break existing user skins.
    public enum BeatmapAttribute
    {
        CircleSize,
        HPDrain,
        Accuracy,
        ApproachRate,
        StarRating,
        Title,
        Artist,
        DifficultyName,
        Creator,
        Length,
        RankedStatus,
        BPM,
        Source,
    }
}
