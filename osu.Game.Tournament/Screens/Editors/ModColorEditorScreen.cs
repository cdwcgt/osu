// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;
using osu.Game.Tournament.Models;
using osu.Game.Tournament.Screens.Editors.Components;
using osuTK;

namespace osu.Game.Tournament.Screens.Editors
{
    public partial class ModColorEditorScreen : TournamentEditorScreen<ModColorEditorScreen.ModColorRow, ModColor>
    {
        protected override BindableList<ModColor> Storage => LadderInfo.ModColors;

        public partial class ModColorRow : CompositeDrawable, IModelBacked<ModColor>
        {
            public ModColor Model { get; set; }

            [Resolved]
            private LadderInfo ladderInfo { get; set; } = null!;

            [Resolved]
            private IDialogOverlay? dialogOverlay { get; set; }

            [Resolved]
            private TournamentSceneManager? sceneManager { get; set; }

            private readonly Bindable<string> modName = new Bindable<string>(string.Empty);
            private readonly Bindable<string> textColor = new Bindable<string>("#FFFFFF");
            private readonly Bindable<string> backgroundColor = new Bindable<string>("#000000");

            private readonly Box background;
            private readonly TournamentSpriteText text;

            public ModColorRow(ModColor modColor)
            {
                Model = modColor;

                Masking = true;
                CornerRadius = 10;

                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;

                modName.Value = Model.ModName;
                textColor.Value = modColor.TextColor.ToHex();
                backgroundColor.Value = modColor.BackgroundColor.ToHex();

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        Colour = OsuColour.Gray(0.1f),
                        RelativeSizeAxes = Axes.Both,
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
                            new SettingsTextBox
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                LabelText = "Mod名称",
                                Width = 0.2f,
                                Current = modName
                            },
                            new SettingsTextBox
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                LabelText = "文字颜色",
                                Width = 0.3f,
                                Current = textColor
                            },
                            new SettingsTextBox
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                LabelText = "背景颜色",
                                Width = 0.3f,
                                Current = backgroundColor
                            },
                            new Container
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                RelativeSizeAxes = Axes.X,
                                Width = 0.1f,
                                Height = 40,
                                Padding = new MarginPadding(5),
                                Children = new Drawable[]
                                {
                                    background = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                    },
                                    text = new TournamentSpriteText
                                    {
                                        Origin = Anchor.Centre,
                                        Anchor = Anchor.Centre,
                                    }
                                }
                            }
                        }
                    },
                    new DangerousSettingsButton
                    {
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                        RelativeSizeAxes = Axes.None,
                        Width = 150,
                        Text = "Delete Round",
                        Action = () => dialogOverlay?.Push(new DeleteModColorDialog(Model, () =>
                        {
                            Expire();
                            ladderInfo.ModColors.Remove(Model);
                        }))
                    }
                };

                modName.BindValueChanged(m =>
                {
                    Model.ModName = m.NewValue;
                    text.Text = m.NewValue;
                }, true);

                textColor.BindValueChanged(c =>
                {
                    if (Colour4.TryParseHex(c.NewValue, out var colour))
                    {
                        Model.TextColor = colour;
                        text.Colour = colour;
                    }
                }, true);

                backgroundColor.BindValueChanged(c =>
                {
                    if (Colour4.TryParseHex(c.NewValue, out var colour))
                    {
                        Model.BackgroundColor = colour;
                        background.Colour = colour;
                    }
                }, true);
            }
        }

        protected override ModColorRow CreateDrawable(ModColor model) => new ModColorRow(model);
    }
}
