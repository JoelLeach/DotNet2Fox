// Fox interface for application-specific code
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
        void StartApp(Fox fox, string key, bool debugMode);
        Task StartAppAsync(Fox fox, string key, bool debugMode);

        // Called before each request
        void StartRequest(Fox fox, string key, bool debugMode);
        Task StartRequestAsync(Fox fox, string key, bool debugMode);

        // Async versions are not included for End* hooks, because .NET 4.x does not have IAsyncDisposable interface
        // Called after request when Fox object is disposed
        void EndRequest(Fox fox, string key, bool debugMode);

        // Called when Fox app is shutting down
        void EndApp(Fox fox, string key, bool debugMode);
    }
}
