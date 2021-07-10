using DotNet2Fox;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNet2Fox.Tests
{
    class FoxNetTestApp : IFoxApp
    {
        public void EndApp(FoxNet foxNet, string key, bool debugMode)
        {
            foxNet.DoCmd("? 'EndApp()'");
        }

        public void EndRequest(FoxNet foxNet, string key, bool debugMode)
        {
            foxNet.DoCmd("? 'EndRequest()'");
        }

        public void StartApp(FoxNet foxNet, string key, bool debugMode)
        {
            foxNet.DoCmd("? 'StartApp()'");
        }

        public void StartRequest(FoxNet foxNet, string key, bool debugMode)
        {
            foxNet.DoCmd("? 'StartRequest()'");
        }
    }
}
