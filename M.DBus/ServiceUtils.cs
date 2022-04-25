using System;
using System.Collections.Generic;
using System.Reflection;
using osu.Framework.Logging;

namespace M.DBus
{
    public class ServiceUtils
    {
        private static readonly Dictionary<string, string> not_available = new Dictionary<string, string>
        {
            ["0"] = "Oops",
            ["1"] = "此属性对外不可用或获取时发生异常",
            ["2"] = "请检查runtime.log以获取更多信息",
        };

        public static Dictionary<string, object> GetMembers(object target)
        {
            var dictionary = new Dictionary<string, object>();

            foreach (var memberInfo in target.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public |
                                                                   BindingFlags.GetProperty | BindingFlags.SetProperty))
                dictionary[memberInfo.Name] = memberInfo;

            return dictionary;
        }

        public static object GetValueFor(object source, string name, IDictionary<string, object> members)
        {
            if (name == "members")
            {
                Logger.Log("members属性对外不可用。");
                return not_available;
            }

            try
            {
                object value = null;

                var targetMember = (MemberInfo)members[name];

                if (targetMember != null && targetMember is PropertyInfo propertyInfo)
                    value = propertyInfo.GetValue(source);

                return value;
            }
            catch (Exception e)
            {
                Logger.Log($"未能在{source}中查找 {name}: {e.Message}");
                Logger.Log(e.StackTrace);
            }

            return not_available;
        }

        public static bool SetValueFor(object source, string name, object newValue, IDictionary<string, object> members)
        {
            try
            {
                CheckIfDirectoryNotReady(source, members, out members);
                var targetMember = (MemberInfo)members[name];

                if (targetMember != null && targetMember is PropertyInfo propertyInfo)
                {
                    //如果旧值等于新值
                    if (propertyInfo.GetValue(source) == newValue)
                        return false;

                    propertyInfo.SetValue(source, newValue);
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e, $"未能在{source}中设置{name}: {e.Message}");
                return false;
            }
        }

        public static bool CheckifContained(object source, string name, IDictionary<string, object> members)
        {
            CheckIfDirectoryNotReady(source, members, out members);
            return members.ContainsKey(name);
        }

        public static void CheckIfDirectoryNotReady(object source,
                                                    IDictionary<string, object> sourceDict,
                                                    out IDictionary<string, object> reCallDict)
        {
            if (sourceDict == null || sourceDict.Count == 0)
                sourceDict = GetMembers(source);

            reCallDict = sourceDict;
        }
    }
}
