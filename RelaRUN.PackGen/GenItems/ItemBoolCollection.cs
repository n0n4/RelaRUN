using System;
using System.Collections.Generic;
using System.Text;

namespace RelaRUN.PackGen.GenItems
{
    public class ItemBoolCollection : IGenItem
    {
        public string[] Names = new string[8];
        public int Count = 0;

        public ItemBoolCollection(string name)
        {
            Add(name);
        }

        public void Add(string name)
        {
            Names[Count] = name;
            Count++;
        }

        public void AddGetLength(StringBuilder sb, string obj)
        {
            sb.Append("1");
        }

        public void AddPack(StringBuilder sb, string obj, string prefix)
        {
            sb.Append(prefix).AppendLine("{");
            sb.Append(prefix).AppendLine("\tbyte scratch = 0;");
            for (int i = 0; i < Count; i++)
            {
                sb.Append(prefix).Append("\tif (").Append(obj).Append(".").Append(Names[i]).AppendLine(")");
                sb.Append(prefix).Append("\t\tscratch = Bits.AddTrueBit(scratch, ").Append(i).AppendLine(");");
            }

            sb.Append(prefix).AppendLine("\tsent.WriteByte(scratch);");
            sb.Append(prefix).AppendLine("}");
        }

        public void AddUnpack(StringBuilder sb, string obj, string data, string prefix)
        {
            sb.Append(prefix).AppendLine("{");
            sb.Append(prefix).Append("\tbyte scratch = ").Append(data).AppendLine("[c]; c++;");
            for (int i = 0; i < Count; i++)
            {
                sb.Append(prefix).Append("\t").Append(obj).Append(".").Append(Names[i]).Append(" = Bits.CheckBit(scratch, ").Append(i).AppendLine(");");
            }

            sb.Append(prefix).AppendLine("}");
        }
    }
}
