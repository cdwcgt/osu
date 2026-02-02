// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using osu.Game.Overlays.Dialog;

namespace osu.Game.Tournament
{
    public partial class TournamentConfirmExitDialog : PopupDialog
    {
        private readonly Action onConfirm;
        private readonly Action? onCancel;

        /// <summary>
        /// Construct a new exit confirmation dialog.
        /// </summary>
        /// <param name="onConfirm">An action to perform on confirmation.</param>
        /// <param name="onCancel">An optional action to perform on cancel.</param>
        public TournamentConfirmExitDialog(Action onConfirm, Action? onCancel = null)
        {
            this.onConfirm = onConfirm;
            this.onCancel = onCancel;
        }

        [BackgroundDependencyLoader]
        private void load(SaveChangesOverlay saveChangesOverlay)
        {
            HeaderText = "Are you sure you want to exit osu!?";

            Icon = FontAwesome.Solid.ExclamationTriangle;

            if (saveChangesOverlay.HaveUnsaveChange)
            {
                BodyText = "Have unsave bracket change!";

                Buttons = new PopupDialogButton[]
                {
                    new PopupDialogDangerousButton
                    {
                        Text = @"Let me out!",
                        Action = onConfirm
                    },
                    new PopupDialogCancelButton
                    {
                        Text = "Back",
                        Action = onCancel
                    },
                };
            }
            else
            {
                BodyText = "Last chance to turn back";

                Buttons = new PopupDialogButton[]
                {
                    new PopupDialogOkButton
                    {
                        Text = @"Let me out!",
                        Action = onConfirm
                    },
                    new PopupDialogCancelButton
                    {
                        Text = @"Just a little more...",
                        Action = onCancel
                    },
                };
            }
        }
    }
}
