namespace XMindParser
{
    public class Port
    {
        public Port()
        {
        }

        public string JsonId;
        public string DataFlowDirection = ">";
        public string PortType = "IDataFlow<string>";
        public string PortName = "NEEDNAME";
        public string FullName { get; set; }
        public Node ParentInstanceNode { get; set; }
        public Node Node { get; set; }

        public override string ToString()
        {
            return $"{DataFlowDirection} {PortType} {PortName}";
        }
    }
}