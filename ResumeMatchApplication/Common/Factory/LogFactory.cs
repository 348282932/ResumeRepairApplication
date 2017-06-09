using System;
using System.IO;
using log4net;
using ResumeMatchApplication.Models;

namespace ResumeMatchApplication.Common
{
    public static class LogFactory
    {
        private static readonly string path = AppDomain.CurrentDomain.BaseDirectory.Remove(AppDomain.CurrentDomain.BaseDirectory.IndexOf("bin") - 1) + "\\Log\\";

        private static ILog _logger;

        public static void Info(string content, MessageSubjectEnum messageSubject = MessageSubjectEnum.System)
        {
            _logger = LogManager.GetLogger("Info");

            content = $"【{messageSubject.Description()}】{content}";

            _logger.Info(content);

            Console.WriteLine(content);
        }

        public static void Error(string content, MessageSubjectEnum messageSubject = MessageSubjectEnum.System)
        {
            _logger = LogManager.GetLogger("Error");

            content = $"【{messageSubject.Description()}】{content}";

            _logger.Error(content);

            Console.WriteLine(content);
        }

        public static void Warn(string content, MessageSubjectEnum messageSubject = MessageSubjectEnum.System)
        {
            _logger = LogManager.GetLogger("Warn");

            content = $"【{messageSubject.Description()}】{content}";

            _logger.Warn(content);

            Console.WriteLine(content);
        }

        public static void Debug(string content, MessageSubjectEnum messageSubject = MessageSubjectEnum.System)
        {
            _logger = LogManager.GetLogger("Debug");

            content = $"【{messageSubject.Description()}】{content}";

            _logger.Debug(content);

            Console.WriteLine(content);
        }

        public static void SetErrorLog(string content)
        {
            var fileName = $"{path}\\Error\\";

            try
            {
                if (!Directory.Exists(fileName))
                    Directory.CreateDirectory(fileName);

                var filePath = $"{fileName}{DateTime.Now:yyyy-MM-dd}.txt";

                if (!File.Exists(filePath))
                    File.Create(filePath).Close();

                var writer = File.AppendText(filePath);

                writer.WriteLineAsync(DateTime.Now.ToString("HH:mm:ss") + Environment.NewLine + content);

                writer.Close();

                writer.Dispose();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public static void SetInfoLog(string content)
        {
            var fileName = $"{path}\\Info\\";

            try
            {
                if (!Directory.Exists(fileName))
                    Directory.CreateDirectory(fileName);

                var filePath = $"{fileName}{DateTime.Now:yyyy-MM-dd}.txt";

                if (!File.Exists(filePath))
                    File.Create(filePath).Close();

                var writer = File.AppendText(filePath);

                writer.WriteLineAsync(DateTime.Now.ToString("HH:mm:ss") + Environment.NewLine + content);

                writer.Close();

                writer.Dispose();
            }
            catch
            {
                // ignored
            }
        }
    }
}
