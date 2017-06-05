using System;
using System.Collections.Generic;
using System.Reflection;

namespace ResumeMatchApplication.Common
{
	public class EnumFactory<T> where T : struct
    {
		/// <summary>
		/// 将字符串转换为枚举
		/// </summary>
		/// <param name="value">枚举项的名称</param>
		/// <returns></returns>
		public static T Parse(string value) {

			if (dic == null)
            {
				foreach (var i in arr)
                {
					if (i.Key == value)
                    {
						return i.Value;
					}
				}
			}
            else
            {
				T val;
				if (dic.TryGetValue(value, out val))
                {
					return val;
				}
			}

			throw new ArgumentException("value 是一个名称，但不是为该枚举定义的命名常量之一。");
		}

        /// <summary>
        /// 将一个枚举转换为另一个枚举
        /// </summary>
        /// <param name="em">要转换的枚举</param>
        /// <returns></returns>
        public static T Parse<TIn>(TIn em) where TIn : struct
        {
			return Parse(em.ToString());
		}

		/// <summary>
		/// 不区分大小写将字符串转换为枚举
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static T ParseCI(string value) {
			value = value.ToUpper();

			foreach(var i in arrCI)
            {
				if(i.Key == value)
                {
					return i.Value;
				}
			}

			throw new ArgumentException("value 是一个名称，但不是为该枚举定义的命名常量之一。");
		}

		/// <summary>
		/// 不区分大小写将一个或多个枚举常数的名称或数字值的字符串表示转换成等效的枚举对象。
		/// </summary>
		/// <param name="value"></param>
		/// <param name="en"></param>
		/// <returns>用于指示转换是否成功的返回值。</returns>
		public static bool TryParseCI(string value, out T en)
        {

			value = value.ToUpper();

			foreach (var i in arrCI)
            {
				if (i.Key == value)
                {
					en = i.Value;
					return true;
				}
			}
			en = default(T);

			return false;
		}
		/// <summary>
		/// 将字符串转换为枚举
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static T? ParseOrNull(string value)
        {
			if (value == null)
            {
				return null;
			}

			return Parse(value);
		}
		/// <summary>
		/// 不区分大小写将字符串转换为枚举
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static T? ParseCIOrNull(string value)
        {
			if (value == null)
            {
				return null;
			}
			return ParseCI(value);
		}

		static EnumFactory() {
			var type = typeof(T);
			if (!type.IsEnum)
            {
				throw new ArgumentException("T 不是 System.Enum");
			}
			var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);

			List<KeyValuePair<string, T>> list = null;

			if (fields.Length > 8)
            {
				dic = new Dictionary<string, T>(fields.Length);
			} else
            {
				list = new List<KeyValuePair<string, T>>(fields.Length);
			}
			foreach (var i in fields)
            {
				if (dic == null)
				{
				    list?.Add(new KeyValuePair<string, T>(i.Name, (T)i.GetValue(null)));
				} else
                {
					dic.Add(i.Name, (T)i.GetValue(null));
				}
			}
			if (list != null)
            {
				arr = list.ToArray();

				arrCI = new KeyValuePair<string, T>[arr.Length];

				for (var i = 0; i < arr.Length; ++i)
                {
					arrCI[i] = new KeyValuePair<string, T>(arr[i].Key.ToUpper(), arr[i].Value);
				}
			}
		}

		private static readonly KeyValuePair<string, T>[] arr;

		private static readonly Dictionary<string, T> dic;

		private static readonly KeyValuePair<string, T>[] arrCI;
	}
}
