using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlWordsCounter
{
    /// <summary>
    /// Класс, обеспечивающий запись в файл значений WordsCounter 
    /// </summary>
    class CounterFileWriter : CounterWriter
    {
        /// <summary>
        /// Имя файла для вывода значений WordsCounter
        /// </summary>
        string fileName;

        /// <summary>
        /// Инициализирует объект CounterFileWriter 
        /// </summary>
        /// <param name="counter">Объект WordsCounter</param>
        /// <param name="fileName">Имя файла для вывода значений WordsCounter</param>
        public CounterFileWriter(WordsCounter counter, string fileName) : base(counter)
        {
            this.fileName = fileName;
        }
        /// <summary>
        /// Инициализирует объект CounterFileWriter 
        /// </summary>
        /// <param name="wrappee">Дополнительный обработчик CounterWriter</param>
        /// <param name="fileName">Имя файла для вывода значений WordsCounter</param>
        public CounterFileWriter(CounterWriter wrappee, string fileName) : base(wrappee)
        {
            this.fileName = fileName;
        }

        /// <summary>
        /// Производит запись в файл значений объекта WordsCounter
        /// </summary>
        public override void Write()
        {
            base.Write();

            if ((counter == null) || (counter.Count == 0))
                return;

            if (string.IsNullOrEmpty(fileName))
                return;

            try
            {
                string wordsCountStr = string.Empty;
                foreach(string word in counter.GetWords())
                    wordsCountStr += string.Format("{0}\t{1}\r\n", word, counter.GetCount(word));

                File.AppendAllText(fileName, wordsCountStr);
            }
            catch (Exception e)
            {
                Logger.LogError(string.Format("Ошибка при выводе значений в файл '{0}'", fileName), e.ToString());
            }
        }

    }
}
