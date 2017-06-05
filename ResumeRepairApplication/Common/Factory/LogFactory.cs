using System;
using System.IO;

namespace ResumeRepairApplication.Common.Factory
{
    public class LogFactory
    {
        private static readonly string path = AppDomain.CurrentDomain.BaseDirectory.Remove(AppDomain.CurrentDomain.BaseDirectory.IndexOf("bin") - 1) + "\\Log\\";

        public static void SetErrorLog(string content)
        {
            var fileName = $"{path}\\Error\\";

            try
            {
                if (!Directory.Exists(fileName))
                    Directory.CreateDirectory(fileName);

                string filePath = $"{fileName}{DateTime.Now.ToString("yyyy-MM-dd")}.txt";

                if (!File.Exists(filePath))
                    File.Create(filePath).Close();

                var writer = File.AppendText(filePath);

                writer.WriteLineAsync(DateTime.Now.ToString("HH:mm:ss") + Environment.NewLine + content);

                writer.Close();

                writer.Dispose();
            }
            catch (Exception)
            {

            }
        }

        public static void SetInfoLog(string content)
        {
            var fileName = $"{path}\\Info\\";

            try
            {
                if (!Directory.Exists(fileName))
                    Directory.CreateDirectory(fileName);

                string filePath = $"{fileName}{DateTime.Now.ToString("yyyy-MM-dd")}.txt";

                if (!File.Exists(filePath))
                    File.Create(filePath).Close();

                var writer = File.AppendText(filePath);

                writer.WriteLineAsync(DateTime.Now.ToString("HH:mm:ss") + Environment.NewLine + content);

                writer.Close();

                writer.Dispose();
            }
            catch{}
        }
    }
}
