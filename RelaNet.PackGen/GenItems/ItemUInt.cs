using System;
using System.Collections.Generic;
using System.Text;

namespace RelaNet.PackGen.GenItems
{
    public class ItemUInt : IGenItem
    {
        public string Name;

        public ItemUInt(string name)
        {
            Name = name;
        }

        public void AddGetLength(StringBuilder sb, string obj)
        {
            sb.Append("4");
        }

        public void AddPack(StringBuilder sb, string obj, string prefix)
        {
            sb.Append(prefix).Append("sent.WriteUInt(").Append(obj).Append(".").Append(Name).AppendLine(");");
        }

        public void AddUnpack(StringBuilder sb, string obj, string data, string prefix)
        {
            sb.Append(prefix).Append(obj).Append(".").Append(Name).Append(" = Bytes.ReadUInt(").Append(data).AppendLine(", c); c += 4;");
        }
    }
}
