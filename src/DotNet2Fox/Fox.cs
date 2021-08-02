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
    /// <summary>
    /// Execute Visual FoxPro code from .NET.
    /// </summary>
    public class Fox : IDisposable
    {
        /// <summary>
        /// FoxCOM object. Simple COM interface that is registered with FoxCOM.exe.
        /// </summary>
        private dynamic foxCOM;
        /// <summary>
        /// FoxRun object used to execute code inside VFP.
        /// </summary>
        private dynamic foxRun;
        /// <summary>
        /// Timer that releases object from pool after period of inactivity.
        /// </summary>
        private Timer foxTimer;
        /// <summary>
        /// Key used to differentiate Fox object within pool.
        /// </summary>
        private string key;
        /// <summary>
        /// FoxApp object with Start/End hooks containing application specific code.
        /// </summary>
        private IFoxApp foxApp;
        /// <summary>
        /// Seconds of inactivity before foxCOM object is released.
        /// </summary>
        private int foxTimeout;
        /// <summary>
        /// When true, commands are executed within Visual FoxPro IDE capable of debugging.
        /// </summary>
        private bool debugMode;
        /// <summary>
        /// When true, Fox object is used within pool.
        /// </summary>
        private bool usingPool;
        /// <summary>
        /// Lock to make sure previous request is completely disposed before starting new request.
        /// </summary>
        private readonly object requestLock = new object();
        /// <summary>
        /// Is true while FoxCOM is being released. Prevents re-entry while release is running.
        /// </summary>
        private bool releasingFoxCOM;
        /// <summary>
        /// Is true while request is running.
        /// </summary>
        private bool requestRunning;
        /// <summary>
        /// Called on a timer from FoxPro to make sure DotNet is still alive.
        /// If not, then FoxPro instances will quit.
        /// </summary>
        private TestCallback callback;
        /// <summary>
        /// Number of times an operation has been retried after an error.
        /// </summary>
        private int retries = 0;
        /// <summary>
        /// If set to true in HandleError(), operation may be retried.
        /// </summary>
        private bool retryAfterError;

        /// <summary>
        /// Unique Fox object ID. Used for DotNet2Fox internal debugging.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Process ID of VFP/COM instance.
        /// </summary>
        public int ProcessId;

        /// <summary>
        /// Execute FoxPro code from .NET.
        /// </summary>
        /// <param name="key">Key used to differentiate Fox object within pool.</param>
        /// <param name="foxApp">FoxApp object with Start/End hooks containing application specific code.</param>
        /// <param name="foxTimeout">Seconds of inactivity before foxCOM object is released.</param>
        /// <param name="debugMode">When true, commands are executed within Visual FoxPro IDE capable of debugging.</param>
        /// <param name="usingPool">When true, Fox object is used within pool.</param>
        public Fox(string key, IFoxApp foxApp = null, int foxTimeout = 60, bool debugMode = false, bool usingPool = false)
        {
            this.key = key;
            this.foxApp = foxApp;
            this.foxTimeout = foxTimeout;
            this.debugMode = debugMode;
            this.usingPool = usingPool;
            Id = Guid.NewGuid().ToString();
            Debug.WriteLine("Fox constructed: " + key + " " + Id);
            CreateTimer();
        }

        /// <summary>
        /// Start request and call FoxApp startup hooks (called automatically by FoxPool.GetObject()).
        /// </summary>
        /// <param name="key">Key used to differentiate Fox object within pool.</param>
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
                Debug.WriteLine("StartRequest(): " + Id);
                // Make sure app started for current key
                StartApp(key);
                // Create FoxRun
                InstantiateFoxRun();
                CallFoxAppHook("StartRequest", key);
            }
        }

        /// <summary>
        /// Start request and call FoxApp async startup hooks (called automatically by FoxPool.GetObjectAsync()).
        /// </summary>
        /// <param name="key">Key used to differentiate Fox object within pool.</param>
        /// <returns></returns>
        // Same as StartRequest() except it calls StartAppAsync() and async hooks
        public async Task StartRequestAsync(string key)
        {
            // Lock to make sure previous request is completely disposed before starting new request
            lock (requestLock)
            {
                // Reset timer
                foxTimer.Stop();
                requestRunning = true;
                // Reset disposal 
                disposedValue = false;
                Debug.WriteLine("StartRequest(): " + Id);
            }
            // await cannot be inside lock, so moved the rest of the code outside
            // Make sure app started for current key
            await StartAppAsync(key);
            // Create FoxRun
            InstantiateFoxRun();
            await CallFoxAppHookAsync("StartRequest", key);
        }

        /// <summary>
        /// Start application and call StartApp hook. 
        /// Runs first time or when key changes.
        /// </summary>
        /// <param name="key">Key used to differentiate Fox object within pool.</param>
        private void StartApp(string key)
        {
            try
            {
                if (foxCOM == null || this.key != key)
                {
                    Debug.WriteLine("StartApp(): " + Id);
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

        /// <summary>
        /// Start application and call StartAppAsync hook. 
        /// </summary>
        /// <param name="key">Key used to differentiate Fox object within pool.</param>
        /// <returns></returns>
        // Same as StartApp() but calls async hook
        private async Task StartAppAsync(string key)
        {
            try
            {
                if (foxCOM == null || this.key != key)
                {
                    Debug.WriteLine("StartApp(): " + Id);
                    this.key = key;
                    InstantiateFoxCOM();
                    InstantiateFoxRun();
                    await CallFoxAppHookAsync("StartApp", key);
                }
            }
            catch (Exception ex)
            {
                HandleError(ex, 1);
                // If COM failure, then retry one time per request
                if (retryAfterError)
                {
                    CheckFoxCOM();
                    await StartAppAsync(key);
                }
            }
        }


        /// <summary>
        /// Call app-specific code in FoxApp object.
        /// </summary>
        /// <param name="hookName">Name of hook to call.</param>
        /// <param name="key">Key used to differentiate Fox object within pool.</param>
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
                            // Start request to ensure this hook can be run
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

        /// <summary>
        /// Call app-specific code in FoxApp object (async).
        /// Only Start* hooks can be called asynchronously.
        /// </summary>
        /// <param name="hookName">Name of hook to call.</param>
        /// <param name="key">Key used to differentiate Fox object within pool.</param>
        private async Task CallFoxAppHookAsync(string hookName, string key)
        {
            if (foxApp != null)
            {
                try
                {
                    switch (hookName.ToLower())
                    {
                        case "startapp":
                            await foxApp.StartAppAsync(this, key, debugMode);
                            break;
                        case "startrequest":
                            await foxApp.StartRequestAsync(this, key, debugMode);
                            break;
                        // Async versions are not included for End* hooks, because .NET 4.x does not have IAsyncDisposable interface
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

        /// <summary>
        /// Execute single Visual FoxPro command.
        /// </summary>
        /// <param name="command">Specifies the Visual FoxPro command to execute.</param>
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

        /// <summary>
        /// Execute single Visual FoxPro command asynchronously.
        /// </summary>
        /// <param name="command">Specifies the Visual FoxPro command to execute.</param>
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

        /// <summary>
        /// Evaluates a Visual FoxPro character expression and returns the result.
        /// </summary>
        /// <param name="expression">Specifies the expression to evaluate.</param>
        /// <returns>Result of expression.</returns>
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

        /// <summary>
        /// Evaluates a Visual FoxPro character expression asynchronously and returns the result.
        /// </summary>
        /// <param name="expression">Specifies the expression to evaluate.</param>
        /// <returns>Result of expression.</returns>
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

        /// <summary>
        /// Calls a Visual FoxPro function and returns the result.
        /// </summary>
        /// <param name="functionName">Name of function to execute. Can be a user-defined function, VFP built-in function, or method on existing public object.</param>
        /// <param name="parameters">Optional. Specify parameters passed to function.</param>
        /// <returns>Result of function.</returns>
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

        /// <summary>
        /// Calls a Visual FoxPro function asynchronously and returns the result.
        /// </summary>
        /// <param name="functionName">Name of function to execute. Can be a user-defined function, VFP built-in function, or method on existing public object.</param>
        /// <param name="parameters">Optional. Specify parameters passed to function.</param>
        /// <returns>Result of function.</returns>
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

        /// <summary>
        /// Instantiate object, call method on it, then release object.
        /// </summary>
        /// <param name="methodName">Name of the method to execute.</param>
        /// <param name="className">Specifies the class or object from which the new class or object is created.</param>
        /// <param name="module">Specifies a .vcx file or Visual FoxPro program containing the class or object specified with className.</param>
        /// <param name="inApplication">Specifies the Visual FoxPro application (.exe or .app) containing the .vcx file you specify with cModule.</param>
        /// <param name="parameters">Optional. Specifies optional parameters that are passed to the method specified with methodName.</param>
        /// <returns>Result of method.</returns>
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

        /// <summary>
        /// Instantiate object, call method on it asynchronously, then release object.
        /// </summary>
        /// <param name="methodName">Name of the method to execute.</param>
        /// <param name="className">Specifies the class or object from which the new class or object is created.</param>
        /// <param name="module">Specifies a .vcx file or Visual FoxPro program containing the class or object specified with className.</param>
        /// <param name="inApplication">Specifies the Visual FoxPro application (.exe or .app) containing the .vcx file you specify with cModule.</param>
        /// <param name="parameters">Optional. Specifies optional parameters that are passed to the method specified with methodName.</param>
        /// <returns>Result of method.</returns>
        public async Task<dynamic> CallMethodAsync(string methodName, string className, string module, string inApplication = "", params object[] parameters)
        {
            dynamic result;
            var tcs = new TaskCompletionSourceWrapper();
            try
            {
                foxRun.CallMethodAsync(tcs, methodName, className, module, inApplication, parameters);
                result = await tcs.Task;
            }
            catch (Exception ex)
            {
                HandleError(ex, 0);
                result = null;
            }
            return result;
        }

        /// <summary>
        /// Instantiate Visual FoxPro object using NewObject().
        /// </summary>
        /// <param name="className">Specifies the class or object from which the new class or object is created.</param>
        /// <param name="module">Specifies a .vcx file or Visual FoxPro program containing the class or object specified with className.</param>
        /// <param name="inApplication">Specifies the Visual FoxPro application (.exe or .app) containing the .vcx file you specify with cModule.</param>
        /// <param name="parameters">Optional. Specifies optional parameters that are passed to the Init event procedure for the class or object.</param>
        /// <returns>Visual FoxPro object.</returns>
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

        /// <summary>
        /// Instantiate Visual FoxPro object asynchronously using NewObject().
        /// </summary>
        /// <param name="className">Specifies the class or object from which the new class or object is created.</param>
        /// <param name="module">Specifies a .vcx file or Visual FoxPro program containing the class or object specified with className.</param>
        /// <param name="inApplication">Specifies the Visual FoxPro application (.exe or .app) containing the .vcx file you specify with cModule.</param>
        /// <param name="parameters">Optional. Specifies optional parameters that are passed to the Init event procedure for the class or object.</param>
        /// <returns>Visual FoxPro object.</returns>
        public async Task<dynamic> CreateNewObjectAsync(string className, string module, string inApplication = "", params object[] parameters)
        {
            dynamic result;
            var tcs = new TaskCompletionSourceWrapper();
            try
            {
                foxRun.CreateNewObjectAsync(tcs, className, module, inApplication, parameters);
                result = await tcs.Task;
            }
            catch (Exception ex)
            {
                HandleError(ex, 0);
                result = null;
            }
            return result;
        }


        /// <summary>
        /// Executes a Visual FoxPro program or procedure. 
        /// The DO command does not return a value.  Use Call() instead if a return value is required.
        /// </summary>
        /// <param name="program">Specifies the name of the program to execute.</param>
        /// <param name="inProgram">Optional. Executes a procedure in the program file specified with ProgramName2.</param>
        /// <param name="parameters">Optional. Specifies parameters to pass to the program or procedure.</param>
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

        /// <summary>
        /// Executes a Visual FoxPro program or procedure asynchronously. 
        /// The DO command does not return a value.  Use Call() instead if a return value is required.
        /// </summary>
        /// <param name="program">Specifies the name of the program to execute.</param>
        /// <param name="inProgram">Optional. Executes a procedure in the program file specified with ProgramName2.</param>
        /// <param name="parameters">Optional. Specifies parameters to pass to the program or procedure.</param>

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

        /// <summary>
        /// Enables you to run multiple lines of code from variables, tables, and other text at runtime.
        /// </summary>
        /// <param name="script">Represents the text, a variable, type string, or memo to be executed as code.</param>
        /// <param name="parameters">Optional. Specify parameters passed to a script that has a parameter statement in first line.</param>
        /// <returns>Value returned by the script.</returns>
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

        /// <summary>
        /// Enables you to asynchronously run multiple lines of code from variables, tables, and other text at runtime.
        /// </summary>
        /// <param name="script">Represents the text, a variable, type string, or memo to be executed as code.</param>
        /// <param name="parameters">Optional. Specify parameters passed to a script that has a parameter statement in first line.</param>
        /// <returns>Value returned by the script.</returns>
        public async Task<dynamic> ExecScriptAsync(string script, params object[] parameters)
        {
            dynamic result;
            var tcs = new TaskCompletionSourceWrapper();
            try
            {
                foxRun.ExecScriptAsync(tcs, script, parameters);
                result = await tcs.Task;
            }
            catch (Exception ex)
            {
                HandleError(ex, 0);
                result = null;
            }
            return result;
        }

        /// <summary>
        /// Call method on existing FoxPro object.
        /// Usually no need to use directly, but could be useful calling from extension methods.
        /// </summary>
        /// <param name="foxObject">Existing FoxPro object containing method to call.</param>
        /// <param name="methodName">Name of method to execute.</param>
        /// <param name="parameters">Optional. Specifies optional parameters that are passed to the method specified with methodName.</param>
        /// <returns>Result of method.</returns>
        public dynamic CallObjectMethod(dynamic foxObject, string methodName, params object[] parameters)
        {
            dynamic result;
            try
            {
                result = foxRun.CallObjectMethod(foxObject, methodName, parameters);
            }
            catch (Exception ex)
            {
                HandleError(ex, 0);
                result = null;
            }
            return result;
        }

        /// <summary>
        /// Call method on existing FoxPro object asynchronously.
        /// </summary>
        /// <param name="foxObject">Existing FoxPro object containing method to call.</param>
        /// <param name="methodName">Name of method to execute.</param>
        /// <param name="parameters">Optional. Specifies optional parameters that are passed to the method specified with methodName.</param>
        /// <returns>Result of method.</returns>
        public async Task<dynamic> CallObjectMethodAsync(dynamic foxObject, string methodName, params object[] parameters)
        {
            dynamic result;
            var tcs = new TaskCompletionSourceWrapper();
            try
            {
                foxRun.CallObjectMethodAsync(tcs, foxObject, methodName, parameters);
                result = await tcs.Task;
            }
            catch (Exception ex)
            {
                HandleError(ex, 0);
                result = null;
            }
            return result;
        }

        /// <summary>
        /// Set Step On before the next call using Fox object.
        /// Only applicable in debugMode.
        /// </summary>
        public void SetStepOn()
        {
            if (debugMode)
            {
                foxRun.SetStepOn();
            }
        }

        /// <summary>
        /// Set breakpoint in specified file/location.
        /// Only applicable in debug mode.
        /// </summary>
        /// <param name="fileName">File to set breakpoint in.</param>
        /// <param name="location">Breakpoint location. Use same format as VFP Breakpoints dialog.</param>
        public void SetBreakpoint(string fileName, string location)
        {
            if (debugMode)
            {
                foxRun.SetBreakpoint(fileName, location);
            }
        }

        // Release COM object reference to FoxPro object.
        public void ReleaseComObject(object comObject)
        {
            Marshal.ReleaseComObject(comObject);
        }

        /// <summary>
        /// Create timer that releases object from pool after period of inactivity.
        /// </summary>
        private void CreateTimer()
        {
            foxTimer = new Timer();
            foxTimer.AutoReset = false;
            foxTimer.Interval = foxTimeout * 1000;
            foxTimer.Elapsed += OnTimerElapsed;
        }

        /// <summary>
        /// Fires when foxTimer elapses. Releases FoxPro COM object.
        /// </summary>
        /// <param name="source">Event source.</param>
        /// <param name="e">Event arguments.</param>
        private void OnTimerElapsed(Object source, System.Timers.ElapsedEventArgs e)
        {
            // Release FoxPro COM object
            Debug.WriteLine("OnTimerElapsed(): " + Id);
            lock (requestLock)
            {
                if (!requestRunning)
                {
                    ReleaseFoxCOM("Timer elapsed");
                }
            }
        }

        /// <summary>
        /// Instantiate FoxPro generic COM object 
        /// </summary>
        private void InstantiateFoxCOM()
        {
            try
            {
                if (foxCOM == null)
                {
                    Debug.WriteLine("InstantiateFoxCOM(): " + Id);
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

        /// <summary>
        /// Make sure FoxCOM object is ok.
        /// Otherwise, errors will occur if VFP instance crashes or is closed and Fox object thinks it is still open.
        /// </summary>
        /// <param name="reinstantiate">Optional. Reinstantiate FoxCOM if there is a problem.</param>
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

        /// <summary>
        /// Instantiate FoxRun object 
        /// </summary>
        private void InstantiateFoxRun()
        {
            // FoxRun is object used to run FoxPro/Nexus commands
            // It is instantiated from within FoxCOM and will be released as well.
            try
            {
                if (foxRun == null)
                {
                    Debug.WriteLine("InstantiateFoxRun(): " + Id);
                    dynamic _VFP;
                    string foxRunVCX = "foxrun.vcx";
                    string foxCOMEXE = "";
                    if (debugMode == true)
                    {
                        // In debug mode, _VFP is the same as foxCOM
                        _VFP = foxCOM.VFP;
                        foxRunVCX = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\FoxCOM\FoxRun.vcx";
                        // Source files are only deployed for Debug builds.
                        // For Release builds, FoxCOM.exe will be deployed and can be used in DotNet2Fox debug mode.
                        // Useful for running unit tests with Release builds.
                        if (!File.Exists(foxRunVCX))
                        {
                            foxCOMEXE = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\FoxCOM\FoxCOM.exe";
                        }
                    }
                    else
                    {
                        // When not in debug mode, _VFP is a property of foxCOM
                        _VFP = foxCOM.VFP;
                    }

                    string cmd = $"NewObject('foxrun','{foxRunVCX}', '{foxCOMEXE}')";
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

        /// <summary>
        /// Make sure FoxRun object is ok.
        /// Otherwise, errors will occur if VFP instance crashes or is closed and Fox object thinks it is still open.
        /// </summary>
        /// <param name="reinstantiate">Optional. Reinstantiate FoxRun if there is a problem</param>
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

        /// <summary>
        /// Handle error that raised while executing FoxPro code.
        /// </summary>
        /// <param name="ex">Exception raised from FoxPro.</param>
        /// <param name="retryCount">Optional. Number of times to retry operation that caused error. Sets retryAfterError to true if operation can be retried.</param>
        public void HandleError(Exception ex, int retryCount = 0)
        {
            Debug.WriteLine("HandleError(): " + Id + ex.ToString());
            retryAfterError = false;
            CheckFoxRun();
            if (ex is NullReferenceException && foxRun != null)
            {
                // Throw FoxPro error
                string error = foxRun.GetErrorMessage();
                Debug.WriteLine("foxRun.GetErrorMessage(): " + Id + "\n" + error);
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
                Debug.WriteLine("HandleError() Retry " + retries.ToString() + ": " + Id);
            }
            else
            {
                throw ex;
            }
        }

        /// <summary>
        /// Released FoxCOM object.
        /// </summary>
        /// <param name="reason">Reason object is being released. Used for DotNet2Fox internal debugging.</param>
        private void ReleaseFoxCOM(string reason)
        {
            Debug.WriteLine("ReleaseFoxCOM(): " + key + " " + Id + " " + reason);
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
                Debug.WriteLine("Marshal.ReleaseComObject(foxCOM): " + key + " " + Id + " " + reason);
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

        /// <summary>
        /// Released FoxRun object.
        /// </summary>
        private void ReleaseFoxRun()
        {
            Debug.WriteLine("ReleaseFoxRun(): " + Id);
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
                Debug.WriteLine("Marshal.ReleaseComObject(foxRun): " + Id);
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
                    Debug.WriteLine("Marshal.ReleaseComObject(foxRun) Error: " + Id);
                }
                Debug.WriteLine("ReleaseFoxRun() Complete: " + Id);
            }
        }

        /// <summary>
        /// Release any Visual FoxPro COM object references.
        /// </summary>
        private void CleanupComObjects()
        {
            Debug.WriteLine("GC Cleanup: " + Id);
            // Release any COM objects created by FoxRun (CreateNewObject(), etc.)
            // See https://stackoverflow.com/questions/37904483/as-of-today-what-is-the-right-way-to-work-with-com-objects
            do
            {
                //Debug.WriteLine("GC Cleanup Loop: " + id);
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            while (Marshal.AreComObjectsAvailableForCleanup());
            Debug.WriteLine("GC Cleanup Complete: " + Id);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Release Fox object or return to pool.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            Debug.WriteLine(key + ": Dispose : " + disposing.ToString() + "  " + disposedValue.ToString() + " " + "usingPool: " + usingPool.ToString() + " " + Id);
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
                            FoxPool.instanceCount--;
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
            Debug.WriteLine(key + ": Fox destructor " + Id);
            usingPool = false;
            Dispose(false);
        }


    }
}
