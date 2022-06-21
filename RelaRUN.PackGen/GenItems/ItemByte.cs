using System;
using System.Collections.Generic;
using System.Text;

namespace RelaRUN.PackGen.GenItems
{
    public class ItemByte : IGenItem
    {
        public string Name;

        public ItemByte(string name)
        {
            Name = name;
        }

        public void AddGetLength(StringBuilder sb, string obj)
        {
            sb.Append("1");
        }

        public void AddPack(StringBuilder sb, string obj, string prefix)
        {
            sb.Append(prefix).Append("sent.WriteByte(").Append(obj).Append(".").Append(Name).AppendLine(");");
        }

        public void AddUnpack(StringBuilder sb, string obj, string data, string prefix)
        {
            sb.Append(prefix).Append(obj).Append(".").Append(Name).Append(" = ").Append(data).AppendLine("[c]; c++;");
        }
    }
}
