// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;

namespace osu.Game.Resources.Custom
{
    public static class CustomResources
    {
        public static Assembly ResourceAssembly => typeof(CustomResources).Assembly;
    }
}
