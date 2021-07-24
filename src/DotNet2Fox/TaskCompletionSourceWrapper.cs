// Could not make TaskCompletionSource ComVisible, so wrapped with this class
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DotNet2Fox
{
    [ComVisible(true)]
    public class TaskCompletionSourceWrapper
    {
        private readonly TaskCompletionSource<dynamic> tcs = new TaskCompletionSource<dynamic>();
        public TaskCompletionSource<dynamic> Tcs => tcs;
        public Task<dynamic> Task => tcs.Task;

        public void SetResult(dynamic result)
        {
            Tcs.SetResult(result);
        } 
        
        public void SetException()
        {
            // HandleError() will recognize NullReferenceException as a FoxPro error
            Tcs.SetException(new NullReferenceException("ASync FoxPro Error"));
        }
    }
}
