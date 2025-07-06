// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using osuTK.Graphics.ES30;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Game.Tournament.Components
{
    public class MemoryTextureUpload : ITextureUpload
    {
        public readonly Rgba32[] PixelData;

        /// <summary>
        /// 构造：rawBytes 必须是 Bitmap.LockBits 拷出来的 BGRA 原始数据，
        /// 并且行长 = width * 4（无额外填充）。
        /// </summary>
        public MemoryTextureUpload(int width, int height)
        {
            PixelData = new Rgba32[width * height];
            Bounds = new RectangleI(0, 0, width, height);
        }

        /// <summary>
        /// 转换后的 Rgba32 数据
        /// </summary>
        public ReadOnlySpan<Rgba32> Data => PixelData;

        /// <summary>
        /// 目标 Mipmap 级别，一般用 0
        /// </summary>
        public int Level => 0;

        /// <summary>
        /// 上传区域，通常整个纹理
        /// </summary>
        public RectangleI Bounds { get; set; }

        /// <summary>
        /// 通知框架这是 RGBA 排布
        /// </summary>
        public PixelFormat Format => PixelFormat.Rgba;

        public void Dispose()
        {
            // 这里不需要释放额外资源
        }
    }
}
