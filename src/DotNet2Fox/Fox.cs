// Run code in FoxPro from .NET
// .NET interface to generic FoxPro COM object
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.CSharp.RuntimeBinder;

namespace DotNet2Fox
{
    public class Fox : IDisposable
    {

        private dynamic foxCOM;
        private dynamic foxRun;
        private Timer foxTimer;
        private string key;
        private IFoxApp foxApp;
        // Seconds of inactivity before foxCOM object is released
        private int foxTimeout;
        private bool debugMode;
        private bool usingPool;
        public string id { get; set; } // unique object id
        private object requestLock = new Object();
        private bool releasingFoxCOM;
        private bool requestRunning;
        private TestCallback callback;
        private int retries = 0;
        private bool retryAfterError;
        public int ProcessId;

        public Fox(string key, IFoxApp foxApp = null, int foxTimeout = 60, bool debugMode = false, bool usingPool = false)
        {
            this.key = key;
            this.foxApp = foxApp;
            this.foxTimeout = foxTimeout;
            this.debugMode = debugMode;
            this.usingPool = usingPool;
            this.id = Guid.NewGuid().ToString();
            Debug.WriteLine("Fox constructed: " + key + " " + id);
            CreateTimer();
        }

        // Start request (called automatically by FoxPool)
        // Initialize FoxRun object before calling other methods
        public void StartRequest(string key)
        {

            // Lock to make sure previous request is completely disposed before starting new request
            lock (requestLock)
            {
                // Reset timer
                foxTimer.Stop();
                requestRunning = true;
                // Reset disposal 
                disposedValue = false;
                Debug.WriteLine("StartRequest(): " + id);
                // Make sure app started for current key
                StartApp(key);
                // Create FoxRun
                InstantiateFoxRun();
                CallFoxAppHook("StartRequest", key);
            }
        }

        // Start application
        // Runs first time or when key changes
        private void StartApp(string key)
        {
            try
            {
                if (foxCOM == null || this.key != key)
                {
                    Debug.WriteLine("StartApp(): " + id);
                    this.key = key;
                    InstantiateFoxCOM();
                    InstantiateFoxRun();
                    CallFoxAppHook("StartApp", key);
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, 1);
                // If COM failure, then retry one time per request
                if (retryAfterError)
                {
                    CheckFoxCOM();
                    StartApp(key);
                }
            }

        }

        // Call app-specific code
        private void CallFoxAppHook(string hookName, string key)
        {
            if (foxApp != null)
            {
                try
                {
                    switch (hookName.ToLower())
                    {
                        case "startapp":
                            foxApp.StartApp(this, key, debugMode);
                            break;
                        case "startrequest":
                            foxApp.StartRequest(this, key, debugMode);
                            break;
                        case "endrequest":
                            foxApp.EndRequest(this, key, debugMode);
                            break;
                        case "endapp":
                            StartRequest(key);
                            foxApp.EndApp(this, key, debugMode);
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // UPDATE: Decided automatically retrying commands is probably a bad idea, 
                    //  so setting retries to zero.
                    HandleError(ex, 0);
                    //HandleError(e, 99);
                    if (retryAfterError)
                    {
                        // If COM failure, then skip hook
                        // Hook will run again if command is retried
                    }
                }
            }

        }

        // Execute single FoxPro command
        public void DoCmd(string command)
        {
            try
            {
                foxRun.DoCmd(command);
            }
            catch (Exception ex)
            {
                // UPDATE: Decided automatically retrying commands is probably a bad idea, 
                //  so setting retries to zero.
                HandleError(ex, 0);
                //HandleError(ex, 1);
                // If COM failure, then retry one time per request
                if (retryAfterError)
                {
                    CheckFoxRun(true);
                    DoCmd(command);
                }
            }
        }

        // Execute single FoxPro command (async)
        public async Task<dynamic> DoCmdAsync(string command)
        {
            dynamic result;
            var tcs = new TaskCompletionSourceWrapper();
            try
            {
                foxRun.DoCmdAsync(tcs, command);
                result = await tcs.Task;
            }
            catch (Exception ex)
            {
                HandleError(ex, 0);
                result = null;
            }
            return result;
        }

        // FoxPro Evaluate() function
        public dynamic Eval(string expression)
        {
            dynamic result;
            try
            {
                result = foxRun.Eval(expression);
            }
            catch (Exception ex)
            {
                // UPDATE: Decided automatically retrying commands is probably a bad idea, 
                //  so setting retries to zero.
                HandleError(ex, 0);
                //HandleError(ex, 1);
                // If COM failure, then retry one time per request
                if (retryAfterError)
                {
                    CheckFoxRun(true);
                    result = Eval(expression);
                }
                else
                {
                    result = null;
                }
            }
            return result;
        }

        // FoxPro Evaluate() function (async)
        public async Task<dynamic> EvalAsync(string expression)
        {
            dynamic result;
            var tcs = new TaskCompletionSourceWrapper();
            try
            {
               foxRun.EvalAsync(tcs, expression);
               result = await tcs.Task;
            }
            catch (Exception ex)
            {
                HandleError(ex, 0);
                result = null;
            }
            return result;
        }

        // Execute/call function
        public dynamic Call(string functionName, params object[] parameters)
        {
            dynamic result;
            try
            {
                result = foxRun.Call(functionName, parameters);
            }
            catch (Exception ex)
            {
                // UPDATE: Decided automatically retrying commands is probably a bad idea, 
                //  so setting retries to zero.
                HandleError(ex, 0);
                //HandleError(ex, 1);
                // If COM failure, then retry one time per request
                if (retryAfterError)
                {
                    CheckFoxRun(true);
                    result = Call(functionName, parameters);
                }
                else
                {
                    result = null;
                }
            }
            return result;
        }

        // Execute/call function (async)
        public async Task<dynamic> CallAsync(string functionName, params object[] parameters)
        {
            dynamic result;
            var tcs = new TaskCompletionSourceWrapper();
            try
            {
                foxRun.CallAsync(tcs, functionName, parameters);
                result = await tcs.Task;
            }
            catch (Exception ex)
            {
                HandleError(ex, 0);
                result = null;
            }
            return result;
        }

        // Instantiate object, call method on it, and release object
        public dynamic CallMethod(string methodName, string className, string module, string inApplication = "", params object[] parameters)
        {
            dynamic result;
            try
            {
                result = foxRun.CallMethod(methodName, className, module, inApplication, parameters);
            }
            catch (Exception ex)
            {
                // UPDATE: Decided automatically retrying commands is probably a bad idea, 
                //  so setting retries to zero.
                HandleError(ex, 0);
                //HandleError(ex, 1);
                // If COM failure, then retry one time per request
                if (retryAfterError)
                {
                    CheckFoxRun(true);
                    result = CallMethod(methodName, className, module, inApplication, parameters);
                }
                else
                {
                    result = null;
                }
            }
            return result;
        }

        // Instantiate FoxPro object using NewObject()
        public dynamic CreateNewObject(string className, string module, string inApplication="", params object[] parameters)
        {
            dynamic result;
            try
            {
                result = foxRun.CreateNewObject(className, module, inApplication, parameters);
            }
            catch (Exception ex)
            {
                // UPDATE: Decided automatically retrying commands is probably a bad idea, 
                //  so setting retries to zero.
                HandleError(ex, 0);
                //HandleError(ex, 1);
                // If COM failure, then retry one time per request
                if (retryAfterError)
                {
                    CheckFoxRun(true);
                    result = CreateNewObject(className, module, inApplication, parameters);
                }
                else
                {
                    result = null;
                }
            }
            return result;
        }

        // Do program. The DO command does not have a return value.  Use Call() instead if a return value is required.
        public void Do(string program, string inProgram = "", params object[] parameters)
        {
            try
            {
                foxRun.Do(program, inProgram, parameters);
            }
            catch (Exception ex)
            {
                // UPDATE: Decided automatically retrying commands is probably a bad idea, 
                //  so setting retries to zero.
                HandleError(ex, 0);
                //HandleError(ex, 1);
                // If COM failure, then retry one time per request
                if (retryAfterError)
                {
                    CheckFoxRun(true);
                    Do(program, inProgram, parameters);
                }
            }

        }

        // Do program. The DO command does not have a return value.  Use Call() instead if a return value is required.
        public async Task DoAsync(string program, string inProgram = "", params object[] parameters)
        {
            var tcs = new TaskCompletionSourceWrapper();
            try
            {
                foxRun.DoAsync(tcs, program, inProgram, parameters);
                // Await (rather than returning Task) allows exception handling here
                await tcs.Task;
            }
            catch (Exception ex)
            {
                HandleError(ex, 0);
            }

        }

        // Execute FoxPro script
        public dynamic ExecScript(string script, params object[] parameters)
        {
            dynamic result;
            try {
                result = foxRun.ExecScript(script, parameters);
            }
            catch (Exception ex)
            {
                // UPDATE: Decided automatically retrying commands is probably a bad idea, 
                //  so setting retries to zero.
                HandleError(ex, 0);
                //HandleError(ex, 1);
                // If COM failure, then retry one time per request
                if (retryAfterError)
                {
                    CheckFoxRun(true);
                    result = ExecScript(script, parameters);
                }
                else
                {
                    result = null;
                }
            }
            return result;
        }

        // Release COM object reference to FoxPro object.
        public void ReleaseComObject(object comObject)
        {
            Marshal.ReleaseComObject(comObject);
        }

        // Setup timer
        private void CreateTimer()
        {
            foxTimer = new Timer();
            foxTimer.AutoReset = false;
            foxTimer.Interval = foxTimeout * 1000;
            foxTimer.Elapsed += OnTimerElapsed;
        }

        // Fires when timer elapses
        private void OnTimerElapsed(Object source, System.Timers.ElapsedEventArgs e)
        {
            // Release FoxPro COM object
            Debug.WriteLine("OnTimerElapsed(): " + id);
            lock (requestLock)
            {
                if (!requestRunning)
                {
                    ReleaseFoxCOM("Timer elapsed");
                }
            }
        }

        // Instantiate FoxPro generic COM object
        private void InstantiateFoxCOM()
        {
            try
            {
                if (foxCOM == null)
                {
                    Debug.WriteLine("InstantiateFoxCOM(): " + id);
                    if (debugMode == true)
                    {
                        dynamic vfp = Activator.CreateInstance(Type.GetTypeFromProgID("VisualFoxPro.Application", true));
                        vfp.Visible = true;
                        string foxCOMPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\FoxCOM\FoxCOM.prg";
                        string cmd = $"NewObject('Application', '{foxCOMPath}')";
                        foxCOM = vfp.Eval(cmd);
                        foxCOM.lQuitOnDestroy = true;
                        Marshal.ReleaseComObject(vfp);
                    }
                    else
                    {
                        foxCOM = Activator.CreateInstance(Type.GetTypeFromProgID("FoxCOM.Application", true));
                        // lQuitOnDestroy was previously only required in debug mode
                        // Errors during async calls could cause "Cannot Quit Visual FoxPro" issue
                        // Setting lQuitOnDestroy prevents that from happening, and doesn't cause other issues
                        foxCOM.lQuitOnDestroy = true;
                        foxCOM.VFP.Visible = true;  // only visible in development or for IIS user
                    }
                    ProcessId = foxCOM.VFP.ProcessId;

                    // Set callback so FoxPro can test if DotNet process is still alive
                    callback = new TestCallback();
                    foxCOM.SetTestCallback(callback, 60);
                }
            }
            catch (Exception ex) 
            {
                HandleError(ex, 3);
                // If COM failure, then retry two times per request
                if (retryAfterError)
                {
                    CheckFoxCOM();
                    InstantiateFoxCOM();
                }
            }
        }

        // Make sure foxCOM object is ok
        // Otherwise, errors will occur if VFP instance crashes or is closed and Fox object thinks it is still open
        // Optionally reinstantiate if there is a problem
        private void CheckFoxCOM(bool reinstantiate = false)
        {
            bool foxCOMOK;
            try
            {
                // Next statement will cause error if FoxPro instance is no longer alive
                if (foxCOM.VFP.Eval("1+1") == 2)
                {
                    foxCOMOK = true;
                }
                else
                {
                    foxCOMOK = false;
                }
            }
            catch (Exception)
            {
                foxCOMOK = false;
            }

            if (!foxCOMOK)
            {
                foxCOM = null;
                if (reinstantiate)
                {
                    //InstantiateFoxCOM();
                    StartApp(this.key);
                }
            }
        }

        // Instantiate FoxRun object
        private void InstantiateFoxRun()
        {
            // FoxRun is object used to run FoxPro/Nexus commands
            // It is instantiated from within FoxCOM and will be released as well.
            try
            {
                if (foxRun == null)
                {
                    Debug.WriteLine("InstantiateFoxRun(): " + id);
                    dynamic _VFP;
                    string foxRunVCX = "foxrun.vcx";
                    if (debugMode == true)
                    {
                        // In debug mode, _VFP is the same as foxCOM
                        _VFP = foxCOM.VFP;
                        foxRunVCX = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\FoxCOM\FoxRun.vcx";
                    }
                    else
                    {
                        // When not in debug mode, _VFP is a property of foxCOM
                        _VFP = foxCOM.VFP;
                    }

                    string cmd = "NewObject('foxrun','" + foxRunVCX + "')";
                    foxRun = _VFP.Eval(cmd);
                    foxRun.lQuitOnDestroy = false;
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, 2);
                // If COM failure, then retry one time per request
                if (retryAfterError)
                {
                    CheckFoxCOM(true);
                    CheckFoxRun();
                    InstantiateFoxRun();
                }
                else
                {
                    throw;
                }
            }
        }

        // Make sure foxCOM object is ok
        // Otherwise, errors will occur if VFP instance crashes or is closed and Fox object thinks it is still open
        // Optionally reinstantiate if there is a problem
        private void CheckFoxRun(bool reinstantiate = false) 
        {
            bool foxRunOK;
            try
            {
                // Next statement will cause error if FoxPro instance is no longer alive
                if (foxRun.Eval("1+1") == 2)
                {
                    foxRunOK = true;
                }
                else
                {
                    foxRunOK = false;
                }
            }
            catch (Exception)
            {
                foxRunOK = false;
            }

            if (!foxRunOK)
            {
                foxRun = null;
                if (reinstantiate)
                {
                    StartRequest(this.key);
                }
            }
        }

        public void HandleError(Exception ex, int retryCount = 0)
        {
            Debug.WriteLine("HandleError(): " + id + ex.ToString());
            retryAfterError = false;
            CheckFoxRun();
            if (ex is NullReferenceException && foxRun != null)
            {
                // Throw FoxPro error
                string error = foxRun.GetErrorMessage();
                Debug.WriteLine("foxRun.GetErrorMessage(): " + id + "\n" + error);
                // Dispose object and don't return to pool
                usingPool = false;
                Dispose();
                throw new COMException(error);
            }
            else if (retries < retryCount && (ex is COMException || ex is MissingMemberException || 
                ex is RuntimeBinderException || ex is NullReferenceException))
            {
                // For COM failures, allow a limited number of retries per request
                retries++;
                retryAfterError = true;
                Debug.WriteLine("HandleError() Retry " + retries.ToString() + ": " + id);
            }
            else
            {
                throw ex;
            }
        }

        private void ReleaseFoxCOM(string reason)
        {
            Debug.WriteLine("ReleaseFoxCOM(): " + key + " " + id + " " + reason);
            foxTimer.Stop();
            //CheckFoxCOM();
            try
            {
                if (!releasingFoxCOM && foxCOM != null)
                {
                    // Prevent re-entry while release is running
                    releasingFoxCOM = true;
                    // Call app-specific EndApp code
                    CallFoxAppHook("EndApp", key);
                    ReleaseFoxRun();
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, 99);
                if (retryAfterError)
                {
                    // If COM failure, just release
                }
            }
            finally
            {
                Debug.WriteLine("Marshal.ReleaseComObject(foxCOM): " + key + " " + id + " " + reason);
                try
                {
                    if (foxCOM != null) // double-check
                    {
                        Marshal.ReleaseComObject(foxCOM);
                        foxCOM = null;
                    }
                }
                catch
                {
                    // Ignore errors when releasing
                }
                releasingFoxCOM = false;
                requestRunning = false;
            }
        }

        private void ReleaseFoxRun()
        {
            Debug.WriteLine("ReleaseFoxRun(): " + id);
            //CheckFoxRun();
            try
            {
                if (foxRun != null)
                {
                    // Call app-specific EndRequest code
                    CallFoxAppHook("EndRequest", key);
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, 99);
                if (retryAfterError)
                {
                    // If COM failure, just release
                }
            }
            finally
            {
                Debug.WriteLine("Marshal.ReleaseComObject(foxRun): " + id);
                retries = 0;   // reset retries count
                try
                {
                    if (foxRun != null) // double-check
                    {
                        Marshal.ReleaseComObject(foxRun);
                        foxRun = null;
                    }

                    if (usingPool)
                    {
                        // If using pool, only allow one thread to cleanup at a time
                        // Marshal.AreComObjectsAvailableForCleanup() can hang if several threads call it at the same time
                        lock (FoxPool.cleanupComLock)
                        {
                            CleanupComObjects();
                        }
                    }
                    else
                    {
                        CleanupComObjects();
                    }
                }
                catch
                {
                    // Ignore errors when releasing
                    Debug.WriteLine("Marshal.ReleaseComObject(foxRun) Error: " + id);
                }
                Debug.WriteLine("ReleaseFoxRun() Complete: " + id);
            }
        }

        private void CleanupComObjects()
        {
            Debug.WriteLine("GC Cleanup: " + id);
            // Release any COM objects created by FoxRun (CreateNewObject(), etc.)
            // See https://stackoverflow.com/questions/37904483/as-of-today-what-is-the-right-way-to-work-with-com-objects
            do
            {
                //Debug.WriteLine("GC Cleanup Loop: " + id);
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            while (Marshal.AreComObjectsAvailableForCleanup());
            Debug.WriteLine("GC Cleanup Complete: " + id);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            Debug.WriteLine(key + ": Dispose : " + disposing.ToString() + "  " + disposedValue.ToString() + " " + "usingPool: " + usingPool.ToString() + " " + id);
            if (!disposedValue)
            {
                disposedValue = true;
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // Release FoxRun after each request
                ReleaseFoxRun();

                if (usingPool)
                {

                    // Lock to make sure finished disposing before starting new request
                    lock (requestLock)
                    {
                        // Put Fox object back in pool
                        var added = FoxPool.AddObject(key, this);
                        if (added)
                        {
                            //Start timer to release Fox COM
                            requestRunning = false;
                            foxTimer.Start();
                        }
                        else
                        {
                            // Object not in pool so release it
                            FoxPool.instCount--;
                            foxTimer.Stop();
                            usingPool = false;
                            ReleaseFoxCOM("Not added to pool");
                        }
                    }
                }
                else
                {
                    ReleaseFoxCOM("Not using pool");
                }
            }
            else
            {
                ReleaseFoxCOM("Previously disposed: " + disposing.ToString() + "  " + disposedValue.ToString());
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // Not sure about this: No need to finalize if everything disposed and object not added back to pool
            //if (foxCOM == null)
            //{
            //    GC.SuppressFinalize(this);
            //}
        }
        #endregion

        // Finalizer in case Dispose not called by user
        ~Fox()
        {
            Debug.WriteLine(key + ": Fox destructor " + id);
            usingPool = false;
            Dispose(false);
        }


    }
}
