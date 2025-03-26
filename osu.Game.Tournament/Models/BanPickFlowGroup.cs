// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace osu.Game.Tournament.Models
{
    public class BanPickFlowGroup
    {
        public Bindable<string> Name = new Bindable<string>(string.Empty);

        public BindableList<BanPickFlowStep> Steps = new BindableList<BanPickFlowStep>();

        public Bindable<int?> RepeatCount = new Bindable<int?>(0);
    }
}
