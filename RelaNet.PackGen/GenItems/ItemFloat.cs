using System;
using System.Collections.Generic;
using System.Text;

namespace RelaNet.PackGen.GenItems
{
    public class ItemFloat : IGenItem
    {
        public string Name;

        public ItemFloat(string name)
        {
            Name = name;
        }

        public void AddGetLength(StringBuilder sb, string obj)
        {
            sb.Append("4");
        }

        public void AddPack(StringBuilder sb, string obj, string prefix)
        {
            sb.Append(prefix).Append("sent.WriteFloat(").Append(obj).Append(".").Append(Name).AppendLine(");");
        }

        public void AddUnpack(StringBuilder sb, string obj, string data, string prefix)
        {
            sb.Append(prefix).Append(obj).Append(".").Append(Name).Append(" = Bytes.ReadFloat(").Append(data).AppendLine(", c); c += 4;");
        }
    }
}
