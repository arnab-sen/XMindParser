using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace XMindParser
{
    public class Node
    {
        public Node()
        {
        }

        public string Id;
        public string Content;
        public Node Parent;
        public List<Node> Children = new List<Node>();
        public JObject NodeJObject;

        public override string ToString()
        {
            return Content;
        }
    }
}