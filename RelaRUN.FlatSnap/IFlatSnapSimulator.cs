using System;
using System.Collections.Generic;
using System.Text;

namespace RelaRUN.FlatSnap
{
    public interface IFlatSnapSimulator
    {
        void Simulate(FlatSnapData data, float elapsedTime, int highestEntityId, EntityInfo entities, FlatSnapInput[] inputs, FlatSnapInput[] prevInputs);
        void PropagateNonNet(FlatSnapData data, float elapsedTime, int entityId, int highestEntityId, EntityInfo entities, FlatSnapInput[] inputs, FlatSnapInput[] prevInputs);

        void ClientSpawn(FlatSnapData data, int id);
        void ClientDeath(FlatSnapData data, int id);
    }
}
