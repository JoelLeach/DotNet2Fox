using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace MBS.FoxPro
{
    [ComVisible(true)]
    public class TestCallback
    {
        // Called on a timer from FoxPro to make sure DotNet is still alive
        // If not, then FoxPro instances will quit
        public bool TestDotNet()
        {
            return true;
        }
    }
}
