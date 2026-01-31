// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;

namespace osu.Game.Tournament.Github.Online
{
    public class GitRefResponse
    {
        [JsonProperty("object")]
        public GitRefObject Object { get; set; } = null!;
    }

    public class GitRefObject
    {
        [JsonProperty("sha")]
        public string Sha { get; set; } = string.Empty;
    }

    public class ContentResponse
    {
        [JsonProperty("sha")]
        public string Sha { get; set; } = string.Empty;

        [JsonProperty("content")]
        public string? Content { get; set; }

        [JsonProperty("encoding")]
        public string? Encoding { get; set; }
    }

    public class PullRequestResponse
    {
        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;
    }

    public class CreateRefRequest
    {
        [JsonProperty("ref")]
        public string Ref { get; set; } = string.Empty;

        [JsonProperty("sha")]
        public string Sha { get; set; } = string.Empty;
    }

    public class CreateOrUpdateFileRequest
    {
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;

        [JsonProperty("branch")]
        public string Branch { get; set; } = string.Empty;

        [JsonProperty("sha")]
        public string? Sha { get; set; }
    }

    public class CreatePullRequestRequest
    {
        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("body")]
        public string Body { get; set; } = string.Empty;

        [JsonProperty("head")]
        public string Head { get; set; } = string.Empty;

        [JsonProperty("base")]
        public string Base { get; set; } = string.Empty;
    }
}
