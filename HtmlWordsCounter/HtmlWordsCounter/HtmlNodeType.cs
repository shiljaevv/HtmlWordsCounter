using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlWordsCounter
{
    public enum HtmlNodeType
    {
        None,
        ContainerTag,
        SingleTag,
        TextBlock,
        Comment
    }
}
