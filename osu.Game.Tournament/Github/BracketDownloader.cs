// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Logging;
using osu.Game.Online.API;
using osu.Game.Tournament.Github.Online;
using osu.Game.Tournament.IO;

namespace osu.Game.Tournament.Github
{
    public partial class BracketDownloader : Component
    {
        [Resolved]
        private TournamentStorage storage { get; set; } = null!;

        private const string github_api_base = "https://api.github.com";

        public async Task DownloadAsync(CancellationToken cancellationToken = default)
        {
            string path = TournamentGameBase.BRACKET_FILENAME.Replace('\\', '/');
            string url = $"{github_api_base}/repos/{GithubConfig.Owner}/{GithubConfig.Repo}/contents/{path}?ref={GithubConfig.BaseBranch}";
            string? token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");

            ContentResponse response = await sendJson<ContentResponse>(HttpMethod.Get, url, token, null, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(response.Content))
            {
                Logger.Log($"Bracket download aborted: {path} content is empty.");
                return;
            }

            if (!string.Equals(response.Encoding, "base64", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Unexpected GitHub content encoding: {response.Encoding}");

            byte[] bytes = Convert.FromBase64String(response.Content.Replace("\n", string.Empty).Replace("\r", string.Empty));

            using (Stream stream = storage.GetStream(TournamentGameBase.BRACKET_FILENAME, FileAccess.Write, FileMode.Create))
                await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);

            Logger.Log($"Bracket download complete: {path} updated.");
        }

        private static async Task<TResponse> sendJson<TResponse>(HttpMethod method, string url, string? token, object? payload, CancellationToken cancellationToken)
        {
            using var request = new OsuJsonWebRequest<TResponse>(url)
            {
                Method = method,
                ContentType = "application/json"
            };

            request.AddHeader("Accept", "application/vnd.github+json");
            if (!string.IsNullOrWhiteSpace(GithubConfig.APIVersion))
                request.AddHeader("X-GitHub-Api-Version", GithubConfig.APIVersion);
            if (!string.IsNullOrWhiteSpace(token))
                request.AddHeader("Authorization", $"Bearer {token}");

            if (payload != null)
                request.AddRaw(JsonConvert.SerializeObject(payload));

            await request.PerformAsync(cancellationToken).ConfigureAwait(false);

            return request.ResponseObject;
        }
    }
}
