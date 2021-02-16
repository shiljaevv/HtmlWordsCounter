using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlWordsCounter
{
    /// <summary>
    /// Класс, обеспечивающий вывод в консоль значений WordsCounter
    /// </summary>
    class CounterConsoleWriter : CounterWriter
    {
        /// <summary>
        /// Инициализирует объект CounterConsoleWriter 
        /// </summary>
        /// <param name="counter">Объект WordsCounter</param>
        public CounterConsoleWriter(WordsCounter counter) : base(counter) { }

        /// <summary>
        /// Инициализирует объект CounterConsoleWriter 
        /// </summary>
        /// <param name="wrappee">Дополнительный обработчик CounterWriter</param>
        public CounterConsoleWriter(CounterWriter wrappee) : base(wrappee) { }

        /// <summary>
        /// Производит вывод в консоль значений объекта WordsCounter
        /// </summary>
        public override void Write()
        {
            base.Write();

            if ((counter == null) || (counter.Count == 0))
                return;

            foreach (string word in counter.GetWords())
                Console.WriteLine("{0} - {1}", word, counter.GetCount(word));
            Console.WriteLine();
        }

    }
}
