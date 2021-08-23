using System.Runtime.InteropServices;

namespace DotNet2Fox
{
    /// <summary>
    /// Called from FoxPro to make sure DotNet is still alive.
    /// </summary>
    [ComVisible(true)]
    public class TestCallback
    {

        /// <summary>
        /// Called on a timer from FoxPro to make sure DotNet is still alive
        /// If not, then FoxPro instances will quit
        /// </summary>
        /// <returns>Returns true if call is successful.</returns>
        public bool TestDotNet()
        {
            return true;
        }
    }
}
