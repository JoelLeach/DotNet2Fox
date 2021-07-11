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
    class FoxTestApp : IFoxApp
    {
        public void EndApp(Fox fox, string key, bool debugMode)
        {
            fox.DoCmd("? 'EndApp()'");
        }

        public void EndRequest(Fox fox, string key, bool debugMode)
        {
            fox.DoCmd("? 'EndRequest()'");
        }

        public void StartApp(Fox fox, string key, bool debugMode)
        {
            fox.DoCmd("? 'StartApp()'");
        }

        public void StartRequest(Fox fox, string key, bool debugMode)
        {
            fox.DoCmd("? 'StartRequest()'");
        }
    }
}
