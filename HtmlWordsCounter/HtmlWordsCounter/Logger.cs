using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HtmlWordsCounter
{
    /// <summary>
    /// Статический клас обеспечивающий ведение журнала работы программы
    /// </summary>
    static class Logger
    {
        /// <summary>
        /// Имя фала журнала
        /// </summary>
        private static string logFilePath;
        /// <summary>
        /// Объект используемый в lock
        /// </summary>
        private static object syncObj;


        /// <summary>
        /// Выполняет настройку класса
        /// </summary>
        /// <param name="logFilePath">Имя фала журнала</param>
        public static void Configure(string logFilePath)
        {
            Logger.logFilePath = logFilePath;
            syncObj = new object();
        }

        /// <summary>
        /// Производит запись в журнал сообщения об ошибке
        /// </summary>
        /// <param name="msg">Сообщение</param>
        /// <param name="details">Подробное сообщение</param>
        public static void LogError(string msg, string details)
        {
            Log(msg, details, LogMessageLevel.Error);
        }

        /// <summary>
        /// Производит запись в журнал сообщения
        /// </summary>
        /// <param name="msg">Сообщение</param>
        /// <param name="level">Уровень сообщения</param>
        public static void Log(string msg, LogMessageLevel level = LogMessageLevel.Info)
        {
            Log(msg, string.Empty, level);
        }

        /// <summary>
        /// Производит запись в журнал сообщения
        /// </summary>
        /// <param name="msg">Сообщение</param>
        /// <param name="details">Подробное сообщение</param>
        /// <param name="level">Уровень сообщения</param>
        public static void Log(string msg, string details, LogMessageLevel level = LogMessageLevel.Info)
        {
            if (string.IsNullOrEmpty(logFilePath))
                return;

            string record = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ");
            switch(level)
            {
                case LogMessageLevel.Warning: record += "(!) "; break;
                case LogMessageLevel.Error: record += "(X) "; break;
            }

            record += msg + (!string.IsNullOrEmpty(details) ? ": " + details : "") + "\r\n";

            lock (syncObj)
            {
                File.AppendAllText(logFilePath, record);
            }

            if (level >= LogMessageLevel.Info)
                Console.WriteLine(msg);
        }
    }
}
