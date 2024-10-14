// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.Multiplayer;
using osu.Game.Overlays.Toolbar;
using osuTK;

namespace osu.Game.Tournament
{
    internal partial class SaveChangesOverlay : CompositeDrawable, IKeyBindingHandler<PlatformAction>
    {
        [Resolved]
        private TournamentGame tournamentGame { get; set; } = null!;

        private string? lastSerialisedLadder;
        private readonly TourneyButton saveChangesButton;
        private readonly AutoSaveCountDown countDown;

        public SaveChangesOverlay()
        {
            RelativeSizeAxes = Axes.Both;

            InternalChild = new CircularContainer
            {
                Anchor = Anchor.BottomRight,
                Origin = Anchor.BottomRight,
                Position = new Vector2(-5),
                Masking = true,
                AutoSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box
                    {
                        Colour = OsuColour.Gray(0.2f),
                        RelativeSizeAxes = Axes.Both,
                    },
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Children = new Drawable[]
                        {
                            new Container
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                AutoSizeAxes = Axes.Both,
                                Child = countDown = new AutoSaveCountDown
                                {
                                    Margin = new MarginPadding { Top = 5f },
                                    TriggerSave = () =>
                                    {
                                        saveChangesButton?.TriggerClick();
                                    }
                                },
                            },
                            saveChangesButton = new TourneyButton
                            {
                                Text = "Save Changes",
                                RelativeSizeAxes = Axes.None,
                                Width = 140,
                                Height = 50,
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Margin = new MarginPadding(10),
                                Action = () =>
                                {
                                    countDown.ResetTime();
                                    saveChanges();
                                },
                                // Enabled = { Value = false },
                            }
                        }
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            scheduleNextCheck();
        }

        private async Task checkForChanges()
        {
            string serialisedLadder = await Task.Run(() => tournamentGame.GetSerialisedLadder()).ConfigureAwait(true);

            // If a save hasn't been triggered by the user yet, populate the initial value
            lastSerialisedLadder ??= serialisedLadder;

            if (lastSerialisedLadder != serialisedLadder && !saveChangesButton.Enabled.Value)
            {
                saveChangesButton.Enabled.Value = true;
                saveChangesButton.Background
                                 .FadeColour(saveChangesButton.BackgroundColour.Lighten(0.5f), 500, Easing.In).Then()
                                 .FadeColour(saveChangesButton.BackgroundColour, 500, Easing.Out)
                                 .Loop();
            }

            scheduleNextCheck();
        }

        public bool OnPressed(KeyBindingPressEvent<PlatformAction> e)
        {
            if (e.Action == PlatformAction.Save && !e.Repeat)
            {
                saveChangesButton.TriggerClick();
                return true;
            }

            return false;
        }

        public void OnReleased(KeyBindingReleaseEvent<PlatformAction> e)
        {
        }

        private void scheduleNextCheck() => Scheduler.AddDelayed(() => checkForChanges().FireAndForget(), 1000);

        private void saveChanges()
        {
            tournamentGame.SaveChanges();
            lastSerialisedLadder = tournamentGame.GetSerialisedLadder();

            saveChangesButton.Enabled.Value = false;
            saveChangesButton.Background.FadeColour(saveChangesButton.BackgroundColour, 500);
        }

        private partial class AutoSaveCountDown : ClockDisplay
        {
            private OsuSpriteText realTime;
            public Action? TriggerSave;
            private DateTimeOffset targetTime;

            public AutoSaveCountDown()
            {
                ResetTime();

                AutoSizeAxes = Axes.Y;

                InternalChildren = new Drawable[]
                {
                    realTime = new OsuSpriteText(),
                };

                Width = 45;
            }

            public void ResetTime() => targetTime = DateTimeOffset.Now + TimeSpan.FromMinutes(5);

            protected override void Update()
            {
                base.Update();

                if (targetTime >= DateTimeOffset.Now) return;

                TriggerSave?.Invoke();
                ResetTime();
            }

            protected override void UpdateDisplay(DateTimeOffset now)
            {
                TimeSpan remainTime = targetTime - now;
                realTime.Text = remainTime.ToLocalisableString(@"mm\:ss");
            }
        }
    }
}
