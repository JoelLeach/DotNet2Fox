// Fox interface for application-specific code
using System.Threading.Tasks;

namespace DotNet2Fox
{

    /// <summary>
    /// Hooks for application-specific code during startup/shutdown
    /// </summary>
    public interface IFoxApp
    {
        /// <summary>
        /// Called when Fox application starts up.
        /// </summary>
        /// <param name="fox">Current Fox instance.</param>
        /// <param name="key">Key used to differentiate Fox object within pool.</param>
        /// <param name="debugMode">When true, commands are executed within Visual FoxPro IDE capable of debugging.</param>
        void StartApp(Fox fox, string key, bool debugMode);
        /// <summary>
        /// Called when Fox application starts up (async).
        /// </summary>
        /// <param name="fox">Current Fox instance.</param>
        /// <param name="key">Key used to differentiate Fox object within pool.</param>
        /// <param name="debugMode">When true, commands are executed within Visual FoxPro IDE capable of debugging.</param>
        Task StartAppAsync(Fox fox, string key, bool debugMode);

        /// <summary>
        /// Called before each request.
        /// </summary>
        /// <param name="fox">Current Fox instance.</param>
        /// <param name="key">Key used to differentiate Fox object within pool.</param>
        /// <param name="debugMode">When true, commands are executed within Visual FoxPro IDE capable of debugging.</param>
        void StartRequest(Fox fox, string key, bool debugMode);
        /// <summary>
        /// Called before each request (async).
        /// </summary>
        /// <param name="fox">Current Fox instance.</param>
        /// <param name="key">Key used to differentiate Fox object within pool.</param>
        /// <param name="debugMode">When true, commands are executed within Visual FoxPro IDE capable of debugging.</param>
        Task StartRequestAsync(Fox fox, string key, bool debugMode);

        // Async versions are not included for End* hooks, because .NET 4.x does not have IAsyncDisposable interface
        /// <summary>
        /// Called after each request when Fox object is disposed.
        /// </summary>
        void EndRequest(Fox fox, string key, bool debugMode);

        /// <summary>
        /// Called when Fox app is shutting down.
        /// </summary>
        /// <param name="fox">Current Fox instance.</param>
        /// <param name="key">Key used to differentiate Fox object within pool.</param>
        /// <param name="debugMode">When true, commands are executed within Visual FoxPro IDE capable of debugging.</param>
        void EndApp(Fox fox, string key, bool debugMode);
    }
}
