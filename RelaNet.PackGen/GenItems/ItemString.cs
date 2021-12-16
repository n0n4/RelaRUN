using System;
using System.Collections.Generic;
using System.Text;

namespace RelaNet.PackGen.GenItems
{
    public class ItemString : IGenItem
    {
        public string Name;

        public ItemString(string name)
        {
            Name = name;
        }

        public void AddGetLength(StringBuilder sb, string obj)
        {
            sb.Append("Bytes.GetStringLength(").Append(obj).Append(".").Append(Name).Append(")");
        }

        public void AddPack(StringBuilder sb, string obj, string prefix)
        {
            sb.Append(prefix).Append("sent.WriteString(").Append(obj).Append(".").Append(Name).AppendLine(");");
        }

        public void AddUnpack(StringBuilder sb, string obj, string data, string prefix)
        {
            sb.Append(prefix).Append("{ ").Append(obj).Append(".").Append(Name).Append(" = Bytes.ReadString(").Append(data).AppendLine(", c, out int len); c += len; }");
        }
    }
}
