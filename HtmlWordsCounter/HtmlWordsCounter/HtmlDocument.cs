using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HtmlWordsCounter
{
    public delegate void ProgressChangedEventHandler(object sender, int progress);
    public delegate void CompletedEventHandler(object sender);

    /// <summary>
    /// Класс, описывающий html-документ
    /// </summary>
    class HtmlDocument : HtmlNode
    {
        /// <summary>
        /// Кодировка файла с html-документом
        /// </summary>
        public Encoding encoding { get; set; }

        /// <summary>
        /// Происходит при изменении прогресса загрузки и сохранения html-страницы
        /// </summary>
        public event ProgressChangedEventHandler DownloadProgressChange;
        /// <summary>
        /// Происходит при завершении процесса загрузки html-страницы
        /// </summary>
        public event CompletedEventHandler DownloadCompleted;

        /// <summary>
        /// Происходит при изменении процесса обработки html-документа
        /// </summary>
        public event ProgressChangedEventHandler LoadHtmlProgressChange;
        /// <summary>
        /// Происходит при завершении процесса обработки html-документа
        /// </summary>
        public event CompletedEventHandler LoadHtmlCompleted;

        /// <summary>
        /// Производит загрузку, сохранение html-документа и предоставляет возможность
        /// последовательной обработки текстовых блоков документа
        /// </summary>
        /// <param name="path">Путь к html-странице. При указании адреса "http://", 
        /// "https://" загружает и сохраняет страницу</param>
        /// <param name="textProcess">Обработчик текстовых болоков документа</param>
        /// <param name="encoding">Кодировка файла (для локальных документов)</param>
        /// <returns></returns>
        public bool LoadTextBlocks(string path, Action<string> textProcess, Encoding encoding = null)
        {
            // Файл для сохранения скачанной страницы
            string pageFileName = "page.html";

            // Скачивание html-документа и определение кодировки файла
            if ((path.IndexOf("http://") == 0) || (path.IndexOf("https://") == 0))
            {
                var downloadingRes = DownloadHtml(path, pageFileName,
                                        (progress) => { DownloadProgressChange?.Invoke(this, progress); },
                                        () => { DownloadCompleted?.Invoke(this); });

                if (!downloadingRes.Item1) return false;
                encoding = downloadingRes.Item2;
            }
            else
            {
                pageFileName = path;
            }

            if (encoding == null) encoding = Encoding.Default;
            this.encoding = encoding;


            // Чтение html-файла и заполнение структуры
            try
            {
                if (!File.Exists(pageFileName))
                {
                    Logger.Log(string.Format("Файл {0} не существует", pageFileName));
                    return false;
                }

                using (BinaryReader reader = new BinaryReader(new FileStream(pageFileName, FileMode.Open), encoding))
                {
                    long beginPosition;     // Позиция в файле начала тега
                    long endPosition;       // Позиция в файле конца тега
                    string content = string.Empty;  // Содержимое тега
                    bool isOpenTag;         // Открывающийся тег
                    HtmlNodeType nodeType;  // Тип тега

                    HtmlNode node = this;   // Текщий элемент
                    bool isInBodyTag = false;   //  Текущий элемент внутри тега <body> 
                    int loadProgress = -1;  // Прогресс загрузки

                    while (reader.PeekChar() >= 0)
                    {
                        // Проверка изменения значения прогресса обработки и вызов обработчиков
                        if ((LoadHtmlProgressChange != null) && ((reader.BaseStream.Position * 100 / reader.BaseStream.Length) != loadProgress))
                        {
                            loadProgress = (int)(reader.BaseStream.Position * 100 / reader.BaseStream.Length);
                            LoadHtmlProgressChange.Invoke(this, loadProgress);
                        }

                        bool notEmpty;

                        // Поиск в файле ближайшего тега, тектового блока и т.п
                        if (node.hasHtmlContant())
                            notEmpty = FindNextNode(reader, out nodeType, out beginPosition, out endPosition, out content, out isOpenTag);
                        else
                            notEmpty = FindNextNode(reader, out nodeType, out beginPosition, out endPosition, out content, out isOpenTag, node.tagName);

                        if (notEmpty)
                        {
                            // Если найден одиночный тег
                            if ((nodeType == HtmlNodeType.SingleTag) || (isOnlySingleTag(content)))
                            {
                                node.AppendChild(new HtmlNode(HtmlNodeType.SingleTag, content, beginPosition, endPosition));
                                continue;
                            }

                            // Если найден не одиночный тег
                            if (nodeType == HtmlNodeType.ContainerTag)
                            {
                                // Если найден открывающийся тег
                                if (isOpenTag)
                                {
                                    // Добавление нового элемента 
                                    HtmlNode newNode = new HtmlNode(HtmlNodeType.ContainerTag, content, beginPosition, endPosition);
                                    if (node.AppendChild(newNode)) node = newNode;

                                    if (content == "body")
                                        isInBodyTag = true;
                                }
                                else
                                {
                                    // Закрытие тега
                                    HtmlNode closedNode = node.CloseTag(content);
                                    if (closedNode != null)
                                    {
                                        closedNode.endPosition = endPosition;
                                        node = closedNode.ParentNode;
                                    }

                                    if (content == "body")
                                        isInBodyTag = false;

                                }
                                continue;
                            }

                            // Если найден тектовый блок
                            if (nodeType == HtmlNodeType.TextBlock)
                            {
                                // Добавление нового тектового блока
                                node.AppendChild(new HtmlNode(HtmlNodeType.TextBlock, beginPosition, endPosition));
                                
                                // Вызов обработчика текста
                                if ((textProcess != null) && isInBodyTag && node.hasHtmlContant())
                                    textProcess.Invoke(content);
                                continue;
                            }
                        }
                    }

                    // Вызов обработчиков окончания обработки
                    LoadHtmlProgressChange?.Invoke(this, (int)(100));
                    LoadHtmlCompleted?.Invoke(this);
                }
            }
            catch(Exception e)
            {
                Logger.LogError(string.Format("Ошибка при обработке файла \'{0}\'", pageFileName), e.ToString());
                return false;
            }

            return true;
         }

        /// <summary>
        /// Загружает html-страницу
        /// </summary>
        /// <param name="uri">uri-адрес html-страницы</param>
        /// <param name="fileName">имя файла для сохранения html-страницы</param>
        /// <param name="ProgressChange">Действие при изменении процесса загрузки</param>
        /// <param name="DownloadComleted">Действие при завершении загрузки</param>
        /// <returns>Возвращает true если загрузка прошла успешно и кодировку загруженной страницы</returns>
        public static Tuple<bool, Encoding> DownloadHtml(string uri, string fileName, Action<int> ProgressChange = null, Action DownloadComleted = null)
        {
            try
            {
                // Проверка входных параметров
                if (string.IsNullOrEmpty(uri) || string.IsNullOrEmpty(fileName))
                    return new Tuple<bool, Encoding>(false, null);

                // Создание объекта WebClient для скачивания документа
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                WebClient webClient = new WebClient();

                // Установка обработчиков событий
                if (ProgressChange != null)
                    webClient.DownloadProgressChanged += (sender, e) => {
                        ProgressChange.Invoke(e.ProgressPercentage);
                    };

                if (DownloadComleted != null)
                    webClient.DownloadFileCompleted += (sender, e) => {
                        DownloadComleted.Invoke();
                    };

                // Загрузка и сохранение страницы
                var downloadTask = webClient.DownloadFileTaskAsync(new Uri(uri), fileName);
                downloadTask.Wait();

                // Определение кодировки загруженной страницы
                string contentType = webClient.ResponseHeaders.Get("Content-Type");

                if ((contentType != null) && (contentType.Contains("charset=")))
                {
                    string encodingName = contentType.Substring(contentType.IndexOf("charset=") + ("charset=").Length);
                    Encoding encoding = Encoding.GetEncoding(encodingName);
                    return new Tuple<bool, Encoding>(true, encoding);
                }
                else
                {
                    return new Tuple<bool, Encoding>(true, Encoding.UTF8);
                }
                                
            }
            catch(Exception e)
            {
                Logger.LogError(string.Format("Ошибка при скачивании страницы \'{0}\'", uri), e.ToString());
                return new Tuple<bool, Encoding>(false, null);
            }
            
        }

        /// <summary>
        /// Производит поиск в файле начала следующего узла (тега, текстового блока, коментария и т.д)
        /// </summary>
        /// <param name="reader">Объект для чтения обрабатываемого файла</param>
        /// <param name="nodeType">Возвращает тип найденного узла</param>
        /// <param name="beginPosition">Возвращает позицию в файле начала найденного узла</param>
        /// <param name="endPosition">Возвращает позицию в файле конца найденного узла</param>
        /// <param name="content">Возвращает имя найденного тега или содержимое текстового блока</param>
        /// <param name="isOpenTag">Возвращяет true если найден открывающийся тег</param>
        /// <returns>Возвращает true при успешном поиске</returns>
        private static bool FindNextNode(BinaryReader reader,
            out HtmlNodeType nodeType,
            out long beginPosition,
            out long endPosition,
            out string content,
            out bool isOpenTag)
        {
            return FindNextNode(reader, out nodeType, out beginPosition, out endPosition, out content, out isOpenTag, string.Empty);
        }

        /// <summary>
        /// Производит поиск в файле начала следующего узла (тега, текстового блока, коментария и т.д)
        /// </summary>
        /// <param name="reader">Объект для чтения обрабатываемого файла</param>
        /// <param name="nodeType">Возвращает тип найденного узла</param>
        /// <param name="beginPosition">Возвращает позицию в файле начала найденного узла</param>
        /// <param name="endPosition">Возвращает позицию в файле конца найденного узла</param>
        /// <param name="content">Возвращает имя найденного тега или содержимое текстового блока</param>
        /// <param name="isOpenTag">Возвращяет true если найден открывающийся тег</param>
        /// <param name="tagToClose">При ненулевом значении указывает найти ближайший 
        /// закрывающийся тег с переданным в параметре именем (для тегов без html-содержимого)</param>
        /// <returns>Возвращает true при успешном поиске</returns>
        private static bool FindNextNode(BinaryReader reader,
            out HtmlNodeType nodeType,
            out long beginPosition,
            out long endPosition,
            out string content,
            out bool isOpenTag,
            string tagToClose)
        {
            try
            {
                beginPosition = -1;
                endPosition = -1;
                nodeType = HtmlNodeType.None;
                isOpenTag = false;
                content = string.Empty;
                bool isNotEmpty = false;

                // Определяем, что принимается за начало тега:
                // При работе с блоком не содержащим html-разметку (script, style)  
                // производится поиск тега, закрывающего этот блок '</...'. 
                // При работе с html-разметкой началом тега считается '<'
                string tagBegin;
                if (string.IsNullOrEmpty(tagToClose))
                    tagBegin = "<";
                else
                    tagBegin = "</" + tagToClose;

                while ((reader.PeekChar() >= 0) && !isNotEmpty)
                {
                    // Сохранение начала найденого узла
                    beginPosition = reader.BaseStream.Position;

                    // Если найдено начало тега
                    if (PeekString(reader, tagBegin.Length) == tagBegin)
                    {
                        // Обработка тегов и коментариев:

                        string tagStr = string.Empty;

                        bool isQuoteOpen = false;
                        bool isApostrOpen = false;

                        char ch;
                        do
                        {
                            ch = reader.ReadChar();
                            tagStr += ch;

                            // Установка признака открытых кавычек
                            if (ch == '\"') isQuoteOpen = !isQuoteOpen;
                            if (ch == '\'') isApostrOpen = !isApostrOpen;
                        }
                        while ((reader.PeekChar() >= 0) && (isQuoteOpen || isApostrOpen || (ch != '>')));

                        // Проверка на соответствие синтаксису тега и обработка
                        Regex tagNamePatt = new Regex(@"^<(\/?)(\w+)(?:\s+.*?)?(\\?)\s*>$", RegexOptions.Singleline);
                        Match tagNameMatch = tagNamePatt.Match(tagStr);
                        if (tagNameMatch.Success)
                        {
                            // Установка типа тега - тег с содержимым или одиночный тег
                            nodeType = (tagNameMatch.Groups[3].Value == "\\") ? HtmlNodeType.SingleTag : HtmlNodeType.ContainerTag;

                            endPosition = reader.BaseStream.Position;
                            content = tagNameMatch.Groups[2].Value;
                            isOpenTag = (tagNameMatch.Groups[1].Value != "/");

                            isNotEmpty = true;
                            continue;
                        }

                        // Обработка коментариев
                        if ((tagStr.Length >= 4) && (tagStr.Substring(0, 4) == "<!--"))
                        {
                            string closeCommentStr = tagStr.Substring(tagStr.Length - 3, 3);

                            // Поиск конца коментария
                            while ((reader.PeekChar() >= 0) && (closeCommentStr != "-->"))
                            {
                                do
                                {
                                    ch = reader.ReadChar();
                                    closeCommentStr = closeCommentStr.Substring(1, 2) + ch;
                                }
                                while ((reader.PeekChar() >= 0) && (ch != '>'));
                            }
                            nodeType = HtmlNodeType.Comment;
                            endPosition = reader.BaseStream.Position;
                            isNotEmpty = true;
                            continue;
                        }
                    }
                    else
                    {
                        // Обработка блоков текста:

                        content = string.Empty;
                        char ch;

                        // Чтение до следующего тега
                        while ((reader.PeekChar() >= 0) &&
                          ((reader.PeekChar() != tagBegin[0]) || (PeekString(reader, tagBegin.Length) != tagBegin)))
                        {
                            ch = reader.ReadChar();
                            isNotEmpty = isNotEmpty || ((ch != '\0') && (ch != '\r') && (ch != '\n') &&
                                (ch != '\r') && (ch != '\t') && (ch != ' '));
                            content += ch;
                        }

                        // Если текстовый блок содержит информацию
                        if (isNotEmpty)
                        {
                            nodeType = HtmlNodeType.TextBlock;
                            endPosition = reader.BaseStream.Position;
                        }
                    }
                }  
                
                return isNotEmpty;
            }
            catch (Exception e)
            {
                Logger.LogError("Ошибка в методе 'FindNextNode'", string.Empty);
                throw e;
            }

        }

        /// <summary>
        /// Возвращает чтение строки указанной длины без смещения позиции
        /// </summary>
        /// <param name="reader">Объект для чтения обрабатываемого файла</param>
        /// <param name="count">Количество символов</param>
        /// <returns>Прочитанная строка</returns>
        private static string PeekString(BinaryReader reader, int count)
        {
            if ((reader == null) || (count <= 0)) return string.Empty;

            try
            {
                if (count == 1) return string.Empty + (char)reader.PeekChar();

                long pos = reader.BaseStream.Position;
                string peekedStr = new string(reader.ReadChars(count));
                reader.BaseStream.Position = pos;
                return peekedStr;
            }
            catch (Exception e)
            {
                Logger.LogError("Ошибка в методе 'PeekString'", string.Empty);
                throw e;
            }
        }

    }
}
