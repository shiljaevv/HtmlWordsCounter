using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace HtmlWordsCounter
{
    /// <summary>
    /// Класс, обеспечиваюший подсчет слов в тексте
    /// </summary>
    class WordsCounter
    {
        /// <summary>
        /// Массив сиволов-разделителей
        /// </summary>
        char[] separators;

        /// <summary>
        /// Структура для хранения количества слов
        /// </summary>
        private Dictionary<string, int> WordsFrequency { get; set; }

        /// <summary>
        /// Возвращает количество сохраненных слов
        /// </summary>
        public int Count
        {
            get
            {
                return WordsFrequency?.Count ?? 0;
            }
        }

        /// <summary>
        /// Инициализирует новый объект типа WordsCounter
        /// </summary>
        /// <param name="separators">Массив сиволов-разделителей</param>
        public WordsCounter(char[] separators)
        {
            this.separators = separators;
            WordsFrequency = new Dictionary<string, int>();
        }

        /// <summary>
        /// Очищает список сохраненных слов
        /// </summary>
        public void Reset()
        {
            if (WordsFrequency != null)
                WordsFrequency.Clear();
            else
                WordsFrequency = new Dictionary<string, int>();
        }

        /// <summary>
        /// Производит подсчет слов в тексте и сохраняет результат подсчета
        /// </summary>
        /// <param name="str">Строка для подсчета</param>
        public void AddWords(string str)
        {
            if (WordsFrequency == null) return;

            string[] words = str.ToLower().Split(separators, StringSplitOptions.RemoveEmptyEntries);

            foreach (string word in words)
            {
                if (WordsFrequency.ContainsKey(word))
                    WordsFrequency[word]++;
                else
                    WordsFrequency.Add(word, 1);
            }

        }
        
        /// <summary>
        /// Возвращает массив сохраненных слов
        /// </summary>
        /// <returns></returns>
        public string[] GetWords()
        {
            return WordsFrequency.Keys.ToArray();
        }

        /// <summary>
        /// Возвращает число использований указанного слова
        /// </summary>
        /// <param name="word">Слово для получения числа использований</param>
        /// <returns>Число использований</returns>
        public int GetCount(string word)
        {
            if (word == null) return 0;
            if (WordsFrequency.ContainsKey(word))
                return WordsFrequency[word];
            else
                return 0;
        }
    }
}
