// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Net.Http;
using Newtonsoft.Json;
using osu.Framework.IO.Network;
using osu.Game.Online.API;

namespace osu.Game.Tournament.Github.Online
{
    public class BracketUploadRequest : APIRequest<BracketUploadResponse>
    {
        private readonly BracketUploadPayload payload;
        private readonly string? providedUsername;

        protected override string Uri
        {
            get
            {
                if (string.IsNullOrWhiteSpace(GithubConfig.BracketUploadServiceUrl))
                    throw new NotSupportedException("Bracket uploader service URL is not configured.");

                return $"{GithubConfig.BracketUploadServiceUrl.TrimEnd('/')}/uploads/bracket";
            }
        }

        protected override string Target => throw new NotSupportedException();

        private BracketUploadRequest(BracketUploadPayload payload, string? providedUsername)
        {
            this.payload = payload;
            this.providedUsername = providedUsername;
        }

        public static BracketUploadRequest CreateFull(string bracketJson, string? baseSha, string? providedUsername) =>
            new BracketUploadRequest(
                new BracketUploadPayload
                {
                    BracketJson = bracketJson,
                    BaseSha = string.IsNullOrWhiteSpace(baseSha) ? null : baseSha,
                },
                providedUsername);

        public static BracketUploadRequest CreateMatch(int matchId, string matchJson, string? providedUsername) =>
            new BracketUploadRequest(
                new BracketUploadPayload
                {
                    MatchId = matchId,
                    MatchJson = matchJson,
                },
                providedUsername);

        protected override WebRequest CreateWebRequest()
        {
            var req = base.CreateWebRequest();
            req.Method = HttpMethod.Post;
            req.ContentType = "application/json";
            req.AddRaw(JsonConvert.SerializeObject(payload));

            if (!string.IsNullOrWhiteSpace(providedUsername))
                req.AddHeader("X-Osu-Username", providedUsername);

            return req;
        }

        private class BracketUploadPayload
        {
            [JsonProperty("bracket_json", NullValueHandling = NullValueHandling.Ignore)]
            public string? BracketJson { get; set; }

            [JsonProperty("base_sha", NullValueHandling = NullValueHandling.Ignore)]
            public string? BaseSha { get; set; }

            [JsonProperty("match_id", NullValueHandling = NullValueHandling.Ignore)]
            public int? MatchId { get; set; }

            [JsonProperty("match_json", NullValueHandling = NullValueHandling.Ignore)]
            public string? MatchJson { get; set; }
        }
    }

    public class BracketUploadResponse
    {
        [JsonProperty("pull_request_url")]
        public string PullRequestUrl { get; set; } = string.Empty;

        [JsonProperty("branch")]
        public string Branch { get; set; } = string.Empty;

        [JsonProperty("base_sha")]
        public string BaseSha { get; set; } = string.Empty;

        [JsonProperty("update_mode")]
        public string UpdateMode { get; set; } = string.Empty;
    }
}
