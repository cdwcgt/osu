// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Tournament.Models;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Gameplay.Components.RoundInformation
{
    public partial class PickMapBox : MapBox
    {
        private static readonly Color4 fail_pick_red_color = Color4Extensions.FromHex("#7A1C1C");
        private static readonly Color4 fail_pick_blue_color = Color4Extensions.FromHex("#143A66");

        private Bindable<TeamColour?> winnerColour = new Bindable<TeamColour?>();

        public PickMapBox(BeatmapChoice choice)
            : base(choice)
        {
            winnerColour.BindTo(choice.Winner);
            winnerColour.BindValueChanged(_ => UpdateStatus());
        }

        protected override void UpdateStatus()
        {
            TopMapContainer.Clear();
            BottomMapContainer.Clear();

            if (winnerColour.Value == null)
            {
                base.UpdateStatus();
                return;
            }

            bool failedPick = Choice.Winner.Value != Choice.Team;

            addGradientBox(winnerColour.Value.Value, failedPick);

            if (failedPick)
            {
                AddModContent(Choice.Team == TeamColour.Red ? fail_pick_red_color : fail_pick_blue_color, RoundBeatmap.TextColor);
            }
            else
            {
                AddModContent(GetColorFromTeamColor(Choice.Team), RoundBeatmap.TextColor);
            }
        }

        private void addGradientBox(TeamColour color, bool failedPick)
        {
            var anchor = color == TeamColour.Red ? Anchor.BottomCentre : Anchor.TopCentre;

            var container = new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 50f,
                Anchor = anchor,
                Origin = anchor,
            };

            var cover = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Height = 0.5f,
                Anchor = color == TeamColour.Red ? Anchor.TopCentre : Anchor.BottomCentre,
                Origin = color == TeamColour.Red ? Anchor.TopCentre : Anchor.BottomCentre,
                Colour = color == TeamColour.Red
                    ? ColourInfo.GradientVertical(Color4.White.Opacity(1f), Color4.White.Opacity(0f))
                    : ColourInfo.GradientVertical(Color4.White.Opacity(0f), Color4.White.Opacity(1f)),
                Blending = new BlendingParameters
                {
                    RGBEquation = BlendingEquation.Add,
                    Source = BlendingType.Zero,
                    Destination = BlendingType.One,
                    AlphaEquation = BlendingEquation.Add,
                    SourceAlpha = BlendingType.Zero,
                    DestinationAlpha = BlendingType.OneMinusSrcAlpha
                }
            };

            var fillBox = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = GetColorFromTeamColor(color),
                Anchor = anchor,
                Origin = anchor
            };

            container.Add(fillBox);
            container.Add(cover);

            if (failedPick)
            {
                var smileIcon = textures.Get("round-information/smile");

                container.Add(new Sprite
                {
                    Texture = smileIcon,
                    Anchor = anchor,
                    Origin = anchor,
                    Scale = new Vector2(0.10f),
                    Y = color == TeamColour.Red ? -4 : 4,
                });
            }

            if (Choice.Winner.Value == TeamColour.Red)
            {
                TopMapContainer.Add(container);
            }
            else
            {
                BottomMapContainer.Add(container);
            }
        }

        protected override Drawable CreateCenterLine(TeamColour color)
        {
            var triangle = textures.Get("round-information/triangle");

            return new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 8,
                Masking = true,
                Rotation = color == TeamColour.Red ? 0 : 180,
                Children = new Drawable[]
                {
                    base.CreateCenterLineBox(color),
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Y = -1,
                        Children = new Drawable[]
                        {
                            new Sprite
                            {
                                Width = 28f,
                                Height = 17f,
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Texture = triangle,
                                EdgeSmoothness = Vector2.Zero,
                                Blending = new BlendingParameters
                                {
                                    RGBEquation = BlendingEquation.Add,
                                    Source = BlendingType.Zero,
                                    Destination = BlendingType.One,
                                    AlphaEquation = BlendingEquation.Add,
                                    SourceAlpha = BlendingType.Zero,
                                    DestinationAlpha = BlendingType.OneMinusSrcAlpha
                                },
                            },
                            new Sprite
                            {
                                Width = 15f,
                                Height = 10f,
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                FillMode = FillMode.Fit,
                                Texture = triangle,
                                EdgeSmoothness = Vector2.Zero,
                                Colour = GetColorFromTeamColor(color)
                            }
                        }
                    }
                }
            };
        }
    }
}
