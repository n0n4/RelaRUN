using System;
using System.Collections.Generic;
using System.Text;

namespace RelaRUN.PackGen.GenItems
{
    public class ItemFloatArray : IGenItem
    {
        public string Name;

        public ItemFloatArray(string name)
        {
            Name = name;
        }

        public void AddGetLength(StringBuilder sb, string obj)
        {
            sb.Append("Bytes.GetStringsLength(").Append(obj).Append(".").Append(Name).Append(")");
        }

        public void AddPack(StringBuilder sb, string obj, string prefix)
        {
            // write the count
            sb.Append(prefix).Append("sent.WriteByte((byte)").Append(obj).Append(".").Append(Name).AppendLine(".Length);");

            // write the items
            sb.Append(prefix).Append("for (int i = 0; i < ").Append(obj).Append(".").Append(Name).AppendLine(".Length; i++)");
            sb.Append(prefix).AppendLine("{");
            sb.Append(prefix).Append("\tsent.WriteFloat(").Append(obj).Append(".").Append(Name).AppendLine("[i]);");
            sb.Append(prefix).AppendLine("}");
        }

        public void AddUnpack(StringBuilder sb, string obj, string data, string prefix)
        {
            // read the count
            sb.Append(prefix).AppendLine("{");
            sb.Append(prefix).Append("\tint scount = ").Append(data).AppendLine("[c]; c++;");

            // instantiate array if needed
            sb.Append(prefix).Append("\tif (").Append(obj).Append(".").Append(Name).Append(" == null || ")
                .Append(obj).Append(".").Append(Name).AppendLine(".Length != scount)");

            sb.Append(prefix).Append("\t\t").Append(obj).Append(".").Append(Name).AppendLine(" = new float[scount];");

            // read strings
            sb.Append(prefix).Append("\tfor (int i = 0; i < scount; i++)");
            sb.Append(prefix).Append(prefix).AppendLine("\t{");
            sb.Append("\t\t").Append(obj).Append(".").Append(Name).Append("[i] = Bytes.Readfloat(").Append(data).AppendLine(", c); c += 4;");
            sb.Append(prefix).AppendLine("\t}");
            sb.Append(prefix).AppendLine("}");
        }
    }
}
