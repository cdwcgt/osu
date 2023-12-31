// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Net.Http;
using osu.Framework.IO.Network;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online.API.Requests
{
    public class GetBeatmapAttributesRequest : APIRequest<APIBeatmapDifficultyAttributesResponse>
    {
        public readonly int BeatmapId;
        public readonly string? Ruleset;
        public readonly string? Mods;

        public GetBeatmapAttributesRequest(int beatmapId, string? mods = null, string? ruleset = null)
        {
            BeatmapId = beatmapId;
            Mods = mods;
            Ruleset = ruleset;
        }

        protected override WebRequest CreateWebRequest()
        {
            var request = base.CreateWebRequest();
            request.Method = HttpMethod.Post;

            if (Mods != null)
            {
                request.AddParameter("mods", Mods);
            }

            if (Ruleset != null)
            {
                request.AddParameter("ruleset", Ruleset);
            }

            return request;
        }

        protected override string Target => $@"beatmaps/{BeatmapId}/attributes";
    }
}
