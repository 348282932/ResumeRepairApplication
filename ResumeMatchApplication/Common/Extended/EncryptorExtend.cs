using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ResumeMatchApplication.Common
{
    public static class EncryptorExtend
    {
        /// <summary>
        /// 指定字符编码MD5加密
        /// </summary>
        /// <param name="value"></param>
        /// <param name="charset"></param>
        /// <returns></returns>
        public static string MD5(this string value, string charset)
        {
            var x = new MD5CryptoServiceProvider();

            var data = Encoding.GetEncoding(charset).GetBytes(value);

            data = x.ComputeHash(data);

            return data.Aggregate("", (current, t) => current + t.ToString("x2").ToLower());
        }

        /// <summary>
        /// 默认MD5加密
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string MD5(this string value)
        {
            var x = new MD5CryptoServiceProvider();

            var data = Encoding.UTF8.GetBytes(value);

            data = x.ComputeHash(data);

            return data.Aggregate("", (current, t) => current + t.ToString("x2").ToLower());
        }

		/// <summary>
		/// 默认MD5加密
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string MD5ToUpper(this string value)
        {
			var x = new MD5CryptoServiceProvider();

			var data = Encoding.UTF8.GetBytes(value);

			data = x.ComputeHash(data);

		    return data.Aggregate("", (current, t) => current + t.ToString("x2").ToUpper());
        }

		/// <summary>
		/// Base 64位加密
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string Base64(this string value)
        {
            var encbuff = Encoding.UTF8.GetBytes(value);

            return Convert.ToBase64String(encbuff);
        }

        /// <summary>
        /// Base64位解密
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Base64Decode(this string value)
        {
            var decbuff = Convert.FromBase64String(value);

            return Encoding.UTF8.GetString(decbuff);
        }
    }
}
