// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Game.Tournament.Github
{
    public class GithubConfig
    {
        public static string Owner => "osu-mania-Chinese-National-Cup";
        public static string Repo => "osu-mania-Chinese-National-Cup"; // 不要带 .git
        public static string BaseBranch => "main"; // 目标分支
        public static string APIVersion => "2022-11-28"; // GitHub API 版本，可留空

        public static string NewBranch => $"bot/update-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
        public static string PrTitle => $"Automated update {DateTime.UtcNow:yyyyMMdd-HHmmss}";
        public static string PrBody => "Updating bracket";
    }
}
