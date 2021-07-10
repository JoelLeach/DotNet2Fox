// FoxNet interface for application-specific code
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNet2Fox
{

    public interface IFoxApp
    {
        // Called when Fox application starts up
        void StartApp(FoxNet foxNet, string key, bool debugMode);

        // Called before each request
        void StartRequest(FoxNet foxNet, string key, bool debugMode);

        // Called after request when FoxNet is disposed
        void EndRequest(FoxNet foxNet, string key, bool debugMode);

        // Called when Fox app is shutting down
        void EndApp(FoxNet foxNet, string key, bool debugMode);
    }
}
