// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using M.Resources.Fonts;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Overlays.Settings.Sections.Mf;

namespace osu.Game.Screens
{
    internal class CustomFontStore : NamespacedResourceStore<byte[]>
    {
        private readonly OsuGameBase gameBase;
        private readonly Storage customStorage;
        private readonly Dictionary<Assembly, Type> loadedAssemblies = new Dictionary<Assembly, Type>();

        public List<Font> ActiveFonts = new List<Font>();
        public static bool CustomFontLoaded;

        public CustomFontStore(Storage storage, OsuGameBase gameBase)
            : base(new StorageBackedResourceStore(storage), "custom")
        {
            this.gameBase = gameBase;

            customStorage = storage.GetStorageForDirectory("custom");

            ActiveFonts.AddRange(new[]
            {
                new ExperimentalSettings.FakeFont(),
                new ExperimentalSettings.FakeFont
                {
                    Name = "Noto fonts",
                    Author = "Google",
                    Homepage = "https://www.google.com/get/noto/",
                    FamilyName = "Noto-CJK-Compatibility",
                    LightAvaliable = false,
                    MediumAvaliable = false,
                    SemiBoldAvaliable = false,
                    BoldAvaliable = false,
                    BlackAvaliable = false
                }
            });

            prepareLoad();
        }

        public bool Contains(string path)
        {
            try
            {
                customStorage.Exists(path);
                return true;
            }
            catch (Exception e)
            {
                if (!(e is ArgumentException))
                    Logger.Error(e, "获取文件路径时发生了错误");

                return false;
            }
        }

        private void prepareLoad()
        {
            //获取custom下面所有以Font.dll、.Mvis.dll结尾的文件
            var fonts = customStorage.GetFiles(".", "*.Font.dll");

            try
            {
                foreach (var assembly in fonts)
                {
                    //获取完整路径
                    var fullPath = customStorage.GetFullPath(assembly);

                    //Logger.Log($"加载 {fullPath}");
                    loadAssembly(Assembly.LoadFrom(fullPath));
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"载入文件时出现问题: {e.Message}");
            }
        }

        /// <summary>
        /// 加载一个Assembly
        /// </summary>
        /// <param name="assembly">要加载的Assembly</param>
        private void loadAssembly(Assembly assembly)
        {
            if (loadedAssemblies.Any(a => a.Key.FullName == assembly.FullName))
                return;

            var name = Path.GetFileNameWithoutExtension(assembly.Location);
            if (loadedAssemblies.Values.Any(t => Path.GetFileNameWithoutExtension(t.Assembly.Location) == name))
                return;

            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    //Logger.Log($"case: 尝试加载 {type}");

                    if (type.IsSubclassOf(typeof(Font)))
                    {
                        loadedAssemblies[assembly] = type;
                        loadedAssemblies[assembly] = type;
                        addFont(type, assembly.FullName);
                    }

                    //Logger.Log($"{type}不是任何一个SubClass");
                }

                //添加store
                gameBase.Resources.AddStore(new DllResourceStore(assembly));
            }
            catch (Exception e)
            {
                Logger.Error(e, $"载入 {assembly.FullName} 时出现了问题, 请联系你的插件提供方。");
            }
        }

        /// <summary>
        /// 向gameBase添加一个字体
        /// </summary>
        /// <param name="fontType">要添加的字体</param>
        /// <param name="fullName">与fontType对应的Assembly的fullName</param>
        private void addFont(Type fontType, string? fullName)
        {
            if (fullName == null)
            {
                Logger.Log("字体" + fontType + "的FullName为null，将不会加载");
                return;
            }

            try
            {
                var currentFontInfo = (Font)Activator.CreateInstance(fontType)!;

                if (ActiveFonts.Any(f => f.FamilyName == currentFontInfo.FamilyName))
                {
                    //Logger.Log($"将跳过 {fullName}, 因为已经存在家族名为 {currentFontInfo.FamilyName} 的字体被加载", level: LogLevel.Important);
                    return;
                }

                //加载字体
                gameBase.AddFont(gameBase.Resources, $"Fonts/{currentFontInfo.FamilyName}-Regular");

                if (currentFontInfo.LightAvaliable)
                    gameBase.AddFont(gameBase.Resources, $"Fonts/{currentFontInfo.FamilyName}-Light");

                if (currentFontInfo.MediumAvaliable)
                    gameBase.AddFont(gameBase.Resources, $"Fonts/{currentFontInfo.FamilyName}-Medium");

                if (currentFontInfo.SemiBoldAvaliable)
                    gameBase.AddFont(gameBase.Resources, $"Fonts/{currentFontInfo.FamilyName}-SemiBold");

                if (currentFontInfo.BoldAvaliable)
                    gameBase.AddFont(gameBase.Resources, $"Fonts/{currentFontInfo.FamilyName}-Bold");

                if (currentFontInfo.BlackAvaliable)
                    gameBase.AddFont(gameBase.Resources, $"Fonts/{currentFontInfo.FamilyName}-Black");

                ActiveFonts.Add(currentFontInfo);

                //设置CustomFontLoaded
                CustomFontLoaded = true;
            }
            catch (Exception e)
            {
                Logger.Error(e, $"尝试添加字体{fullName}时出现了问题");
            }
        }
    }
}
