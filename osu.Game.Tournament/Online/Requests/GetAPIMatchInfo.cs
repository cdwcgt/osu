// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.IO.Network;
using osu.Game.Online.API;
using osu.Game.Tournament.Online.Requests.Responses;

namespace osu.Game.Tournament.Online.Requests
{
    public class GetAPIMatchInfo : APIRequest<APIMatchInfo>
    {
        public int MatchID;
        public int? BeforeEvent;
        public int? AfterEvent;

        public GetAPIMatchInfo(int matchID)
        {
            MatchID = matchID;
        }

        protected override WebRequest CreateWebRequest()
        {
            var req = base.CreateWebRequest();

            if (BeforeEvent.HasValue)
            {
                req.AddParameter("before", BeforeEvent.ToString());
            }

            if (AfterEvent.HasValue)
            {
                req.AddParameter("after", AfterEvent.ToString());
            }

            return req;
        }

        protected override string Target => @$"matches/{MatchID}";
    }
}
