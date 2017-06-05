using System;
using System.Security.Cryptography;
using System.Text;

namespace CheckResumeShooting.Common.Extended
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
            MD5CryptoServiceProvider x = new MD5CryptoServiceProvider();
            //byte[] data = Encoding.ASCII.GetBytes(value);
            byte[] data = Encoding.GetEncoding(charset).GetBytes(value);
            data = x.ComputeHash(data);
            string ret = "";
            for (int i = 0; i < data.Length; i++)
                ret += data[i].ToString("x2").ToLower();
            return ret;
        }

        /// <summary>
        /// 默认MD5加密
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string MD5(this string value)
        {
            MD5CryptoServiceProvider x = new MD5CryptoServiceProvider();
            byte[] data = Encoding.UTF8.GetBytes(value);
            //byte[] data = Encoding.GetEncoding(charset).GetBytes(value);
            data = x.ComputeHash(data);
            string ret = "";
            for (int i = 0; i < data.Length; i++)
                ret += data[i].ToString("x2").ToLower();
            return ret;
        }
		/// <summary>
		/// 默认MD5加密
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string MD5ToUpper(this string value) {
			MD5CryptoServiceProvider x = new MD5CryptoServiceProvider();
			byte[] data = Encoding.UTF8.GetBytes(value);
			data = x.ComputeHash(data);
			StringBuilder sb = new StringBuilder(32);
			foreach (var i in data) {
				sb.Append(i.ToString("x2").ToUpper());
			}
			return sb.ToString();
		}

		/// <summary>
		/// Base 64位加密
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string Base64(this string value)
        {
            byte[] encbuff = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(encbuff);
        }

        /// <summary>
        /// Base64位解密
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Base64Decode(this string value)
        {
            byte[] decbuff = Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(decbuff);
        }
    }
}
