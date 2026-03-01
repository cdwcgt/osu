// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
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
using osu.Game.Tournament.Configuration;
using osu.Game.Tournament.Github.Online;
using osu.Game.Tournament.IO;
using osu.Game.Tournament.Models;

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

        [Resolved]
        private TournamentConfigManager config { get; set; } = null!;

        public async Task UploadAsync(CancellationToken cancellationToken = default)
        {
            if (!storage.Exists(TournamentGameBase.BRACKET_FILENAME))
            {
                Logger.Log($"Bracket upload aborted: {TournamentGameBase.BRACKET_FILENAME} does not exist.");
                return;
            }

            string? token = GithubConfig.GithubToken;

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

            string savedSha = config.Get<string>(StorageConfig.LastGithubCommitSha);
            string baseSha = string.IsNullOrWhiteSpace(savedSha)
                ? await GithubApiClient.GetBaseBranchShaAsync(token, cancellationToken).ConfigureAwait(false)
                : savedSha;

            await ensureBranch(token, baseSha, newBranch, cancellationToken).ConfigureAwait(false);

            string? existingFileSha = await getFileSha(token, newBranch, cancellationToken).ConfigureAwait(false);
            await putFile(token, bracketJson, TournamentGameBase.BRACKET_FILENAME, existingFileSha, newBranch, cancellationToken).ConfigureAwait(false);

            string prUrl = await createPullRequest(token, newBranch, prTitle, prBody, cancellationToken).ConfigureAwait(false);
            host.OpenUrlExternally(prUrl);
            Logger.Log($"Bracket upload complete. PR created: {prUrl}");
        }

        public async Task UploadByMatchAsync(TournamentMatch match, CancellationToken cancellationToken = default)
        {
            string? token = GithubConfig.GithubToken;

            if (string.IsNullOrWhiteSpace(token))
            {
                Logger.Log("Bracket upload aborted: GITHUB_TOKEN is not set.");
                return;
            }

            string baseSha = await GithubApiClient.GetBaseBranchShaAsync(token, cancellationToken).ConfigureAwait(false);

            byte[] bracketBytes = await BracketDownloader.GetJsonBytes(baseSha, cancellationToken).ConfigureAwait(false);

            // 希望够用
            var bracket = JsonConvert.DeserializeObject<LadderInfo>(Encoding.UTF8.GetString(bracketBytes), new JsonPointConverter());
            if (bracket == null)
                throw new InvalidOperationException("Failed to resolve bracket from GitHub.");

            var existing = bracket.Matches.FirstOrDefault(m => m.ID == match.ID)
                           ?? throw new InvalidOperationException("Failed to get match from Github bracket.");

            int idx = bracket.Matches.IndexOf(existing);
            bracket.Matches[idx] = match;

            string bracketJson = JsonConvert.SerializeObject(bracket,
                new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    Converters = new JsonConverter[] { new JsonPointConverter() }
                });

            string newBranch = GithubConfig.NewBranch;
            string prTitle = $"Match: {match.ID}, {GithubConfig.PrTitle}";
            string prBody = GithubConfig.PrBody;

            await ensureBranch(token, baseSha, newBranch, cancellationToken).ConfigureAwait(false);

            string? existingFileSha = await getFileSha(token, newBranch, cancellationToken).ConfigureAwait(false);
            await putFile(token, bracketJson, TournamentGameBase.BRACKET_FILENAME, existingFileSha, newBranch, cancellationToken).ConfigureAwait(false);

            string prUrl = await createPullRequest(token, newBranch, prTitle, prBody, cancellationToken).ConfigureAwait(false);
            host.OpenUrlExternally(prUrl);
            Logger.Log($"Bracket upload complete. PR created: {prUrl}");
        }

        private async Task ensureBranch(string token, string baseSha, string newBranch, CancellationToken cancellationToken)
        {
            string? existingSha = await getBranchSha(token, newBranch, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(existingSha))
                return;

            string url = GithubApiClient.CreateRepoUrl("git/refs");
            var payload = new CreateRefRequest
            {
                Ref = $"refs/heads/{newBranch}",
                Sha = baseSha
            };

            await GithubApiClient.SendJsonAsync<object>(HttpMethod.Post, url, token, payload, cancellationToken).ConfigureAwait(false);
        }

        private async Task<string?> getBranchSha(string token, string newBranch, CancellationToken cancellationToken)
        {
            string url = GithubApiClient.CreateRepoUrl($"git/ref/heads/{newBranch}");

            try
            {
                GitRefResponse response = await GithubApiClient.SendJsonAsync<GitRefResponse>(HttpMethod.Get, url, token, null, cancellationToken).ConfigureAwait(false);
                return response.Object?.Sha;
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> getFileSha(string token, string branch, CancellationToken cancellationToken)
        {
            string path = TournamentGameBase.BRACKET_FILENAME.Replace('\\', '/');
            string url = $"{GithubApiClient.CreateRepoUrl($"contents/{path}")}?ref={branch}";

            try
            {
                ContentResponse response = await GithubApiClient.SendJsonAsync<ContentResponse>(HttpMethod.Get, url, token, null, cancellationToken).ConfigureAwait(false);
                return response.Sha;
            }
            catch
            {
                return null;
            }
        }

        private async Task putFile(string token, string bracketJson, string fileName, string? existingSha, string newBranch, CancellationToken cancellationToken)
        {
            string path = fileName.Replace('\\', '/');
            string url = GithubApiClient.CreateRepoUrl($"contents/{path}");

            string contentBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(bracketJson));
            var payload = new CreateOrUpdateFileRequest
            {
                Message = $"Update {path}",
                Content = contentBase64,
                Branch = newBranch,
                Sha = string.IsNullOrWhiteSpace(existingSha) ? null : existingSha
            };

            await GithubApiClient.SendJsonAsync<object>(HttpMethod.Put, url, token, payload, cancellationToken).ConfigureAwait(false);
        }

        private async Task<string> createPullRequest(string token, string newBranch, string title, string body, CancellationToken cancellationToken)
        {
            string url = GithubApiClient.CreateRepoUrl("pulls");
            var payload = new CreatePullRequestRequest
            {
                Title = title,
                Body = $"{body}, by {api.ProvidedUsername}",
                Head = newBranch,
                Base = GithubConfig.BaseBranch
            };

            PullRequestResponse response = await GithubApiClient.SendJsonAsync<PullRequestResponse>(HttpMethod.Post, url, token, payload, cancellationToken).ConfigureAwait(false);
            string htmlUrl = response.HtmlUrl;

            if (string.IsNullOrWhiteSpace(htmlUrl))
                throw new InvalidOperationException("Failed to resolve PR URL from GitHub.");

            return htmlUrl;
        }
    }
}
