using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Threading;

namespace HtmlWordsCounter
{
    class Program
    {
        static void Main(string[] args)
        {  
            Logger.Configure("log.txt"); 

            // Выделение переданных параметров
            string pagePath = string.Empty;
            string dbConnectStr = string.Empty;
            string outFile = string.Empty;
            string encodingStr = string.Empty;

            if (args.Length < 1)
            {
                Logger.Log("Передано недостаточно параметров", LogMessageLevel.Error);
                Console.WriteLine("\r\nНажмите любую клавишу для завершения...");
                Console.ReadKey();
                return;
            }

            pagePath = args[0];

            if (args.Length > 0) pagePath = args[0];

            for (int narg = 1; narg < args.Length; narg++)
            {
                // Параметры соединения с БД
                if ((args[narg] == "--dbconnect") && ((narg + 1) < args.Length))    
                    dbConnectStr = args[narg + 1];
                // Файл с результатами
                if ((args[narg] == "--out") && ((narg + 1) < args.Length))          
                    outFile = args[narg + 1];
                // Кодировка
                if ((args[narg] == "--encoding") && ((narg + 1) < args.Length))
                    encodingStr = args[narg + 1];

            }

            // Получение кодировки на основе переданного значения
            Encoding encoding = null;
            try
            {                
                if (!string.IsNullOrEmpty(encodingStr))
                    encoding = Encoding.GetEncoding(encodingStr);
            }
            catch (Exception e)
            {
                Logger.LogError(string.Format("Ошибка при определении кодировки {0}", encodingStr), e.ToString());
                return;
            }


            // Создание объекта для подсчета слов
            char[] separators = new char[] { ' ', '\t', '\r', '\n', '.', ',', '-', '(', ')',
                '<', '>', '{', '}', '[', ']', '\'', '\"', ':', ';', '!', '?'};
            WordsCounter counter = new WordsCounter(separators);

            // Создание структуры для html-страницы
            HtmlDocument doc = new HtmlDocument();

            // Определение действий при изменении прогресса загрузки и обработки
            doc.DownloadProgressChange += Root_DownloadProgressChange;
            doc.DownloadCompleted += (sender) => { Console.WriteLine(); };
            doc.LoadHtmlProgressChange += Root_LoadHtmlProgressChange;
            doc.LoadHtmlCompleted += (sender) => { Console.WriteLine(); };

            Logger.Log("Начало загрузки страницы " + pagePath);

            // Загрузка страницы
            bool ok = doc.LoadTextBlocks(pagePath, counter.AddWords, encoding);

            if (ok)
            {
                // Вывод результатов подсчета
                CounterWriter writer = new CounterConsoleWriter(counter);

                // Выбор записи результатов подсчета в БД
                if (!string.IsNullOrEmpty(dbConnectStr))
                    writer = new CounterDbWriter(writer, dbConnectStr);

                // Выбор записи результатов подсчета в файл
                if (!string.IsNullOrEmpty(outFile))
                    writer = new CounterFileWriter(writer, outFile);

                writer.Write();

            }
            Console.WriteLine();
            Logger.Log("Обработка завершена");

            Console.WriteLine("\r\nНажмите любую клавишу для завершения...");
            Console.ReadKey();
        }

        private static void Root_LoadHtmlProgressChange(object sender, int progress)
        {
            Console.Write("\rLoading Html page... {0}%", progress);
        }

        private static void Root_DownloadProgressChange(object sender, int progress)
        {
            Console.Write("\rDownloading... {0}%", progress);
        }
    }
}