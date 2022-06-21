using RelaRUN.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RelaRUN.FlatSnap.Basic2d
{
    public class FlatSnapBasic2dSimulator : IFlatSnapSimulator
    {
        public void Simulate(FlatSnapData data, float elapsedTime, int highestEnt, EntityInfo ents, FlatSnapInput[] inputs, FlatSnapInput[] prevInputs)
        {
            for (int i = 0; i < highestEnt; i++)
            {
                if (!ents.InUse[i] || ents.Spawn[i] > data.Time || ents.Death[i] < data.Time)
                    continue;

                int fi = data.FloatsPer * i; // x, y, rot, xvel, yvel
                int ui = data.UShortsPer * i; // type
                int bi = data.BytesPer * i; // bool flag, owner id
                int ii = data.IntsPer * i; // health

                int nnfi = data.NonNetFloatsPer * i; // health regen

                byte ownerid = data.Bytes[bi + 1];
                if (ownerid != 0 && ownerid < inputs.Length)
                {
                    FlatSnapInput input = inputs[ownerid];
                    // horiz vel, vert vel, mouse x, mouse y

                    // update velocities
                    data.Floats[fi + 3] = input.Floats[0];
                    data.Floats[fi + 4] = input.Floats[1];

                    // update rotation
                    data.Floats[fi + 2] = RMathF.Atan2(input.Floats[3], input.Floats[2]);
                }

                // update position based on velocities
                data.Floats[fi] += data.Floats[fi + 3] * elapsedTime;
                data.Floats[fi + 1] += data.Floats[fi + 4] * elapsedTime;

                // regain health slowly
                int health = data.Ints[ii];
                if (health < 100)
                {
                    // increment the health regen timer
                    data.NonNetFloats[nnfi] += elapsedTime;
                    if (data.NonNetFloats[nnfi] > 500f)
                    {
                        // regain 1 health if the timer exceeds 500ms
                        data.NonNetFloats[nnfi] = 0;
                        data.Ints[ii] = health + 1;
                    }
                }
            }
        }

        public void ClientSpawn(FlatSnapData data, int id)
        {
            data.NonNetFloats[id * data.NonNetFloatsPer] = 0;
        }

        public void ClientDeath(FlatSnapData data, int id)
        {

        }

        public void PropagateNonNet(FlatSnapData data, float elapsedTime, int entityId, int highestEntityId, EntityInfo entities, FlatSnapInput[] inputs, FlatSnapInput[] prevInputs)
        {
            throw new NotImplementedException();
        }
    }
}
