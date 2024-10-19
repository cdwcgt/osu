// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Screens;
using osu.Framework.Testing;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Online.Multiplayer;
using osu.Game.Screens;
using osu.Game.Screens.OnlinePlay.Match.Components;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Overlays.SkinEditor
{
    public partial class ExternalSkinEditScreen : OsuScreen
    {
        [Resolved]
        private GameHost gameHost { get; set; } = null!;

        private OverlayColourProvider colourProvider { get; } = new OverlayColourProvider(OverlayColourScheme.Blue);

        [Resolved]
        private SkinManager skinManager { get; set; } = null!;

        private Task? fileMountOperation;

        public ExternalEditOperation<SkinInfo>? EditOperation;

        private FillFlowContainer flow = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = new Container
            {
                Masking = true,
                CornerRadius = 20,
                AutoSizeAxes = Axes.Both,
                AutoSizeDuration = 500,
                AutoSizeEasing = Easing.OutQuint,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Children = new Drawable[]
                {
                    new Box
                    {
                        Colour = colourProvider.Background5,
                        RelativeSizeAxes = Axes.Both,
                    },
                    flow = new FillFlowContainer
                    {
                        Margin = new MarginPadding(20),
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Spacing = new Vector2(15),
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            fileMountOperation = begin();
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            // Don't allow exiting until the file mount operation has completed.
            // This is mainly to simplify the flow (once the screen is pushed we are guaranteed an attempted mount).
            if (fileMountOperation?.IsCompleted == false)
                return true;

            // If the operation completed successfully, ensure that we finish the operation before exiting.
            // The finish() call will subsequently call Exit() when done.
            if (EditOperation != null)
            {
                finish().FireAndForget();
                return true;
            }

            return base.OnExiting(e);
        }

        private async Task begin()
        {
            showSpinner("Exporting for edit...");

            await Task.Delay(500).ConfigureAwait(true);

            SkinInfo detachSkinInfo = skinManager.CurrentSkin.Value.SkinInfo.Value.Detach();

            try
            {
                EditOperation = await skinManager.BeginExternalEditing(detachSkinInfo).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Logger.Log($@"Failed to initiate external edit operation: {ex}", LoggingTarget.Database);
                fileMountOperation = null;
                showSpinner("Export failed!");
                await Task.Delay(1000).ConfigureAwait(true);
                this.Exit();
            }

            flow.Children = new Drawable[]
            {
                new OsuSpriteText
                {
                    Text = "Skin is mounted externally",
                    Font = OsuFont.Default.With(size: 30),
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                },
                new OsuTextFlowContainer
                {
                    Padding = new MarginPadding(5),
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Width = 350,
                    AutoSizeAxes = Axes.Y,
                    Text = "Any changes made to the exported folder will be imported to the game, including file additions, modifications and deletions.",
                },
                new PurpleRoundedButton
                {
                    Text = "Open folder",
                    Width = 350,
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Action = openDirectory,
                    Enabled = { Value = false }
                },
                new DangerousRoundedButton
                {
                    Text = "Finish editing and import changes",
                    Width = 350,
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Action = () => finish().FireAndForget(),
                    Enabled = { Value = false }
                }
            };

            Scheduler.AddDelayed(() =>
            {
                foreach (var b in flow.ChildrenOfType<RoundedButton>())
                    b.Enabled.Value = true;
                openDirectory();
            }, 1000);
        }

        private void openDirectory()
        {
            if (EditOperation == null)
                return;

            // Ensure the trailing separator is present in order to show the folder contents.
            gameHost.OpenFileExternally(EditOperation.MountedPath.TrimDirectorySeparator() + Path.DirectorySeparatorChar);
        }

        private async Task finish()
        {
            showSpinner("Cleaning up...");

            Live<SkinInfo>? skin = null;

            try
            {
                skin = await EditOperation!.Finish().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Logger.Log($@"Failed to finish external edit operation: {ex}", LoggingTarget.Database);
                showSpinner("Import failed!");
                await Task.Delay(1000).ConfigureAwait(true);
            }

            // Setting to null will allow exit to succeed.
            EditOperation = null;

            if (skin != null)
            {
                skinManager.CurrentSkinInfo.Value = skin;
            }

            this.Exit();
        }

        private void showSpinner(string text)
        {
            foreach (var b in flow.ChildrenOfType<RoundedButton>())
                b.Enabled.Value = false;

            flow.Children = new Drawable[]
            {
                new OsuSpriteText
                {
                    Text = text,
                    Font = OsuFont.Default.With(size: 30),
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                },
                new LoadingSpinner
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    State = { Value = Visibility.Visible }
                },
            };
        }
    }
}
