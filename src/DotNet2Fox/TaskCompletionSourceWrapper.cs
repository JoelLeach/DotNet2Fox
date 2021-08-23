// Could not make TaskCompletionSource ComVisible, so wrapped with this class
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DotNet2Fox
{
    /// <summary>
    /// Wrapper for TaskCompletionSource.
    /// Necesssary because generic in TaskCompletionSource does not work with ComVisible().
    /// </summary>
    [ComVisible(true)]
    public class TaskCompletionSourceWrapper
    {
        private readonly TaskCompletionSource<dynamic> tcs = new TaskCompletionSource<dynamic>();
        /// <summary>
        /// Reference to wrapped TaskCompletionSource.
        /// </summary>
        public TaskCompletionSource<dynamic> Tcs => tcs;
        /// <summary>
        /// Reference to wrapped TaskCompletionSource.Task.
        /// </summary>
        public Task<dynamic> Task => tcs.Task;

        /// <summary>
        /// Set TaskCompletionSource result.
        /// </summary>
        /// <param name="result"></param>
        public void SetResult(dynamic result)
        {
            Tcs.SetResult(result);
        }

        /// <summary>
        /// Set TaskCompletionSource error.
        /// </summary>
        public void SetException()
        {
            // HandleError() will recognize NullReferenceException as a FoxPro error
            Tcs.SetException(new NullReferenceException("ASync FoxPro Error"));
        }
    }
}
