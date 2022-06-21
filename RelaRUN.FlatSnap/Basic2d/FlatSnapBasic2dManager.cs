using System;
using System.Collections.Generic;
using System.Text;

namespace RelaRUN.FlatSnap.Basic2d
{
    public class FlatSnapBasic2dManager
    {
        public NetExecutorFlatSnap Executor;
        public FlatSnapBasic2dSimulator Simulator;
        public FlatSnapInputManager InputManager;
        
        public FlatSnapBasic2dManager()
        {
            // horiz vel, vert vel, mouse x, mouse y
            // buttons bool
            InputManager = new FlatSnapInputManager(64, 4, 4, 1);

            Simulator = new FlatSnapBasic2dSimulator();

            // x, y, rot, xvel, yvel
            // bool flag, owner id
            // entity type
            // health

            // nonnet
            // health timer
            // 
            // 
            //
            Executor = new NetExecutorFlatSnap(Simulator, 64, 10000, 
                5, 2, 1, 1,
                1, 0, 0, 0,
                InputManager);
        }
    }
}
