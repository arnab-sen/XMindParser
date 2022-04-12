using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Windows.Documents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace XMindParser
{
    public class XMindGraph
    {
        public readonly Dictionary<string, string> JsonTemplates;
        public readonly JObject SheetTemplate;
        public List<XMindNode> MainTopics = new List<XMindNode>();
        public List<XMindSheet> Sheets = new List<XMindSheet>();
        public Dictionary<string, XMindSheet> SheetDictionary = new Dictionary<string, XMindSheet>();

        public XMindGraph(string jsonString = null)
        {
            JsonTemplates = GetJsonTemplates();
            SheetTemplate = JObject.FromObject(new
            {
                id = "id",
                title = "title",
                theme = JObject.FromObject(JsonConvert.DeserializeObject(JsonTemplates["theme"])),
                legend = JObject.FromObject(JsonConvert.DeserializeObject(JsonTemplates["legend"])),
                topicPositioning = "fixed"
            });

            if (jsonString != null) CreateGraphFromJson(jsonString);
        }

        public static Dictionary<string, string> GetJsonTemplates()
        {
            var templates = new Dictionary<string, string>();
            templates["theme"] =
                @"{""relationship"":{""type"":""relationship"",""properties"":{""arrow-begin-class"":""org.xmind.arrowShape.none"",""arrow-end-class"":""org.xmind.arrowShape.triangle"",""fo:color"":""#BF1E1B"",""fo:font-family"":""ComicSansMS"",""fo:font-size"":""12pt"",""line-color"":""#BF1E1B"",""line-pattern"":""dash"",""line-width"":""1pt"",""shape-class"":""org.xmind.relationshipShape.curved""}},""centralTopic"":{""type"":""topic"",""properties"":{""fo:color"":""#004080"",""fo:font-family"":""Verdana"",""fo:font-weight"":""normal"",""line-class"":""org.xmind.branchConnection.curve"",""line-color"":""#004080"",""shape-class"":""org.xmind.topicShape.roundedRect"",""svg:fill"":""#68A3DF""}},""subTopic"":{""type"":""topic"",""properties"":{""fo:color"":""#004080"",""fo:font-family"":""Verdana"",""svg:fill"":""none""}},""floatingTopic"":{""type"":""topic"",""properties"":{""fo:color"":""#004080"",""fo:font-family"":""Verdana"",""fo:font-weight"":""normal"",""line-class"":""org.xmind.branchConnection.curve"",""line-color"":""#004080"",""shape-class"":""org.xmind.topicShape.roundedRect"",""svg:fill"":""#D3DFFF""}},""mainTopic"":{""type"":""topic"",""properties"":{""fo:color"":""#004080"",""fo:font-family"":""Verdana"",""svg:fill"":""#D3DFFF""}},""summaryTopic"":{""type"":""topic"",""properties"":{""border-line-width"":""0pt"",""fo:color"":""#BF1E1B"",""fo:font-family"":""ComicSansMS"",""fo:font-size"":""12pt"",""line-class"":""org.xmind.branchConnection.arrowedCurve"",""shape-class"":""org.xmind.topicShape.ellipse"",""svg:fill"":""none""}},""boundary"":{""type"":""boundary"",""properties"":{""fo:color"":""#535353"",""fo:font-family"":""Verdana"",""line-color"":""#999999"",""line-pattern"":""dash"",""line-width"":""1pt"",""shape-class"":""org.xmind.boundaryShape.scallops"",""svg:fill"":""#FFFFFF""}}}";

            templates["legend"] = @"{""groups"":{},""markers"":{}}";

            templates["rootTopic"] =
                @"{""id"":""4a682ed6646ba1c3a56668bfad"",""structureClass"":""org.xmind.ui.map.unbalanced"",""title"":""DataLink"",""extensions"":[{""provider"":""org.xmind.ui.map.unbalanced"",""content"":[{""name"":""right-number"",""content"":""1""}]}],""children"":{""attached"":[]}}";

            return templates;
        }

        public void GetSheetsFromJson(string json)
        {
            JArray mainArray = (JArray)JsonConvert.DeserializeObject(json);
            List<JObject> sheetList = new List<JObject>();
            foreach (JObject sheet in mainArray.Children<JObject>())
            {
                XMindSheet xmindSheet = new XMindSheet(fromExistingJObject: sheet);
                SheetDictionary[sheet.Property("title").Value.ToString()] = xmindSheet;
                Sheets.Add(xmindSheet);
            }
        }


        private void AddNewSheetByRoot(XMindNode root)
        {
            MainTopics.Add(root);
        }

        public XMindSheet NewSheet(string title = null, XMindNode root = null)
        {
            var sheet = new XMindSheet(title ?? $"Sheet {Guid.NewGuid()}", mainTopic: root);
            Sheets.Add(sheet);
            return sheet;
        }

        public JObject CreateGraphFromJson(string jsonString)
        {
            string json = jsonString;
            List<JObject> sheets = JArray.FromObject(JsonConvert.DeserializeObject(json)).Select(jToken => (JObject)jToken).ToList();
            JObject mainSheet = sheets[0];
            JObject sheet = JObject.FromObject(SheetTemplate.DeepClone());

            return mainSheet;
        }

        public void RecursiveDepthFirstSearch(JObject node)
        {
            // Base case
            if (!node.ContainsKey("children") || !((JObject)node.Property("children").Value).ContainsKey("attached"))
            {
                return;
            }
            else
            {
                foreach (var jToken in ((JObject)node.Property("children").Value).Property("attached").Value)
                {
                    JObject child = (JObject)jToken;
                    RecursiveDepthFirstSearch(child);
                }
            }
        }

        public string ToJson()
        {
            // Each Sheet contains a root topic, the child of which is another node called the main topic
            string json = "";
            JArray sheetArray = JArray.FromObject(Sheets.Select(xmindSheet => xmindSheet.Sheet));
            json = JsonConvert.SerializeObject(sheetArray);
            return json;
        }
    }

}