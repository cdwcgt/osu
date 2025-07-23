// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays.Settings;
using osu.Game.Tournament.Models;
using osuTK;

namespace osu.Game.Tournament.Screens.Editors
{
    public partial class ModMultiplierSettingsEditorScreen : TournamentEditorScreen<ModMultiplierSettingsEditorScreen.ModMultiplierSettingsRow, ModMultiplierSetting>
    {
        protected override BindableList<ModMultiplierSetting> Storage => LadderInfo.ModMultiplierSettings;

        protected override ModMultiplierSettingsRow CreateDrawable(ModMultiplierSetting model) => new ModMultiplierSettingsRow(model);

        public partial class ModMultiplierSettingsRow : CompositeDrawable, IModelBacked<ModMultiplierSetting>
        {
            public ModMultiplierSetting Model { get; }

            public ModMultiplierSettingsRow(ModMultiplierSetting model)
            {
                Model = model;
                AutoSizeAxes = Axes.Y;
                RelativeSizeAxes = Axes.X;
                Masking = true;
                CornerRadius = 10;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = OsuColour.Gray(0.1f),
                    },
                    new FillFlowContainer
                    {
                        Margin = new MarginPadding(5),
                        Padding = new MarginPadding { Right = 160 },
                        Spacing = new Vector2(5),
                        Direction = FillDirection.Full,
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Children = new Drawable[]
                        {
                            new LabelledEnumDropdown<LegacyMods>
                            {
                                Width = 0.3f,
                                Label = "Mod",
                                Current = Model.Mods,
                            },
                            new SettingsSlider<double>
                            {
                                Width = 0.5f,
                                Current = Model.Multiplier,
                                LabelText = "Multiplier",
                                KeyboardStep = 0.1f,
                            },
                        }
                    }
                };
            }
        }
    }
}
