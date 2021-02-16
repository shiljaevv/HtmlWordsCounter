using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlWordsCounter
{
    /// <summary>
    /// Класс, обеспечивающий вывод значений WordsCounter
    /// </summary>
    class CounterWriter
    {
        /// <summary>
        /// Объект WordsCounter
        /// </summary>
        protected WordsCounter counter;
        /// <summary>
        /// Дополнительный обработчик CounterWriter
        /// </summary>
        protected CounterWriter wrappee;

        /// <summary>
        /// Инициализирует объект CounterWriter 
        /// </summary>
        public CounterWriter()
        {
            this.wrappee = null;
            this.counter = null;
        }

        /// <summary>
        /// Инициализирует объект CounterWriter 
        /// </summary>
        /// <param name="wrappee">Дополнительный обработчик CounterWriter</param>
        public CounterWriter(CounterWriter wrappee)
        {
            this.wrappee = wrappee;
            this.counter = wrappee?.counter;
        }

        /// <summary>
        /// Инициализирует объект CounterWriter 
        /// </summary>
        /// <param name="counter">Объект WordsCounter</param>
        public CounterWriter(WordsCounter counter)
        {
            this.wrappee = null;
            this.counter = counter;
        }

        /// <summary>
        /// Производит вывод значений объекта WordsCounter
        /// </summary>
        public virtual void Write()
        {
            wrappee?.Write();
        }
    }
}
