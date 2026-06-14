// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Logging;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Online.Multiplayer;
using osuTK;
using Velopack;
using Velopack.Sources;

namespace osu.Game.Tournament.Screens.Setup
{
    public partial class UpdateCheckAction : LabelledDrawable<Drawable>
    {
        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [Resolved]
        private OsuGameBase game { get; set; } = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        private readonly Bindable<ReleaseStream> releaseStream = new Bindable<ReleaseStream>();

        private string getCurrentVersion => $"当前版本为{game.Version}";

        public UpdateCheckAction()
            : base(true)
        {
            Label = "版本更新";
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddInternal(progressBar = new Box
            {
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
                RelativeSizeAxes = Axes.X,
                Height = 5f,
                Width = 0f,
                Colour = colours.Blue,
            });

            text.Text = getCurrentVersion;

            string version = game.Version;

            config.SetValue(OsuSetting.Version, version);
            config.BindWith(OsuSetting.ReleaseStream, releaseStream);
            releaseStream.BindValueChanged(_ => CheckForUpdate());
            releaseStreamDropdown.Current.BindTo(releaseStream);

            CheckForUpdate();
        }

        private CancellationTokenSource updateCancellationSource = new CancellationTokenSource();
        private RoundedButton button = null!;
        private TournamentSpriteText text = null!;
        private Box progressBar = null!;
        private OsuEnumDropdown<ReleaseStream> releaseStreamDropdown = null!;

        public void CheckForUpdate()
        {
            CheckForUpdateAsync().FireAndForget();
        }

        public async Task<bool> CheckForUpdateAsync(CancellationToken cancellationToken = default) => await Task.Run(async () =>
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Cancels the last update and closes any existing notifications as stale.
            using (var lastCts = Interlocked.Exchange(ref updateCancellationSource, cts))
                await lastCts.CancelAsync().ConfigureAwait(false);

            try
            {
                return await PerformUpdateCheck(cts.Token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Log($"{nameof(PerformUpdateCheck)} failed ({e.Message})");
                return true;
            }
        }, cancellationToken).ConfigureAwait(false);

        protected virtual async Task<bool> PerformUpdateCheck(CancellationToken cancellationToken)
        {
            text.Text = "正在检查更新...";

            try
            {
                IUpdateSource updateSource = new GithubSource(@"https://github.com/osu-mania-Chinese-National-Cup/osu/", null, false);
                Velopack.UpdateManager updateManager = new Velopack.UpdateManager(updateSource, new UpdateOptions
                {
                    AllowVersionDowngrade = true,
                    ExplicitChannel = releaseStream.Value.ToString().ToLowerInvariant()
                });

                UpdateInfo? update = await updateManager.CheckForUpdatesAsync().ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                {
                    log("Update check cancelled");
                    text.Text = $"更新已取消，{getCurrentVersion}";
                    return true;
                }

                if (update == null)
                {
                    // No update is available.
                    log("No update found");
                    text.Text = $"已是最新版本，{getCurrentVersion}";

                    return false;
                }

                // Download update in the background while notifying awaiters of the update being available.
                log($"New update available: {update.TargetFullRelease.Version}");
                text.Text = $"新版本可用，正在下载 {update.TargetFullRelease.Version}";
                downloadUpdate(updateManager, update, cancellationToken);
                return true;
            }
            catch (Exception e)
            {
                log($"Update check failed with error ({e.Message})");

                return true;
            }
        }

        private void downloadUpdate(Velopack.UpdateManager updateManager, UpdateInfo update, CancellationToken cancellationToken) => Task.Run(async () =>
        {
            log($"Beginning download of update {update.TargetFullRelease.Version}...");

            Schedule(() =>
            {
                button.Enabled.Value = false;
                progressBar.Colour = colours.Blue;
            });

            try
            {
                await updateManager.DownloadUpdatesAsync(update, p => Scheduler.AddOnce(() =>
                {
                    progressBar.Width = p / 100f;
                }), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                text.Text = $"更新已取消，当前版本为{game.Version}";
                Schedule(() =>
                {
                    progressBar.Colour = colours.Red;
                    button.Enabled.Value = true;
                });
                log(@"Update cancelled");
                return false;
            }
            catch (Exception e)
            {
                text.Text = $"更新失败，当前版本为{game.Version}, 错误信息：{e.Message}";
                Schedule(() =>
                {
                    progressBar.Colour = colours.Red;
                    button.Enabled.Value = true;
                });
                Logger.Error(e, @"Update failed!");
                return false;
            }

            text.Text = $"下载完成，重启以安装更新{update.TargetFullRelease.Version}";
            button.Text = "重新启动";

            Schedule(() =>
            {
                button.Action = () => restartToApplyUpdate(updateManager, update);
                button.Enabled.Value = true;
                releaseStreamDropdown.Current.Disabled = true;
            });

            return true;
        }, cancellationToken);

        private void restartToApplyUpdate(Velopack.UpdateManager updateManager, UpdateInfo update) => Task.Run(async () =>
        {
            await updateManager.WaitExitThenApplyUpdatesAsync(update.TargetFullRelease).ConfigureAwait(false);
            Schedule(() => game.AttemptExit());
        });

        protected override Drawable CreateComponent() => new Container
        {
            AutoSizeAxes = Axes.Y,
            RelativeSizeAxes = Axes.X,
            Children = new Drawable[]
            {
                text = new TournamentSpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                },
                releaseStreamDropdown = new OsuEnumDropdown<ReleaseStream>
                {
                    RelativeSizeAxes = Axes.X,
                    Width = 0.5f,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                },
                new FillFlowContainer
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    AutoSizeAxes = Axes.Both,
                    Spacing = new Vector2(10, 0),
                    Children = new Drawable[]
                    {
                        button = new RoundedButton
                        {
                            Size = new Vector2(120, 40),
                            Text = "检查并安装更新",
                            Action = CheckForUpdate
                        }
                    }
                }
            }
        };

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            updateCancellationSource.Cancel();
            updateCancellationSource.Dispose();
        }

        private static void log(string text) => Logger.Log($"VelopackUpdateManager: {text}");
    }
}
