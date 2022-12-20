﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Platform;
using osu.Game.Configuration;
using osu.Game.Online.API;
using osu.Game.Overlays.Notifications;

namespace osu.Game.Updater
{
    /// <summary>
    /// An update manager that shows notifications if a newer release is detected.
    /// Installation is left up to the user.
    /// </summary>
    public partial class SimpleUpdateManager : UpdateManager
    {
        private string version;

        [Resolved]
        private GameHost host { get; set; }

        [Resolved]
        private OsuConfigManager config { get; set; }

        [BackgroundDependencyLoader]
        private void load(OsuGameBase game)
        {
            version = game.Version;
        }

        protected override async Task<bool> PerformUpdateCheck()
        {
            try
            {
                bool useOfficalReleaseStream = config.Get<ReleaseStream>(OsuSetting.ReleaseStream) == ReleaseStream.Lazer;

                var releases = new OsuJsonWebRequest<GitHubRelease>($"https://api.github.com/repos/{(useOfficalReleaseStream ? "ppy" : "MATRIX-feather")}/osu/releases/latest");

                await releases.PerformAsync().ConfigureAwait(false);

                var latest = releases.ResponseObject;

                // avoid any discrepancies due to build suffixes for now.
                // eventually we will want to support release streams and consider these.
                version = version.Split('-').First();
                string latestTagName = latest.TagName.Split('-').First();

                if (latestTagName != version)
                {
                    Notifications.Post(new SimpleNotification
                    {
                        Text = "osu!已有新版本可用!\n"
                               + $"你的版本{version}\n"
                               + $"最新版本{latest.TagName}.\n\n"
                               + $"点击这里前往github{(useOfficalReleaseStream ? "下载" : "查看")}",
                        Icon = FontAwesome.Solid.Upload,
                        Activated = () =>
                        {
                            host.OpenUrlExternally(getBestUrl(latest));
                            return true;
                        }
                    });

                    return true;
                }
            }
            catch
            {
                // we shouldn't crash on a web failure. or any failure for the matter.
                return true;
            }

            return false;
        }

        private string getBestUrl(GitHubRelease release)
        {
            GitHubAsset bestAsset = null;

            switch (RuntimeInfo.OS)
            {
                case RuntimeInfo.Platform.Windows:
                    bestAsset = release.Assets?.Find(f => f.Name.EndsWith(".exe", StringComparison.Ordinal));
                    break;

                case RuntimeInfo.Platform.macOS:
                    string arch = RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "Apple.Silicon" : "Intel";
                    bestAsset = release.Assets?.Find(f => f.Name.EndsWith($".app.{arch}.zip", StringComparison.Ordinal));
                    break;

                case RuntimeInfo.Platform.Linux:
                    bestAsset = release.Assets?.Find(f => f.Name.EndsWith(".AppImage", StringComparison.Ordinal));
                    break;

                case RuntimeInfo.Platform.iOS:
                    // iOS releases are available via testflight. this link seems to work well enough for now.
                    // see https://stackoverflow.com/a/32960501
                    return "itms-beta://beta.itunes.apple.com/v1/app/1447765923";

                case RuntimeInfo.Platform.Android:
                    // on our testing device this causes the download to magically disappear.
                    //bestAsset = release.Assets?.Find(f => f.Name.EndsWith(".apk"));
                    break;
            }

            return bestAsset?.BrowserDownloadUrl ?? release.HtmlUrl;
        }
    }
}
