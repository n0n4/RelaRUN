using System;
using System.Collections.Generic;
using System.Text;

namespace RelaRUN.PackGen.GenItems
{
    public class ItemShort : IGenItem
    {
        public string Name;

        public ItemShort(string name)
        {
            Name = name;
        }

        public void AddGetLength(StringBuilder sb, string obj)
        {
            sb.Append("2");
        }

        public void AddPack(StringBuilder sb, string obj, string prefix)
        {
            sb.Append(prefix).Append("sent.WriteShort(").Append(obj).Append(".").Append(Name).AppendLine(");");
        }

        public void AddUnpack(StringBuilder sb, string obj, string data, string prefix)
        {
            sb.Append(prefix).Append(obj).Append(".").Append(Name).Append(" = Bytes.ReadShort(").Append(data).AppendLine(", c); c += 2;");
        }
    }
}
