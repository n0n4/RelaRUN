using System;
using System.Collections.Generic;
using System.Text;

namespace RelaNet
{
    public class NetLogger
    {
        public bool On = false;

        public Action<string> LogCallback;
        
        public NetLogger(Action<string> logCallback)
        {
            LogCallback = logCallback;
        }

        public void Log(string s)
        {
            LogCallback(s);
        }

        public void Error(string s, Exception e)
        {
            LogCallback(s);
            LogCallback(e.Message);
            LogCallback(e.StackTrace);
        }
    }
}
