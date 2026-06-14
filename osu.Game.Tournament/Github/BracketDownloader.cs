// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Logging;
using osu.Game.Online.API;
using osu.Game.Tournament.Configuration;
using osu.Game.Tournament.Github.Online;
using osu.Game.Tournament.IO;

namespace osu.Game.Tournament.Github
{
    public partial class BracketDownloader : Component
    {
        [Resolved]
        private TournamentStorage storage { get; set; } = null!;

        [Resolved]
        private TournamentConfigManager config { get; set; } = null!;

        public async Task DownloadAsync(CancellationToken cancellationToken = default)
        {
            string path = TournamentGameBase.BRACKET_FILENAME.Replace('\\', '/');

            byte[] bytes = await GetJsonBytes(GithubConfig.BaseBranch, cancellationToken).ConfigureAwait(false);

            using (Stream stream = storage.CreateFileSafely(TournamentGameBase.BRACKET_FILENAME))
                await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);

            string baseSha = await GithubApiClient.GetBaseBranchShaAsync(GithubConfig.GithubToken, cancellationToken).ConfigureAwait(false);
            config.SetValue(StorageConfig.LastGithubCommitSha, baseSha);

            Logger.Log($"Bracket download complete: {path} updated.");
        }

        public static async Task<byte[]> GetJsonBytes(string refCommit, CancellationToken cancellationToken = default)
        {
            string path = TournamentGameBase.BRACKET_FILENAME.Replace('\\', '/');
            string url = $"{GithubApiClient.CreateRepoUrl($"contents/{path}")}?ref={refCommit}";

            ContentResponse response = await GithubApiClient.SendJsonAsync<ContentResponse>(HttpMethod.Get, url, GithubConfig.GithubToken, null, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(response.Content) && string.IsNullOrWhiteSpace(response.DownloadUrl))
                throw new InvalidOperationException($"Bracket download aborted: {path} content is empty.");

            byte[] bytes;

            if (string.Equals(response.Encoding, "base64", StringComparison.OrdinalIgnoreCase))
            {
                bytes = Convert.FromBase64String(response.Content!.Replace("\n", string.Empty).Replace("\r", string.Empty));
            }
            else
            {
                var request = new OsuWebRequest(response.DownloadUrl!);
                await request.PerformAsync(cancellationToken).ConfigureAwait(false);

                bytes = request.GetResponseData() ?? throw new InvalidOperationException("Failed to get response data.");
            }

            return bytes;
        }
    }
}
