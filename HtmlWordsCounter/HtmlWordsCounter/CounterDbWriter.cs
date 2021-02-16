using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlWordsCounter
{
    /// <summary>
    /// Класс, обеспечивающий вывод значений WordsCounter в БД
    /// </summary>
    class CounterDbWriter : CounterWriter
    {
        /// <summary>
        /// Строка с параметрами подключения к БД
        /// </summary>
        string dbConnectionString;

        /// <summary>
        /// Инициализирует объект CounterWriter 
        /// </summary>
        /// <param name="counter">Объект WordsCounter</param>
        /// <param name="dbConnectionString">Строка с параметрами подключения к БД</param>
        public CounterDbWriter(WordsCounter counter, string dbConnectionString) : base(counter)
        {
            this.dbConnectionString = dbConnectionString;
        }
        /// <summary>
        /// Инициализирует объект CounterWriter 
        /// </summary>
        /// <param name="wrappee">Дополнительный обработчик CounterWriter</param>
        /// <param name="dbConnectionString">Строка с параметрами подключения к БД</param>
        public CounterDbWriter(CounterWriter wrappee, string dbConnectionString) : base(wrappee) 
        {
            this.dbConnectionString = dbConnectionString;
        }

        /// <summary>
        /// Производит запись в БД значений объекта WordsCounter
        /// </summary>
        public override void Write()
        {
            base.Write();

            if ((counter == null) || (counter.Count == 0)) 
                return;

            Console.WriteLine();
            Logger.Log("Запись в БД...");

            // Получение поствщика данных
            string factoryName = "System.Data.SqlClient";
            DbProviderFactory dbFactory = null;

            try
            {
                dbFactory = DbProviderFactories.GetFactory(factoryName);
            }
            catch(Exception e)
            {
                Logger.LogError(string.Format("Ошибка при получении объекта поставщика данных {0}", factoryName), e.ToString());
                return;
            }

            if (dbFactory == null) return;

            try
            {
                using (DbConnection connection = dbFactory.CreateConnection())
                {
                    if (connection == null) return;

                    // Открытие соединения
                    connection.ConnectionString = dbConnectionString;
                    connection.Open();


                    // Формирование строки с sql-запросом
                    DbCommand command = dbFactory.CreateCommand();

                    if (command == null) return;
                    command.Connection = connection;

                    string commandStr = string.Empty;
                    int maxCountValues = 1000;

                    string[] words = counter.GetWords();
                    for (int num = 0; num < words.Length; num++)
                    {
                        if (num % maxCountValues == 0)
                            commandStr = "Insert into WordsStatistic Values ";

                        commandStr += string.Format("('{0}', {1}), ", words[num], counter.GetCount(words[num]));

                        if (((num + 1) % maxCountValues == 0) || (num == words.Length - 1))
                        { 
                            // Выполнение запроса
                            commandStr = commandStr.Substring(0, commandStr.Length - 2);                            
                            command.CommandText = commandStr;
                            command.ExecuteNonQuery();
                        }
                    }

                    Logger.Log("Запись в БД завершена");
                }
            }
            catch(Exception e)
            {
                Logger.LogError("Ошибка при записи в БД", e.ToString());
            }

        }
    }
}
