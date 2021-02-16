using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace HtmlWordsCounter
{
    /// <summary>
    /// Класс, описывающий элемент html-документа
    /// </summary>
    class HtmlNode
    {
        /// <summary>
        /// Список дочерних элементов
        /// </summary>        
        public List<HtmlNode> children { get; set; }

        /// <summary>
        /// Ссылка на родительский элемент
        /// </summary>        
        public HtmlNode ParentNode { get; set; }

        /// <summary>
        /// Позиция в файле начала данного элемента 
        /// </summary>        
        public long beginPosition { get; set; }
        /// <summary>
        /// Позиция в файле конца данного элемента 
        /// </summary>
        public long endPosition { get; set; }

        /// <summary>
        /// Имя тега (для элементов типа 'ContainerTag' и 'SingleTag')
        /// </summary>
        public string tagName  { get; set; }

        /// <summary>
        /// Тип элемента
        /// </summary>
        public HtmlNodeType nodeType { get; set; }

        /// <summary>
        /// Список имен одиночных тегов
        /// </summary>
        private static List<string> singleTags = new List<string>() { "br", "source", "meta", "link", "img", "input" };
        /// <summary>
        /// Список имен тегов не содержащих html-разметку
        /// </summary>
        private static List<string> notHtmlContantTags = new List<string>() { "script", "style" };

        /// <summary>
        /// Инициализирует объект типа HtmlNode
        /// </summary>
        public HtmlNode() : this(null) { }

        /// <summary>
        /// Инициализирует объект типа HtmlNode
        /// </summary>
        /// <param name="parentNode">Родительский элемент</param>
        public HtmlNode(HtmlNode parentNode)
        {
            this.ParentNode = parentNode;
            this.children = null;

            this.nodeType = HtmlNodeType.None;
            this.tagName = string.Empty;
            this.beginPosition = -1;
            this.endPosition = -1;
        }

        /// <summary>
        /// Инициализирует объект типа HtmlNode
        /// </summary>
        /// <param name="nodeType">Тип элемента</param>
        /// <param name="beginPosition">Позиция в файле начала данного элемента </param>
        /// <param name="endPosition">Позиция в файле конца данного элемента </param>
        public HtmlNode(HtmlNodeType nodeType, long beginPosition, long endPosition) : 
            this(null, nodeType, string.Empty, beginPosition, endPosition) { }

        /// <summary>
        /// Инициализирует объект типа HtmlNode
        /// </summary>
        /// <param name="nodeType">Тип элемента</param>
        /// <param name="tagName">Имя тега</param>
        /// <param name="beginPosition">Позиция в файле начала данного элемента </param>
        /// <param name="endPosition">Позиция в файле конца данного элемента </param>
        public HtmlNode(HtmlNodeType nodeType, string tagName, long beginPosition, long endPosition) :
            this(null, nodeType, tagName, beginPosition, endPosition) { }

        /// <summary>
        /// Инициализирует объект типа HtmlNode
        /// </summary>
        /// <param name="parentNode">Родительский элемент</param>
        /// <param name="nodeType">Тип элемента</param>
        /// <param name="tagName">Имя тега</param>
        /// <param name="beginPosition">Позиция в файле начала данного элемента </param>
        /// <param name="endPosition">Позиция в файле конца данного элемента </param>
        public HtmlNode(HtmlNode parentNode, HtmlNodeType nodeType, string tagName, long beginPosition, long endPosition)
        {
            this.ParentNode = parentNode;
            this.children = null;

            this.nodeType = nodeType;
            this.tagName = tagName;
            this.beginPosition = beginPosition;
            this.endPosition = endPosition;
        }

        /// <summary>
        /// Определяет является тег одиночным
        /// </summary>
        /// <param name="tagName">Имя тега</param>
        /// <returns>Возвращает true, если тег является одиночным</returns>
        public static bool isOnlySingleTag(string tagName)
        {
            return singleTags.Contains(tagName);
        }
        /// <summary>
        /// Определяет, может ли тег содержать html-разметку
        /// </summary>
        /// <param name="tagName">Имя тега</param>
        /// <returns>Возвращает true, если тег может содержать html-разметку</returns>
        public static bool hasHtmlContant(string tagName)
        {
            return !notHtmlContantTags.Contains(tagName);
        }

        /// <summary>
        /// Определяет, может ли элемент содержать html-разметку
        /// </summary>
        /// <returns>Возвращает true, если тег может содержать html-разметку</returns>
        public bool hasHtmlContant()
        {
            return HtmlNode.hasHtmlContant(this.tagName);
        }

        /// <summary>
        /// Добавляет дочерний элемент
        /// </summary>
        /// <param name="node">Добавляемый элемент</param>
        /// <returns>Возвращает true, если элемент был успешно добавлен</returns>
        public bool AppendChild(HtmlNode node)
        {
            if ((this.nodeType != HtmlNodeType.TextBlock) && (this.hasHtmlContant()))
            {
                if (this.children == null) children = new List<HtmlNode>();

                this.children.Add(node);
                node.ParentNode = this;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Закрывает текущий или один из родительских тегов
        /// </summary>
        /// <param name="tagName">Имя тега</param>
        /// <returns>Возвращает закрытый тег</returns>
        public HtmlNode CloseTag(string tagName)
        {
            if (string.IsNullOrEmpty(tagName) || (this.tagName == tagName)) return this;

            HtmlNode node = this;

            while(node.ParentNode != null)
            {
                node = node.ParentNode;

                if (node.tagName == tagName)
                    return node;
            }

            return null;
        }

        public string ToStr(string tab = "")
        {
            string result = string.Format("{0}<{1}> {2}-{3}", tab, tagName, beginPosition, endPosition);

            if (children != null)
            {
                foreach (HtmlNode node in children)
                    if (node.nodeType != HtmlNodeType.TextBlock)
                        result += "\r\n" + node.ToStr(tab + "| ");
                    else
                        result += "\r\n" + tab + "| " + "textblock";
            }
            //result += string.Format("{0}</{1}>\r\n", tab, tagName);
            return result;
        }
    }
}
