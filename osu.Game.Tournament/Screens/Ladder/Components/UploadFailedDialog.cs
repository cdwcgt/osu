// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Platform;
using osu.Game.Overlays.Dialog;

namespace osu.Game.Tournament.Screens.Ladder.Components
{
    public partial class UploadFailedDialog : PopupDialog
    {
        [Resolved]
        private Storage storage { get; set; } = null!;

        [Resolved]
        private GameHost host { get; set; } = null!;

        public UploadFailedDialog(string? failedMessage)
        {
            HeaderText = "Upload failed!";
            Icon = FontAwesome.Solid.ExclamationTriangle;
            BodyText = $"{failedMessage} Please send log!!";

            Buttons = new[]
            {
                new PopupDialogOkButton
                {
                    Text = "Open log folder",
                    Action = () =>
                    {
                        host.PresentFileExternally( Path.GetFullPath(Path.Join(storage.GetFullPath(string.Empty), "..", "..", "logs")));
                    }
                }
            };
        }
    }
}
