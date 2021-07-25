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

        public async Task StartAppAsync(Fox fox, string key, bool debugMode)
        {
            await fox.DoCmdAsync("? 'StartAppAsync()'"); ;
        }

        public void StartRequest(Fox fox, string key, bool debugMode)
        {
            fox.DoCmd("? 'StartRequest()'");
        }

        public async Task StartRequestAsync(Fox fox, string key, bool debugMode)
        {
            await fox.DoCmdAsync("? 'StartRequestAsync()'"); ;
        }
    }
}
