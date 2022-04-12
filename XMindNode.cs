using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace XMindParser
{
    public class XMindNode
    {
        public string Id;

        public string Content
        {
            get => content;
            set
            {
                content = value;
                if (InnerJObject != null) InnerJObject.Property("title").Value = value;
            }
        }
        public JObject InnerJObject;
        public XMindNode Parent;
        public List<XMindNode> Children = new List<XMindNode>();

        public string Fill
        {
            get => fill;
            set
            {
                fill = value;

                if (InnerJObject != null)
                {
                    JObject newStyle = JObject.FromObject(new
                    {
                        type = "Topic",
                        properties = JObject.FromObject(JsonConvert.DeserializeObject($"{{ \"svg:fill\": \"{value}\" }}"))
                    });

                    if (InnerJObject.ContainsKey("style"))
                    {
                        InnerJObject.Property("style").Value.Replace(newStyle);
                    }
                    else
                    {
                        InnerJObject.Add("style", newStyle);
                    }
                }
            }
        }

        private string content;
        private string fill;

        public XMindNode()
        {
            Id = Guid.NewGuid().ToString();
            Content = $"Node {Id}";
            InnerJObject = JObject.FromObject(new
            {
                id = Id,
                title = Content,
                style = JObject.FromObject(new
                {
                    type = "Topic",
                    properties = JObject.FromObject(JsonConvert.DeserializeObject($"{{ \"svg:fill\": \"{Fill}\" }}"))
                }),
                children = JObject.FromObject(new
                {
                    attached = new JArray()
                })
            });
        }

        public static explicit operator JObject(XMindNode xmindNode)
        {
            // ((JObject)xmindNode.InnerJObject.Property("children").Value).Property("attached").Value = new JArray();
            //
            // foreach (var child in xmindNode.Children)
            // {
            //     xmindNode.AddChild(child);
            // }
            xmindNode.InnerJObject.Property("title").Value = xmindNode.Content;

            return xmindNode.InnerJObject;
        }

        public JObject ToJObject()
        {
            return (JObject)this;
        }

        public void AddChild(XMindNode child)
        {
            Children.Add(child);
            child.Parent = this;
            ((JArray)((JObject)InnerJObject.Property("children").Value).Property("attached").Value).Add(child.InnerJObject);
        }


        public static XMindNode FromJObject(JObject jObject)
        {
            XMindNode node = new XMindNode();
            node.InnerJObject = jObject.ToObject<JObject>();
            node.Id = node.InnerJObject.Property("id").Value.ToString();
            node.Content = node.InnerJObject.Property("title").Value.ToString();

            // Clear existing children so that duplicates are not added
            if (node.InnerJObject.ContainsKey("children") && ((JObject)node.InnerJObject.Property("children").Value).ContainsKey("attached"))
            {
                ((JObject)node.InnerJObject.Property("children").Value).Property("attached").Value = new JArray();
                foreach (JObject child in ((JObject)jObject.Property("children")?.Value)?.Property("attached").Value)
                {
                    node.AddChild(FromJObject(child));
                }
            }

            return node;
        }
    }
}