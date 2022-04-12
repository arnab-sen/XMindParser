using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace XMindParser
{
    public class XMindSheet
    {
        public JObject Sheet;

        public string Id
        {
            get => id;
            set => id = value;
        }

        private string id;

        public XMindSheet(string title = null, XMindNode mainTopic = null, JObject fromExistingJObject = null)
        {
            var templates = XMindGraph.GetJsonTemplates();
            string newSheetId = Guid.NewGuid().ToString();
            Sheet = fromExistingJObject ?? JObject.FromObject(new
            {
                id = newSheetId,
                title = title ?? $"Sheet {newSheetId.Substring(0, 5)}",
                theme = JObject.FromObject(JsonConvert.DeserializeObject(templates["theme"])),
                legend = JObject.FromObject(JsonConvert.DeserializeObject(templates["legend"])),
                rootTopic = JObject.FromObject(JsonConvert.DeserializeObject(templates["rootTopic"])),
                topicPositioning = "fixed",
                relationships = new JArray()
            });

            if (fromExistingJObject == null)
            {
                GetRootTopic().Property("id").Value = Guid.NewGuid().ToString();
                Id = newSheetId;
            }
            else
            {
                Id = Sheet.Property("id").Value.ToString();
            }

            if (mainTopic != null) AddNodeToRootTopic(mainTopic);
        }

        public JObject GetRootTopic()
        {
            return (JObject)Sheet.Property("rootTopic").Value;
        }

        public void SetRootTopic(JObject rootTopic)
        {
            Sheet.Property("rootTopic").Value = rootTopic;
        }

        public void SetRootTopicText(string content)
        {
            ((JObject)Sheet.Property("rootTopic").Value).Property("title").Value = content;
        }

        public void AddNodeToRootTopic(XMindNode node)
        {
            AddNodeToRootTopic((JObject)node);
        }

        public void AddNodeToRootTopic(JObject node)
        {
            ((JArray)((JObject)GetRootTopic().Property("children").Value).Property("attached").Value).Add(node);
        }

        public void SetTopicText(string id, string content)
        {

        }

        public JObject GetFirstOccurrenceOf(string content)
        {
            return new JObject();
        }

        public JObject GetTopicById(string id)
        {
            return new JObject();
        }

        public void AddRelationship(XMindNode source, XMindNode destination)
        {
            ((JArray)Sheet.Property("relationships").Value).Add(JObject.FromObject(new
            {
                id = Guid.NewGuid().ToString(),
                end1Id = source.Id,
                end2Id = destination.Id
            }));
        }

        /// <summary>
        /// Adds a root to the current sheet, attached to the central topic.
        /// </summary>
        /// <param name="node"></param>
        public void AddRoot(XMindNode node)
        {
            AddNodeToRootTopic(node);
        }
    }
}