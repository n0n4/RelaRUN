﻿using RelaNet.PackGen.GenItems;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace RelaNet.PackGen
{
    public class GenInfo
    {
        public string TypeName = string.Empty;
        public List<IGenItem> Items = new List<IGenItem>();

        public static GenInfo Read(Type t)
        {
            FieldInfo[] fields = t.GetFields();
            Array.Sort(fields, (a, b) => { return a.Name.CompareTo(b.Name); });

            GenInfo info = new GenInfo();

            info.TypeName = t.Name;

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo f = fields[i];

                if (f.FieldType == typeof(bool))
                {
                    // check if we have any other open bool collections already
                    bool found = false;
                    for (int o = 0; o < info.Items.Count; o++)
                    {
                        if (!(info.Items[o] is ItemBoolCollection))
                            continue;

                        ItemBoolCollection bools = info.Items[o] as ItemBoolCollection;
                        if (bools.Count >= 8)
                            continue;

                        bools.Add(f.Name);
                        found = true;
                    }

                    if (found)
                        continue;

                    info.Items.Add(new ItemBoolCollection(f.Name));
                }
                else if (f.FieldType == typeof(int))
                {
                    info.Items.Add(new ItemInt(f.Name));
                }
                else if (f.FieldType == typeof(uint))
                {
                    info.Items.Add(new ItemUInt(f.Name));
                }
                else if (f.FieldType == typeof(byte))
                {
                    info.Items.Add(new ItemByte(f.Name));
                }
                else if (f.FieldType == typeof(ushort))
                {
                    info.Items.Add(new ItemUShort(f.Name));
                }
                else if (f.FieldType == typeof(short))
                {
                    info.Items.Add(new ItemShort(f.Name));
                }
                else if (f.FieldType == typeof(float))
                {
                    info.Items.Add(new ItemFloat(f.Name));
                }
                else if (f.FieldType == typeof(double))
                {
                    info.Items.Add(new ItemDouble(f.Name));
                }
                else if (f.FieldType == typeof(string))
                {
                    info.Items.Add(new ItemString(f.Name));
                }
                else if (f.FieldType.IsArray && f.FieldType.GetElementType() == typeof(string))
                {
                    info.Items.Add(new ItemStringArray(f.Name));
                }
                else if (f.FieldType.IsArray && f.FieldType.GetElementType() == typeof(float))
                {
                    info.Items.Add(new ItemFloatArray(f.Name));
                }
            }

            return info;
        }

        public void WriteClass(StringBuilder sb, string namespaceName, bool useRef)
        {
            sb.AppendLine("/* THIS CLASS WAS GENERATED BY PACKGEN */");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Text;");
            sb.AppendLine("using RelaNet.Messages;");
            sb.AppendLine("using RelaNet.Utilities;");
            sb.AppendLine();
            sb.Append("namespace ").AppendLine(namespaceName);
            sb.AppendLine("{");
            {
                sb.Append("\tpublic static class ").Append(TypeName).AppendLine("Packer");
                sb.AppendLine("\t{");
                {
                    WriteGetLengthMethod(sb, true, "\t\t");
                    sb.AppendLine();

                    WritePackMethod(sb, true, "\t\t");
                    sb.AppendLine();

                    WriteUnpackMethod(sb, true, useRef, "\t\t");
                    sb.AppendLine();
                }
                sb.AppendLine("\t}");
            }
            sb.AppendLine("}");
        }

        public void WriteGetLengthMethod(StringBuilder sb, bool isStatic, string prefix)
        {
            sb.Append(prefix).Append("public ");
            if (isStatic)
                sb.Append("static ");
            sb.Append("int GetWriteLength(").Append(TypeName)
                .AppendLine(" obj)");

            sb.Append(prefix).AppendLine("{");
            {
                sb.Append(prefix).Append("\treturn ");
                WriteGetLength(sb, "obj");
                sb.AppendLine(";");
            }
            sb.Append(prefix).AppendLine("}");
        }

        public void WriteGetLength(StringBuilder sb, string obj)
        {
            bool first = true;
            foreach (IGenItem i in Items)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(" + ");
                }
                i.AddGetLength(sb, obj);
            }
        }

        public void WritePackMethod(StringBuilder sb, bool isStatic, string prefix)
        {
            sb.Append(prefix).Append("public ");
            if (isStatic)
                sb.Append("static "); 
            sb.Append("void Pack(").Append(TypeName).AppendLine(" obj, Sent sent)");

            sb.Append(prefix).AppendLine("{");
            {
                WritePack(sb, "obj", prefix + "\t");
            }
            sb.Append(prefix).AppendLine("}");
        }

        public void WritePack(StringBuilder sb, string obj, string prefix)
        {
            foreach (IGenItem i in Items)
                i.AddPack(sb, obj, prefix);
        }

        public void WriteUnpackMethod(StringBuilder sb, bool isStatic, bool useRef, string prefix)
        {
            sb.Append(prefix).Append("public ");
            if (isStatic)
                sb.Append("static "); 
            sb.Append("int Unpack(");
            if (useRef)
                sb.Append("ref ");
            sb.Append(TypeName).AppendLine(" obj, Receipt receipt, int c)");

            sb.Append(prefix).AppendLine("{");
            {
                WriteUnpack(sb, "obj", "receipt.Data", prefix + "\t");

                sb.Append(prefix).AppendLine("\treturn c;");
            }
            sb.Append(prefix).AppendLine("}");
        }

        public void WriteUnpack(StringBuilder sb, string obj, string data, string prefix)
        {
            foreach (IGenItem i in Items)
                i.AddUnpack(sb, obj, data, prefix);
        }
    }
}
