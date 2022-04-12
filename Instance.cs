using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace XMindParser
{
    public class Instance
    {
        public Instance()
        {
        }

        public string VarId;
        public string JsonId;
        public string FullName = "";
        public JObject InstanceJObject;
        public List<Port> Ports = new List<Port>();
        public List<Tuple<Instance, string>> Parents = new List<Tuple<Instance, string>>();
        public List<Tuple<Instance, string>> Children = new List<Tuple<Instance, string>>();
        public List<string> UnnamedConstructorParameters = new List<string>();
        public List<string> NamedConstructorParameters = new List<string>();
        public List<string> PublicProperties = new List<string>();
        public List<Tuple<Instance, Port, Port, Instance>> TreeWiring;
        public List<Tuple<Instance, Port, Port, Instance>> ArrowWiring;

        public string InstanceType { get; set; }
        public string InstanceName { get; set; }
        public JObject ParentJObject { get; set; }
        public Node Node { get; set; }

        public override string ToString()
        {
            return $"{InstanceType} {InstanceName}";
        }
    }
}