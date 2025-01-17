// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Textures;
using osu.Game.Graphics;
using osu.Game.Tournament.Models;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Gameplay.Components.RoundInformation
{
    public abstract partial class MapBox : CompositeDrawable
    {
        public static readonly Color4 TEAM_RED = Color4Extensions.FromHex("#D43030");
        public static readonly Color4 TEAM_BLUE = Color4Extensions.FromHex("#2A82E4");

        protected readonly BeatmapChoice Choice;
        protected Container TopMapContainer = null!;

        protected Container BottomMapContainer = null!;

        protected Box CenterLine = null!;

        protected RoundBeatmap RoundBeatmap = null!;

        [Resolved]
        protected LadderInfo Ladder { get; private set; } = null!;

        [Resolved]
        protected TextureStore textures { get; private set; } = null!;

        protected string ModString
        {
            get
            {
                var modArray = Ladder.CurrentMatch.Value?.Round.Value?.Beatmaps.Where(b => b.Mods == RoundBeatmap.Mods).ToArray();
                if (modArray == null)
                    return string.Empty;

                int id = Array.FindIndex(modArray, b => b.ID == RoundBeatmap.ID) + 1;
                return $"{RoundBeatmap.Mods}{id}";
            }
        }

        protected MapBox(BeatmapChoice choice)
        {
            Choice = choice;
            Size = new Vector2(42, 110);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            TournamentRound? round = Ladder.CurrentMatch.Value?.Round.Value;

            RoundBeatmap = round?.Beatmaps.FirstOrDefault(roundMap => roundMap.ID == Choice.BeatmapID) ?? new RoundBeatmap();

            InternalChildren = new Drawable[]
            {
                TopMapContainer = new Container
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Height = 0.5f,
                    Width = 1f,
                    Padding = new MarginPadding { Bottom = 6f },
                    RelativeSizeAxes = Axes.Both,
                },
                CreateCenterLine(Choice.Team).With(d =>
                {
                    d.Anchor = Anchor.Centre;
                    d.Origin = Anchor.Centre;
                }),
                BottomMapContainer = new Container
                {
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Height = 0.5f,
                    Width = 1f,
                    Padding = new MarginPadding { Top = 6f },
                    RelativeSizeAxes = Axes.Both,
                },
            };

            UpdateStatus();
        }

        protected virtual Drawable CreateCenterLine(TeamColour color) => CreateCenterLineBox(color);

        public static Color4 GetColorFromTeamColor(TeamColour colour) => colour == TeamColour.Red ? TEAM_RED : TEAM_BLUE;

        protected virtual Box CreateCenterLineBox(TeamColour color) => CenterLine = new Box
        {
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            Height = 4f,
            RelativeSizeAxes = Axes.X,
            Colour = GetColorFromTeamColor(color)
        };

        protected virtual void UpdateStatus() => AddModContent(RoundBeatmap.BackgroundColor, RoundBeatmap.TextColor);

        protected void AddModContent(Color4 backgroundColor, Color4 textColor)
        {
            var mapBoxContent = CreateMapBoxContent(ModString, backgroundColor, textColor)
                .With(d =>
                {
                    var anchor = Choice.Team == TeamColour.Red ? Anchor.BottomCentre : Anchor.TopCentre;

                    d.Anchor = anchor;
                    d.Origin = anchor;
                });

            if (Choice.Team == TeamColour.Red)
            {
                TopMapContainer.Add(mapBoxContent);
            }
            else
            {
                BottomMapContainer.Add(mapBoxContent);
            }
        }

        protected static Drawable CreateMapBoxContent(string mapName, Color4 backgroundColor, Color4 textColor)
        {
            return new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 18f,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = backgroundColor,
                    },
                    new TournamentSpriteText
                    {
                        Text = mapName,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Colour = textColor,
                        Font = OsuFont.Torus.With(size: 18),
                        Shadow = true,
                    }
                }
            };
        }
    }
}
