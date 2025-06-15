// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using osuTK.Graphics.ES30;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Game.Tournament.Components
{
    public partial class MemoryTextureUpload : ITextureUpload
    {
        private readonly Rgba32[] pixelData;

        /// <summary>
        /// 构造：rawBytes 必须是 Bitmap.LockBits 拷出来的 BGRA 原始数据，
        /// 并且行长 = width * 4（无额外填充）。
        /// </summary>
        public MemoryTextureUpload(byte[] rawBytes, int width, int height)
        {
            if (rawBytes.Length != width * height * 4)
                throw new ArgumentException("rawBytes 长度应等于 width*height*4");

            pixelData = new Rgba32[width * height];
            int dstIdx = 0;

            for (int i = 0; i < rawBytes.Length; i += 4)
            {
                byte b = rawBytes[i + 0];
                byte g = rawBytes[i + 1];
                byte r = rawBytes[i + 2];

                // 貌似部分全黑会导致透明度也0，所以强制为255
                byte a = 255;
                pixelData[dstIdx++] = new Rgba32(r, g, b, a);
            }

            Bounds = new RectangleI(0, 0, width, height);
        }

        /// <summary>
        /// 转换后的 Rgba32 数据
        /// </summary>
        public ReadOnlySpan<Rgba32> Data => pixelData;

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
