using System;
using System.Collections.Generic;
using System.Text;

namespace RelaRUN.PackGen.GenItems
{
    public interface IGenItem
    {
        void AddGetLength(StringBuilder sb, string obj);
        void AddPack(StringBuilder sb, string obj, string prefix);
        void AddUnpack(StringBuilder sb, string obj, string data, string prefix);
    }
}
