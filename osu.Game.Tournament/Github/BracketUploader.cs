// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Online.API;
using osu.Game.Tournament.Github.Online;
using osu.Game.Tournament.IO;

namespace osu.Game.Tournament.Github
{
    public partial class BracketUploader : Component
    {
        [Resolved]
        private TournamentStorage storage { get; set; } = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private GameHost host { get; set; } = null!;

        private const string github_api_base = "https://api.github.com";

        public async Task UploadAsync(CancellationToken cancellationToken = default)
        {
            if (!storage.Exists(TournamentGameBase.BRACKET_FILENAME))
            {
                Logger.Log($"Bracket upload aborted: {TournamentGameBase.BRACKET_FILENAME} does not exist.");
                return;
            }

            string? token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");

            if (string.IsNullOrWhiteSpace(token))
            {
                Logger.Log("Bracket upload aborted: GITHUB_TOKEN is not set.");
                return;
            }

            string bracketJson;
            using (Stream stream = storage.GetStream(TournamentGameBase.BRACKET_FILENAME, FileAccess.Read, FileMode.Open))
            using (var sr = new StreamReader(stream, Encoding.UTF8))
                bracketJson = await sr.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

            string newBranch = GithubConfig.NewBranch;
            string prTitle = GithubConfig.PrTitle;
            string prBody = GithubConfig.PrBody;

            string baseSha = await getBaseBranchSha(token, cancellationToken).ConfigureAwait(false);
            await ensureBranch(token, baseSha, newBranch, cancellationToken).ConfigureAwait(false);

            string? existingFileSha = await getFileSha(token, cancellationToken).ConfigureAwait(false);
            await putFile(token, bracketJson, existingFileSha, newBranch, cancellationToken).ConfigureAwait(false);

            string prUrl = await createPullRequest(token, newBranch, prTitle, prBody, cancellationToken).ConfigureAwait(false);
            host.OpenUrlExternally(prUrl);
            Logger.Log($"Bracket upload complete. PR created: {prUrl}");
        }

        private async Task<string> getBaseBranchSha(string token, CancellationToken cancellationToken)
        {
            string url = $"{github_api_base}/repos/{GithubConfig.Owner}/{GithubConfig.Repo}/git/ref/heads/{GithubConfig.BaseBranch}";
            GitRefResponse response = await sendJson<GitRefResponse>(HttpMethod.Get, url, token, null, cancellationToken).ConfigureAwait(false);
            string? sha = response.Object?.Sha;

            if (string.IsNullOrWhiteSpace(sha))
                throw new InvalidOperationException("Failed to resolve base branch SHA from GitHub.");

            return sha;
        }

        private async Task ensureBranch(string token, string baseSha, string newBranch, CancellationToken cancellationToken)
        {
            string? existingSha = await getBranchSha(token, newBranch, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(existingSha))
                return;

            string url = $"{github_api_base}/repos/{GithubConfig.Owner}/{GithubConfig.Repo}/git/refs";
            var payload = new CreateRefRequest
            {
                Ref = $"refs/heads/{newBranch}",
                Sha = baseSha
            };

            await sendJson<object>(HttpMethod.Post, url, token, payload, cancellationToken).ConfigureAwait(false);
        }

        private async Task<string?> getBranchSha(string token, string newBranch, CancellationToken cancellationToken)
        {
            string url = $"{github_api_base}/repos/{GithubConfig.Owner}/{GithubConfig.Repo}/git/ref/heads/{newBranch}";

            try
            {
                GitRefResponse response = await sendJson<GitRefResponse>(HttpMethod.Get, url, token, null, cancellationToken).ConfigureAwait(false);
                return response.Object?.Sha;
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> getFileSha(string token, CancellationToken cancellationToken)
        {
            string path = TournamentGameBase.BRACKET_FILENAME.Replace('\\', '/');
            string url = $"{github_api_base}/repos/{GithubConfig.Owner}/{GithubConfig.Repo}/contents/{path}?ref={GithubConfig.BaseBranch}";

            try
            {
                ContentResponse response = await sendJson<ContentResponse>(HttpMethod.Get, url, token, null, cancellationToken).ConfigureAwait(false);
                return response.Sha;
            }
            catch
            {
                return null;
            }
        }

        private async Task putFile(string token, string bracketJson, string? existingSha, string newBranch, CancellationToken cancellationToken)
        {
            string path = TournamentGameBase.BRACKET_FILENAME.Replace('\\', '/');
            string url = $"{github_api_base}/repos/{GithubConfig.Owner}/{GithubConfig.Repo}/contents/{path}";

            string contentBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(bracketJson));
            var payload = new CreateOrUpdateFileRequest
            {
                Message = $"Update {path}",
                Content = contentBase64,
                Branch = newBranch,
                Sha = string.IsNullOrWhiteSpace(existingSha) ? null : existingSha
            };

            await sendJson<object>(HttpMethod.Put, url, token, payload, cancellationToken).ConfigureAwait(false);
        }

        private async Task<string> createPullRequest(string token, string newBranch, string title, string body, CancellationToken cancellationToken)
        {
            string url = $"{github_api_base}/repos/{GithubConfig.Owner}/{GithubConfig.Repo}/pulls";
            var payload = new CreatePullRequestRequest
            {
                Title = title,
                Body = $"{body}, by {api.ProvidedUsername}",
                Head = newBranch,
                Base = GithubConfig.BaseBranch
            };

            PullRequestResponse response = await sendJson<PullRequestResponse>(HttpMethod.Post, url, token, payload, cancellationToken).ConfigureAwait(false);
            string htmlUrl = response.HtmlUrl;

            if (string.IsNullOrWhiteSpace(htmlUrl))
                throw new InvalidOperationException("Failed to resolve PR URL from GitHub.");

            return htmlUrl;
        }

        private static async Task<TResponse> sendJson<TResponse>(HttpMethod method, string url, string token, object? payload, CancellationToken cancellationToken)
        {
            using var request = new OsuJsonWebRequest<TResponse>(url)
            {
                Method = method,
                ContentType = "application/json"
            };

            request.AddHeader("Accept", "application/vnd.github+json");
            request.AddHeader("Authorization", $"Bearer {token}");
            if (!string.IsNullOrWhiteSpace(GithubConfig.APIVersion))
                request.AddHeader("X-GitHub-Api-Version", GithubConfig.APIVersion);

            if (payload != null)
                request.AddRaw(JsonConvert.SerializeObject(payload));

            await request.PerformAsync(cancellationToken).ConfigureAwait(false);

            return request.ResponseObject;
        }
    }
}
