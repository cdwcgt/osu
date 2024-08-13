// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Screens.MapGuess
{
    public partial class MapGuessGameScreen : OsuScreen
    {
        public override bool DisallowExternalBeatmapRulesetChanges => true;

        public override bool? AllowGlobalTrackControl => false;

        public override bool HideOverlaysOnEnter => true;

        [Resolved]
        private BeatmapManager beatmaps { get; set; } = null!;

        [Resolved]
        private RulesetStore rulesets { get; set; } = null!;

        private WorkingBeatmap beatmap = null!;
        private MapGuessPlayer? player;

        private bool newAnswer;
        private MapGuessGameState state;
        private Hints hint;
        private int currentTryCount;
        private int countdownCount;
        private double countdownStartTime;
        private readonly int countdownTime;
        private readonly int totalCount;
        private readonly BeatmapSetInfo[] beatmapSets;
        private readonly OsuScreenStack screenStack;
        private readonly BeatmapDropdown beatmapDropdown;
        private readonly RoundedButton restartMusicButton;
        private readonly RoundedButton hintButton;
        private readonly RoundedButton skipButton;
        private readonly OsuSpriteText hintText;
        private readonly OsuSpriteText countdownText;
        private readonly Random random = new Random();
        private readonly ShakeContainer shakeContainer;
        private readonly MapGuessConfig config;

        public MapGuessGameScreen(MapGuessConfig config, BeatmapSetInfo[] beatmapSets)
        {
            this.config = config;
            this.beatmapSets = beatmapSets;

            totalCount = config.AutoSkip.Value;
            countdownTime = config.AutoSkip.Value * 1000;

            Padding = new MarginPadding
            {
                Horizontal = 20,
                Vertical = 10,
            };

            InternalChildren =
            [
                new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Height = 0.95f,
                    RowDimensions =
                    [
                        new Dimension(),
                        new Dimension(GridSizeMode.AutoSize),
                        new Dimension(GridSizeMode.AutoSize),
                        new Dimension(GridSizeMode.AutoSize)
                    ],
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            new PlayerContainer
                            {
                                Masking = true,
                                RelativeSizeAxes = Axes.Both,
                                Child = screenStack = new OsuScreenStack
                                {
                                    RelativeSizeAxes = Axes.Both
                                }
                            }
                        },
                        new Drawable[]
                        {
                            new GridContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 160,
                                Padding = new MarginPadding { Vertical = 10 },
                                ColumnDimensions = [new Dimension(GridSizeMode.Relative, 0.1f)],
                                Content = new Drawable[][]
                                {
                                    [
                                        countdownText = new OsuSpriteText
                                        {
                                            Font = OsuFont.Numeric.With(size: 40)
                                        },
                                        shakeContainer = new ShakeContainer
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Child = beatmapDropdown = new BeatmapDropdown
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                AlwaysShowSearchBar = true
                                            }
                                        }
                                    ]
                                }
                            }
                        },
                        new Drawable[]
                        {
                            new GridContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 50f,
                                Content = new Drawable[][]
                                {
                                    [
                                        restartMusicButton = new RoundedButton
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Text = "Restart music",
                                            Action = restartMusic
                                        },
                                        hintButton = new RoundedButton
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Text = "Hint",
                                            Action = updateHint
                                        },
                                        skipButton = new RoundedButton
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Text = "Skip",
                                            Action = () => showAnswer(false)
                                        },
                                    ]
                                }
                            },
                        },
                        new Drawable[]
                        {
                            hintText = new OsuSpriteText
                            {
                                RelativeSizeAxes = Axes.Both,
                                Font = OsuFont.Default.With(size: 30)
                            },
                        },
                    }
                }
            ];
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            OverlayActivationMode.Value = OverlayActivation.Disabled;
            beatmapDropdown.Current.BindValueChanged(_ => newAnswer = true);
            beatmapDropdown.Items = beatmapSets;
            updateBeatmap();
            beatmapDropdown.Menu.StateChanged += menuState =>
            {
                if (menuState == MenuState.Closed && newAnswer)
                    answerChanged();
            };
        }

        protected override void Update()
        {
            base.Update();
            updateCountdown();
        }

        private void updateCountdown()
        {
            if (config.AutoSkipMode.Value != AutoSkipMode.Countdown || state == MapGuessGameState.Answer)
                return;

            double amountTimePassed = Math.Clamp((Time.Current - countdownStartTime) / countdownTime, 0, countdownTime);
            int newCount = Math.Clamp(totalCount - (int)Math.Floor(amountTimePassed * totalCount), 0, totalCount);

            if (countdownCount != newCount)
            {
                countdownText.Text = Math.Max(0, newCount).ToString();
            }

            countdownCount = newCount;

            if (countdownCount == 0)
            {
                showAnswer(false);
            }
        }

        private void answerChanged()
        {
            if (state == MapGuessGameState.Answer)
                return;

            newAnswer = false;
            currentTryCount++;

            bool correct = string.Equals(beatmapDropdown.Current.Value.Metadata.Title, beatmap.BeatmapSetInfo.Metadata.Title, StringComparison.OrdinalIgnoreCase);

            if (correct)
            {
                showAnswer(true);
                return;
            }

            shakeContainer.Shake();

            switch (config.AutoSkipMode.Value)
            {
                case AutoSkipMode.None:
                    return;

                case AutoSkipMode.TryCount:
                    int remaining = config.AutoSkip.Value - currentTryCount;
                    countdownText.Text = remaining.ToString();

                    if (remaining == 0)
                        showAnswer(false);
                    break;
            }
        }

        private void showAnswer(bool winning)
        {
            state = MapGuessGameState.Answer;
            skipButton.Enabled.Value = false;
            restartMusic();
            beatmapDropdown.Current.Value = beatmap.BeatmapSetInfo;
            Scheduler.AddDelayed(updateBeatmap, config.PreviewLength.Value);
        }

        private void restartMusic() => player?.Reset();

        private void updateHint()
        {
            while (!hintAvailable(++hint) && hint <= Hints.UnblurBackground)
            {
            }

            switch (hint)
            {
                case Hints.Music:
                    beatmap.Track.Volume.Value = 1;
                    restartMusic();
                    break;

                case Hints.ArtistRedacted:
                    string artist = beatmap.Metadata.Artist;
                    hintText.Text = $"Artist: {artist[0]}{hideChar(artist[1..])}";
                    break;

                case Hints.Artist:
                    hintText.Text = $"Artist: {beatmap.Metadata.Artist}";
                    break;

                case Hints.TitleRedacted:
                    hintText.Text += $", Title: {hideChar(beatmap.Metadata.Title)} ";
                    break;

                case Hints.BlurredBackground:
                    player?.ShowBackground(true);
                    break;

                case Hints.UnblurBackground:
                    player?.ShowBackground(false);
                    break;
            }

            if (hint == Hints.UnblurBackground)
                hintButton.Enabled.Value = false;
        }

        private void updateBeatmap()
        {
            var selected = beatmapSets[random.Next(beatmapSets.Length)];
            beatmap = beatmaps.GetWorkingBeatmap(selected.Beatmaps.MaxBy(b => b.StarRating));
            var ruleset = rulesets.GetRuleset(beatmap.BeatmapInfo.Ruleset.OnlineID)?.CreateInstance();
            var autoplayMod = ruleset?.GetAutoplayMod();

            if (ruleset == null || autoplayMod == null)
                return;

            Beatmap.Value = beatmap;

            var score = autoplayMod.CreateScoreFromReplayData(beatmap.GetPlayableBeatmap(ruleset.RulesetInfo), Mods.Value);

            if (player != null)
                screenStack.Exit();

            screenStack.Push(player = new MapGuessPlayer(score, beatmap.BeatmapInfo.Metadata.PreviewTime, config));
            restartMusicButton.Enabled.UnbindBindings();
            restartMusicButton.Enabled.BindTo(player.Paused);

            currentTryCount = 0;
            skipButton.Enabled.Value = true;
            countdownText.Text = config.AutoSkip.Value.ToString();
            hintText.Text = string.Empty;
            beatmapDropdown.SearchTerm.Value = string.Empty;
            hint = Hints.None;
            state = MapGuessGameState.Guessing;
            countdownStartTime = Time.Current;
        }

        private partial class PlayerContainer : Container
        {
            public override bool PropagatePositionalInputSubTree => false;
            public override bool PropagateNonPositionalInputSubTree => false;
        }

        private enum MapGuessGameState
        {
            Guessing,
            Answer,
        }

        private enum Hints
        {
            None,
            Music,
            ArtistRedacted,
            Artist,
            TitleRedacted,
            BlurredBackground,
            UnblurBackground
        }

        private bool hintAvailable(Hints hint)
        {
            switch (hint)
            {
                case Hints.Music:
                    return !config.Music.Value;

                case Hints.BlurredBackground:
                    return !config.ShowBackground.Value || (config.ShowBackground.Value && config.BackgroundBlur.Value > 0); ;
            }

            return true;
        }

        private static string hideChar(string original, bool hideAll = false)
        {
            string text = string.Empty;

            foreach (char t in original)
            {
                if (hideAll ? char.IsWhiteSpace(t) : (!char.IsAsciiLetter(t) || char.IsUpper(t)))
                    text += t;
                else
                    text += '?';
            }

            return text;
        }

        private partial class BeatmapDropdown : OsuDropdown<BeatmapSetInfo>
        {
            private IEnumerable<BeatmapSetInfo> allItems = [];

            public new Framework.Graphics.UserInterface.Menu Menu => base.Menu;
            public Bindable<string> SearchTerm => Header.SearchTerm;

            public new IEnumerable<BeatmapSetInfo> Items
            {
                get => allItems;
                set
                {
                    allItems = value;
                    updateItems();
                }
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                Header.SearchTerm.BindValueChanged(_ => updateItems());
            }

            private void updateItems()
            {
                string searchTerm = Header.SearchTerm.Value;

                if (string.IsNullOrWhiteSpace(searchTerm))
                    return;

                base.Items = allItems.DistinctBy(b => getDisplayTitle(b.Metadata)).Where(s => s.Metadata.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                                                                                              || s.Metadata.TitleUnicode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).Take(5).ToArray();
            }

            protected override LocalisableString GenerateItemText(BeatmapSetInfo item)
            {
                return getDisplayTitle(item.Metadata);
            }

            private static string getDisplayTitle(IBeatmapMetadataInfo metadata)
            {
                string title = metadata.Title;

                if (title != metadata.TitleUnicode)
                    title += $" ({metadata.TitleUnicode})";

                return title;
            }
        }
    }
}
