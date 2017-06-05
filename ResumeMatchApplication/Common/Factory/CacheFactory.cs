using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace ResumeMatchApplication.Common
{
    public class CacheFactory<TData> where TData : class
    {
        private readonly string path = AppDomain.CurrentDomain.BaseDirectory.Remove(AppDomain.CurrentDomain.BaseDirectory.IndexOf("bin") - 1) + "\\Cache\\";

        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public DataResult SetCache(string fileName, TData data)
        {
            fileName = fileName + ".cache";

            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var filePath = path + fileName;

                if (!File.Exists(filePath))
                    File.Create(filePath).Close();

                var cache = JsonConvert.SerializeObject(data);

                File.WriteAllText(filePath, cache, Encoding.UTF8);
            }
            catch (UnauthorizedAccessException)
            {
                return new DataResult("权限异常！请以管理员身份运行该程序！");
            }

            return new DataResult();
        }

        /// <summary>
        /// 读取缓存
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public DataResult<TData> GetCache(string fileName)
        {
            fileName = fileName + ".cache";

            var filePath = path + fileName;

            if (!File.Exists(filePath))
            {
                return new DataResult<TData>("找不到缓存文件！");
            }

            var sr = new StreamReader(filePath, Encoding.UTF8);

            var cache = sr.ReadToEnd();

            sr.Close();

            return new DataResult<TData>(JsonConvert.DeserializeObject<TData>(cache));
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        /// <param name="cacheName">缓存文件名称</param>
        /// <param name="isVlurry">是否启用模糊删除</param>
        public void Clear(string cacheName, bool isVlurry = false)
        {
            string filePath;

            if (isVlurry)
            {
                var theFolder = new DirectoryInfo(path);

                var fileNames = theFolder.GetFiles().Where(w => w.Name.Contains(cacheName));

                foreach (var item in fileNames)
                {
                    filePath = path + item.Name;

                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
            }
            else
            {
                cacheName = cacheName + ".cache";

                filePath = path + cacheName;

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }
    }
}