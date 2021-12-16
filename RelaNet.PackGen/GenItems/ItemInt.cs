using System;
using System.Collections.Generic;
using System.Text;

namespace RelaNet.PackGen.GenItems
{
    public class ItemInt : IGenItem
    {
        public string Name;

        public ItemInt(string name)
        {
            Name = name;
        }

        public void AddGetLength(StringBuilder sb, string obj)
        {
            sb.Append("4");
        }

        public void AddPack(StringBuilder sb, string obj, string prefix)
        {
            sb.Append(prefix).Append("sent.WriteInt(").Append(obj).Append(".").Append(Name).AppendLine(");");
        }

        public void AddUnpack(StringBuilder sb, string obj, string data, string prefix)
        {
            sb.Append(prefix).Append(obj).Append(".").Append(Name).Append(" = Bytes.ReadInt(").Append(data).AppendLine(", c); c += 4;");
        }
    }
}
