// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using osu.Game.Online.API;
using osu.Game.Tournament.Github.Online;

namespace osu.Game.Tournament.Github
{
    public static class GithubApiClient
    {
        private const string github_api_base = "https://api.github.com";

        public static string CreateRepoUrl(string relativePath)
            => $"{github_api_base}/repos/{GithubConfig.Owner}/{GithubConfig.Repo}/{relativePath.TrimStart('/')}";

        public static async Task<string> GetBaseBranchShaAsync(string? token, CancellationToken cancellationToken)
        {
            string url = CreateRepoUrl($"git/ref/heads/{GithubConfig.BaseBranch}");
            GitRefResponse response = await SendJsonAsync<GitRefResponse>(HttpMethod.Get, url, token, null, cancellationToken).ConfigureAwait(false);
            string? sha = response.Object?.Sha;

            if (string.IsNullOrWhiteSpace(sha))
                throw new InvalidOperationException("Failed to resolve base branch SHA from GitHub.");

            return sha;
        }

        public static async Task<TResponse> SendJsonAsync<TResponse>(HttpMethod method, string url, string? token, object? payload, CancellationToken cancellationToken)
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
