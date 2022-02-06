// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.Win32;
using osu.Desktop.DBus;
using osu.Desktop.Security;
using osu.Framework.Platform;
using osu.Game;
using osu.Framework;
using osu.Framework.Logging;
using osu.Framework.Screens;
using osu.Game.Updater;
using osu.Desktop.Windows;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Threading;
using osu.Game.Configuration;
using osu.Game.IO;
using osu.Game.Screens.Menu;

namespace osu.Desktop
{
    internal class OsuGameDesktop : OsuGame
    {
        private DBusManagerContainer dBusManagerContainer;

        public OsuGameDesktop(string[] args = null)
            : base(args)
        {
        }

        public override StableStorage GetStorageForStableInstall()
        {
            try
            {
                if (Host is DesktopGameHost desktopHost)
                {
                    string stablePath = getStableInstallPath();
                    if (!string.IsNullOrEmpty(stablePath))
                        return new StableStorage(stablePath, desktopHost);
                }
            }
            catch (Exception)
            {
                Logger.Log("Could not find a stable install", LoggingTarget.Runtime, LogLevel.Important);
            }

            return null;
        }

        private string getStableInstallPath()
        {
            static bool checkExists(string p) => Directory.Exists(Path.Combine(p, "Songs")) || File.Exists(Path.Combine(p, "osu!.cfg"));

            string stableInstallPath;

            if (OperatingSystem.IsWindows())
            {
                try
                {
                    stableInstallPath = getStableInstallPathFromRegistry();

                    if (!string.IsNullOrEmpty(stableInstallPath) && checkExists(stableInstallPath))
                        return stableInstallPath;
                }
                catch
                {
                }
            }

            stableInstallPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"osu!");
            if (checkExists(stableInstallPath))
                return stableInstallPath;

            stableInstallPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".osu");
            if (checkExists(stableInstallPath))
                return stableInstallPath;

            return null;
        }

        [SupportedOSPlatform("windows")]
        private string getStableInstallPathFromRegistry()
        {
            using (RegistryKey key = Registry.ClassesRoot.OpenSubKey("osu"))
                return key?.OpenSubKey(@"shell\open\command")?.GetValue(string.Empty)?.ToString()?.Split('"')[1].Replace("osu!.exe", "");
        }

        protected override UpdateManager CreateUpdateManager()
        {
            string packageManaged = Environment.GetEnvironmentVariable("OSU_EXTERNAL_UPDATE_PROVIDER");

            if (!string.IsNullOrEmpty(packageManaged))
                return new NoActionUpdateManager();

            switch (RuntimeInfo.OS)
            {
                default:
                    return new SimpleUpdateManager();
            }
        }

        private DependencyContainer dependencies;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent) =>
            dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

        protected override void LoadComplete()
        {
            base.LoadComplete();

            LoadComponentAsync(new DiscordRichPresence(), Add);

            if (RuntimeInfo.OS == RuntimeInfo.Platform.Windows)
                LoadComponentAsync(new GameplayWinKeyBlocker(), Add);

            dBusManagerContainer = new DBusManagerContainer(
                true,
                MConfig.GetBindable<bool>(MSetting.DBusIntegration));

            dependencies.Cache(dBusManagerContainer.DBusManager);
            Add(dBusManagerContainer);
            dBusManagerContainer.NotificationAction += n => Notifications.Post(n);

            LoadComponentAsync(new ElevatedPrivilegesChecker(), Add);

            MConfig.BindWith(MSetting.AllowWindowFadeEffect, allowWindowFade);

            windowOpacity = new BindableFloat
            {
                Value = allowWindowFade.Value
                    ? 0
                    : 1
            };

            windowOpacity.BindValueChanged(v => SetWindowOpacity(v.NewValue), true);
        }

        private readonly BindableBool allowWindowFade = new BindableBool();

        protected override void ScreenChanged(IScreen lastScreen, IScreen newScreen)
        {
            base.ScreenChanged(lastScreen, newScreen);

            switch (newScreen)
            {
                case IntroScreen introScreen:
                    if (!(lastScreen is Disclaimer) && allowWindowFade.Value)
                        TransformWindowOpacity(0, introScreen.FadeOutTime - 1);

                    break;

                case Disclaimer _:
                    if (!(lastScreen is IntroScreen) && allowWindowFade.Value)
                        TransformWindowOpacity(1, 300);
                    break;
            }
        }

        private BindableFloat windowOpacity;

        public override void ForceWindowFadeIn() => TransformWindowOpacity(1, 300);

        public override void SetHost(GameHost host)
        {
            base.SetHost(host);

            var iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetType(), "lazer.ico");

            var desktopWindow = (SDL2DesktopWindow)host.Window;

            desktopWindow.CursorState |= CursorState.Hidden;
            desktopWindow.SetIconFromStream(iconStream);
            desktopWindow.Title = Name;
            desktopWindow.DragDrop += f => fileDrop(new[] { f });

            desktopWindow.MinimumSize = new Size(800, 600);
        }

        private readonly List<string> importableFiles = new List<string>();
        private ScheduledDelegate importSchedule;

        private void fileDrop(string[] filePaths)
        {
            lock (importableFiles)
            {
                string firstExtension = Path.GetExtension(filePaths.First());

                if (filePaths.Any(f => Path.GetExtension(f) != firstExtension)) return;

                importableFiles.AddRange(filePaths);

                Logger.Log($"Adding {filePaths.Length} files for import");

                // File drag drop operations can potentially trigger hundreds or thousands of these calls on some platforms.
                // In order to avoid spawning multiple import tasks for a single drop operation, debounce a touch.
                importSchedule?.Cancel();
                importSchedule = Scheduler.AddDelayed(handlePendingImports, 100);
            }
        }

        private void handlePendingImports()
        {
            lock (importableFiles)
            {
                Logger.Log($"Handling batch import of {importableFiles.Count} files");

                string[] paths = importableFiles.ToArray();
                importableFiles.Clear();

                Task.Factory.StartNew(() => Import(paths), TaskCreationOptions.LongRunning);
            }
        }

        public void TransformWindowOpacity(float final, double duration = 0, Easing easing = Easing.None) =>
            this.TransformBindableTo(windowOpacity, final, duration, easing);

        public void SetWindowOpacity(float value) => ((SDL2DesktopWindow)Window).Opacity = value;
    }
}
