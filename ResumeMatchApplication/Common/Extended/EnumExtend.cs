using System.ComponentModel;

namespace ResumeMatchApplication.Common
{
    public static class EnumExtend 
    {
        /// <summary>
        /// 获取枚举描述
        /// </summary>
        /// <param name="_enum"></param>
        /// <returns></returns>
        public static string Description<T>(this T _enum) where T : struct
        {
            try
            {
                var typeCode = GetEnumDescription(_enum);

                return typeCode;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 获取枚举类子项描述信息
        /// </summary>
        /// <param name="enumSubitem">枚举类子项</param>        
        private static string GetEnumDescription<T>(T enumSubitem) where T : struct
        {
            var strValue = enumSubitem.ToString();

            var t = enumSubitem.GetType();

            if (!t.IsEnum) return null;

            var fieldinfo = t.GetField(strValue);

            var objs = fieldinfo.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (objs.Length == 0)
            {
                return strValue;
            }

            var da = (DescriptionAttribute)objs[0];

            return da.Description;
        }

        /// <summary>
        /// 不区分大小写将一个或多个枚举常数的名称或数字值的字符串表示转换成等效的枚举对象。
        /// </summary>
        /// <param name="value"></param>
        /// <param name="en"></param>
        /// <returns>用于指示转换是否成功的返回值。</returns>
        public static bool TryParseCI<T>(string value, out T en) where T : struct
        {
            return EnumFactory<T>.TryParseCI(value, out en);
        }
    }
}