// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
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

            if (string.IsNullOrWhiteSpace(GithubConfig.BracketUploadServiceUrl))
            {
                Logger.Log("Bracket upload aborted: BRACKET_UPLOADER_URL is not set.");
                return;
            }

            string bracketJson;
            using (Stream stream = storage.GetStream(TournamentGameBase.BRACKET_FILENAME, FileAccess.Read, FileMode.Open))
            using (var sr = new StreamReader(stream, Encoding.UTF8))
                bracketJson = await sr.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

            string savedSha = config.Get<string>(StorageConfig.LastGithubCommitSha);
            var request = BracketUploadRequest.CreateFull(
                bracketJson,
                string.IsNullOrWhiteSpace(savedSha) ? null : savedSha,
                api.ProvidedUsername);

            await api.PerformAsync(request).ConfigureAwait(false);

            string prUrl = request.Response?.PullRequestUrl
                           ?? throw new InvalidOperationException("Failed to resolve PR URL from bracket upload service.");
            host.OpenUrlExternally(prUrl);
            Logger.Log($"Bracket upload complete. PR created: {prUrl}");
        }

        public async Task UploadByMatchAsync(TournamentMatch match, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(GithubConfig.BracketUploadServiceUrl))
            {
                Logger.Log("Bracket upload aborted: BRACKET_UPLOADER_URL is not set.");
                return;
            }

            string matchJson = JsonConvert.SerializeObject(match,
                new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    Converters = new JsonConverter[] { new JsonPointConverter() }
                });

            var request = BracketUploadRequest.CreateMatch(match.ID, matchJson, api.ProvidedUsername);

            await api.PerformAsync(request).ConfigureAwait(false);

            string prUrl = request.Response?.PullRequestUrl
                           ?? throw new InvalidOperationException("Failed to resolve PR URL from bracket upload service.");
            host.OpenUrlExternally(prUrl);
            Logger.Log($"Bracket upload complete. PR created: {prUrl}");
        }
    }
}
