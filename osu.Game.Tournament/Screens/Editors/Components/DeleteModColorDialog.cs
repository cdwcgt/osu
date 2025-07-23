// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Overlays.Dialog;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.Screens.Editors.Components
{
    public partial class DeleteModColorDialog : DeletionDialog
    {
        public DeleteModColorDialog(ModColor color, Action action)
        {
            HeaderText = color.ModName.Length > 0 ? $@"Delete mod ""{color.ModName.Length}"" color?" : @"Delete unnamed mod color?";
            DangerousAction = action;
        }
    }
}
