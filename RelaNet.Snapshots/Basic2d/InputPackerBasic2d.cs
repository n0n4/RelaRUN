using System;
using System.Collections.Generic;
using System.Text;
using RelaNet.Messages;
using RelaNet.Utilities;

namespace RelaNet.Snapshots.Basic2d
{
    public struct InputPackerBasic2d : ISnapInputPacker<InputBasic2d>
    {
        public int GetWriteLength(InputBasic2d from)
        {
            return 13;
        }

        public int Read(ref InputBasic2d into, Receipt receipt, int c)
        {
            into.Vertical = Bytes.ReadFloat(receipt.Data, c); c += 4;
            into.Horizontal = Bytes.ReadFloat(receipt.Data, c); c += 4;
            into.Rotation = Bytes.ReadFloat(receipt.Data, c); c += 4;
            into.Inputs = receipt.Data[c]; c++;
            return c;
        }

        public void Write(InputBasic2d from, Sent sent)
        {
            sent.WriteFloat(from.Vertical);
            sent.WriteFloat(from.Horizontal);
            sent.WriteFloat(from.Rotation);
            sent.WriteByte(from.Inputs);
        }
    }
}
