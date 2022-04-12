using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace XMindParser
{
    public class ALAGraph
    {
        public Dictionary<string, Node> NodesById = new Dictionary<string, Node>();
        public Dictionary<string, string> InstantiationLines = new Dictionary<string, string>();
        public List<string> WiringLines = new List<string>();
        public Dictionary<string, JObject> SheetDictionary;
        public List<JObject> SheetList;
        public JObject currentSheet;

        public ALAGraph()
        {
        }

        public void GetSheets(string json)
        {
            JArray mainArray = (JArray)JsonConvert.DeserializeObject(json);
            List<JObject> sheetList = new List<JObject>();
            Dictionary<string, JObject> sheetDictionary = new Dictionary<string, JObject>();
            foreach (JObject sheet in mainArray.Children<JObject>())
            {
                sheetDictionary[sheet.Property("title").Value.ToString()] = sheet;
                sheetList.Add(sheet);
            }

            SheetDictionary = sheetDictionary;
            SheetList = sheetList;
        }


        public void GetNodesById(string sheetName)
        {
            JObject rootJObject = !string.IsNullOrEmpty(sheetName)
                ? (JObject)SheetDictionary[sheetName].Property("rootTopic").Value
                : (JObject)SheetList[0].Property("rootTopic").Value;
            Node rootNode = new Node()
            {
                Id = rootJObject.Property("id").Value.ToString(),
                Content = rootJObject.Property("title").Value.ToString(),
                NodeJObject = rootJObject
            };
            NodesById[rootNode.Id] = rootNode;
            RecursiveDepthFirstSearch(rootNode);
        }

        public void RecursiveDepthFirstSearch(Node parent)
        {
            // Base case
            if (!parent.NodeJObject.ContainsKey("children") ||
                !((JObject)parent.NodeJObject.Property("children").Value).ContainsKey("attached"))
            {
                return;
            }
            else
            {
                foreach (var jToken in ((JObject)parent.NodeJObject.Property("children").Value)
                    .Property("attached").Value)
                {
                    JObject childJObject = (JObject)jToken;
                    Node childNode = new Node()
                    {
                        Id = childJObject.Property("id").Value.ToString(),
                        Content = childJObject.Property("title").Value.ToString(),
                        NodeJObject = childJObject
                    };
                    NodesById[childNode.Id] = childNode;
                    parent.Children.Add(childNode);
                    childNode.Parent = parent;
                    RecursiveDepthFirstSearch(childNode);
                }
            }
        }

        public static bool IsPort(Node child)
        {
            string fullName = child.NodeJObject.ContainsKey("title")
                ? child.NodeJObject.Property("title").Value.ToString()
                : "";
            fullName = Regex.Replace(fullName, @", ", ",");
            if (fullName.StartsWith("$") || fullName.StartsWith("//") || fullName.StartsWith("\\")) return false;
            fullName = Regex.Replace(fullName, @"[\*#]", "");
            List<string> names =
                new List<string>(fullName.Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
            List<string> varDescriptors =
                new List<string>(names[0].Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
            return new string[] { "<", ">", "<>", "<<", ">>" }.Contains(varDescriptors[0]);
        }

        private Tuple<Instance, Port> ParseAsInstanceOrPort(Node child)
        {
            if (child == null) return new Tuple<Instance, Port>(null, null);

            Instance returnInstance = null;
            Port returnPort = null;

            string fullName = child.NodeJObject.ContainsKey("title")
                ? child.NodeJObject.Property("title").Value.ToString()
                : "";
            fullName = Regex.Replace(fullName, @", ", ",");
            if (fullName.StartsWith("$") || fullName.StartsWith("//") || fullName.StartsWith("\\"))
                return new Tuple<Instance, Port>(null, null);
            if (new HashSet<string> { "", "Application", "root", "Root", "Central Topic", "Main Topic" }.Contains(fullName))
                return new Tuple<Instance, Port>(null, null);
            string unparsedId = child.NodeJObject.Property("id").Value.ToString();
            string parsedId = Regex.Replace(child.NodeJObject.Property("id").Value.ToString(), @"-", "");

            List<string> names =
                new List<string>(fullName.Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
            names[0] = Regex.Replace(names[0], @"[\*#]", "");

            List<string> varDescriptors =
                new List<string>(names[0].Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
            bool isPort = new string[] { "<", ">", "<>", "<<", ">>" }.Contains(varDescriptors[0]);
            List<string> varNames = varDescriptors.Where(s => Regex.IsMatch(s, @"\w")).ToList();

            if (!isPort)
            {
                if (varNames.Count == 1)
                {
                    varNames.Add($"id_{parsedId}");
                }

                Instance instance = new Instance()
                {
                    VarId = parsedId,
                    JsonId = unparsedId,
                    InstanceJObject = child.NodeJObject,
                    ParentJObject = child.Parent?.NodeJObject,
                    FullName = fullName,
                    InstanceType = varNames[0],
                    InstanceName = varNames[1],
                    PublicProperties = new List<string>()
                        {
                            $"InstanceName = {((varNames[1].StartsWith("id_")) ? "\"Default\"" : $"\"{varNames[1]}\"")}"
                        },
                    Node = child
                };

                for (int i = 1; i < names.Count; i++)
                {
                    string name = names[i];
                    if (Regex.IsMatch(name, @"^([\w\d]*:)"))
                    {
                        instance.NamedConstructorParameters.Add(name);
                    }
                    else if (Regex.IsMatch(name, @"^([\w\d]*\s*=)"))
                    {
                        if (instance.PublicProperties.Count == 0 || !name.StartsWith("InstanceName"))
                            instance.PublicProperties.Add(name);
                    }
                    else
                    {
                        instance.UnnamedConstructorParameters.Add(name);
                    }
                }

                returnInstance = instance;
            }
            else
            {
                if (varNames.Count == 1) varNames.Add("NEEDNAME");
                Node instance = !IsPort(child.Parent) ? child.Parent : child.Children.FirstOrDefault();
                Port port = new Port()
                {
                    JsonId = unparsedId,
                    DataFlowDirection = varDescriptors[0],
                    PortType = varNames[0],
                    PortName = varNames[1],
                    FullName = fullName,
                    ParentInstanceNode = instance,
                    Node = child
                };

                returnPort = port;
            }

            return new Tuple<Instance, Port>(returnInstance, returnPort);
        }

        public void GetWiring()
        {
            foreach (Node nodeA in NodesById.Values)
            {
                var tuple = ParseAsInstanceOrPort(nodeA);
                bool isInstance = tuple.Item1 != null && tuple.Item2 == null;

                if (isInstance)
                {
                    Wire wire = new Wire()
                    {
                        A = tuple.Item1
                    };

                    Instantiate(wire.A);
                    nodeA.Children.Add(
                        NodesById[
                            nodeA.Parent
                                .Id]); // An instance's parent node is always a port, and should be a possible candidate for P1

                    foreach (Node nodeP1 in nodeA.Children)
                    {
                        wire.P1 = ParseAsInstanceOrPort(nodeP1).Item2;
                        foreach (Node nodeP2 in nodeP1.Children)
                        {
                            wire.P2 = ParseAsInstanceOrPort(nodeP2).Item2;
                            if (wire.P2 == null) continue;
                            wire.B = ParseAsInstanceOrPort(wire.P2.ParentInstanceNode).Item1;
                            if (wire.B == null) continue;
                            Instantiate(wire.B);
                            WiringLines.Add(wire.ToString());
                        }
                    }
                }
            }
        }

        public void Instantiate(Instance instance)
        {
            if (instance == null || InstantiationLines.ContainsKey(instance.JsonId) ||
                instance.InstanceType.StartsWith("@")) return;

            StringBuilder sb = new StringBuilder();
            sb.Append($"{instance.InstanceType} {instance.InstanceName} = new {instance.InstanceType}(");
            foreach (var parameter in instance.UnnamedConstructorParameters) sb.Append($"{parameter}, ");
            foreach (var parameter in instance.NamedConstructorParameters) sb.Append($"{parameter}, ");
            sb.Append(") ");
            sb.Append("{ ");
            foreach (var parameter in instance.PublicProperties) sb.Append($"{parameter}, ");
            string initLine = sb.ToString();
            initLine += " };";
            initLine = Regex.Replace(initLine, @",\s*(?=[\}\)]+)", " ");

            InstantiationLines[instance.JsonId] = initLine;
        }

        public void GetRelationships(string sheetName)
        {
            JObject rootJObject = !string.IsNullOrEmpty(sheetName) ? SheetDictionary[sheetName] : SheetList[0];
            if (!rootJObject.ContainsKey("relationships")) return;
            JArray relationships = (JArray)rootJObject.Property("relationships").Value;
            foreach (var relationship in relationships)
            {
                var rel = (JObject)relationship;
                string end1Id = rel.Property("end1Id").Value.ToString();
                string end2Id = rel.Property("end2Id").Value.ToString();
                if (NodesById.ContainsKey(end1Id) && NodesById.ContainsKey(end2Id))
                {
                    NodesById[end1Id].Children.Add(NodesById[end2Id]);
                }
            }
        }
    }
}