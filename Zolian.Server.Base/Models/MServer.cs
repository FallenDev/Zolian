using System.Net;
using System.Xml.Serialization;

namespace Darkages.Models;

public class MServer
{
    public MServer() { }

    public MServer(byte id, IPAddress address, ushort port, string name, string description)
    {
        ID = id;
        Address = address;
        Port = port;
        Name = name;
        Description = description;
    }

    [XmlElement("ID")] public byte ID { get; set; }
    [XmlIgnore] public IPAddress Address { get; set; }
    [XmlElement("Addr")]
    public string AddressString
    {
        get => Address.ToString();
        set => Address = IPAddress.Parse(value);
    }
    [XmlElement("Port")] public ushort Port { get; set; }
    [XmlElement("Name")] public string Name { get; set; }
    [XmlElement("Desc")] public string Description { get; set; }
}