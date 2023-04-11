// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Newtonsoft.Json;

namespace osu.Game.Online.API.Requests.Responses;

public class APIAccoungStanding
{
    [JsonProperty(@"description")]
    public string Description = null!;

    [JsonProperty(@"id")]
    public int Id;

    [JsonProperty(@"length")]
    public int Length;

    [JsonProperty(@"permanent")]
    public bool Permanent;

    [JsonProperty(@"timestamp")]
    public DateTimeOffset TimeStamp;

    [JsonProperty(@"type")]
    private string type
    {
        set => Type = Enum.Parse<AccountHistoryType>(value, true);
    }

    public AccountHistoryType Type;

    public enum AccountHistoryType
    {
        Note,
        Restriction,
        Silence
    }
}



