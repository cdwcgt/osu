// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Overlays.Settings;

namespace osu.Game.Tournament
{
    public partial class TourneyNumberBox : SettingsItem<double?>
    {
        protected override Drawable CreateControl() => new NumberControl
        {
            RelativeSizeAxes = Axes.X,
        };

        private sealed partial class NumberControl : CompositeDrawable, IHasCurrentValue<double?>
        {
            private readonly BindableWithCurrent<double?> current = new BindableWithCurrent<double?>();

            public Bindable<double?> Current
            {
                get => current.Current;
                set => current.Current = value;
            }

            public NumberControl()
            {
                AutoSizeAxes = Axes.Y;

                OutlinedNumberBox numberBox;

                InternalChildren = new[]
                {
                    numberBox = new OutlinedNumberBox
                    {
                        RelativeSizeAxes = Axes.X,
                        CommitOnFocusLost = true
                    }
                };

                numberBox.Current.BindValueChanged(e =>
                {
                    if (string.IsNullOrEmpty(e.NewValue))
                    {
                        Current.Value = null;
                        return;
                    }

                    if (double.TryParse(e.NewValue, out double intVal))
                    {
                        if (e.NewValue.EndsWith('.'))
                            return;

                        Current.Value = intVal;
                    }
                    else
                    {
                        numberBox.NotifyInputError();
                    }

                    // trigger Current again to either restore the previous text box value, or to reformat the new value via .ToString().
                    Current.TriggerChange();
                });

                Current.BindValueChanged(e =>
                {
                    numberBox.Current.Value = e.NewValue?.ToString();
                });
            }
        }

        private partial class OutlinedNumberBox : OutlinedTextBox
        {
            protected override bool AllowIme => false;

            protected override bool CanAddCharacter(char character) => char.IsAsciiDigit(character) || character == '.';

            public new void NotifyInputError() => base.NotifyInputError();
        }
    }
}
