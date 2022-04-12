using System.Linq;

namespace XMindParser
{
    public class Wire
    {
        public Instance A;
        public Port P1;
        public Port P2;
        public Instance B;
        public string WireType = "WireTo";

        public Wire(Instance a = null, Port p1 = null, Port p2 = null, Instance b = null)
        {
            A = a ?? new Instance();
            P1 = p1 ?? new Port();
            P2 = p2 ?? new Port();
            B = b ?? new Instance();
        }

        public bool IsReverseWire(Port port)
        {
            string[] reversePorts = new[] { "IDataFlowB", "IEventB" };
            bool isReversePort = false;

            foreach (string revPort in reversePorts)
            {
                if (port.PortType.StartsWith(revPort))
                {
                    isReversePort = true;
                    break;
                }
            }

            return (port.DataFlowDirection == "<<" || (port.DataFlowDirection == "<" && !isReversePort) ||
                    (port.DataFlowDirection == ">" && isReversePort));
        }

        public string WiringCode()
        {
            if (IsReverseWire(P1) && ALAGraph.IsPort(P1.Node.Children?.First()))
            {
                return $"{B.InstanceName}.{WireType}({A.InstanceName}, \"{P2.PortName}\");";
            }
            else
            {
                return $"{A.InstanceName}.{WireType}({B.InstanceName}, \"{P1.PortName}\");";
            }
        }

        public string WiringComment()
        {
            if (IsReverseWire(P1))
            {
                return
                    $"// ({B.InstanceType} ({B.InstanceName}).{P2.PortName}) -- [{P1.PortType}] --> ({A.InstanceType} ({A.InstanceName}).{P1.PortName})";
            }
            else
            {
                return
                    $"// ({A.InstanceType} ({A.InstanceName}).{P1.PortName}) -- [{P1.PortType}] --> ({B.InstanceType} ({B.InstanceName}).{P2.PortName})";
            }
        }

        public override string ToString()
        {
            return WiringCode() + " " + WiringComment();
        }
    }
}