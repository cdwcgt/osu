// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;

namespace osu.Game.Tournament.Models
{
    public enum MatchStructureType
    {
        [Description("普通团队1v1")]
        HeadToHead = 2,

        [Description("四个队伍")]
        FourTeams = 4,
    }
}
