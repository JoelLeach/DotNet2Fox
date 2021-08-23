// Pool for Fox objects
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace DotNet2Fox
{
    /// <summary>
    ///  DotNet2Fox object pool.
    /// </summary>
    public class FoxPool
    {
        /// <summary>
        /// Pool of Fox objects.
        /// </summary>
        private static ConcurrentDictionary<string, Fox> pool;
        /// <summary>
        /// Enable/disable pool.
        /// </summary>
        private static volatile bool poolEnabled = true;
        /// <summary>
        /// Number of Fox object instances. Includes instances executing code or waiting in pool.
        /// </summary>
        public static volatile int instanceCount = 0;
        /// <summary>
        /// Number of times existing Fox object was taken from pool. For DotNet2Fox internal debugging only.
        /// </summary>
        public static volatile int poolCount = 0;
        /// <summary>
        /// Lock used to allow one only thread to cleanup COM objects at a time.
        /// </summary>
        public static object cleanupComLock = new object();

        /// <summary>
        /// Maximum number of Fox objects that can be instantiated and placed in pool.
        /// </summary>
        public static int PoolSize { get; set; }
        /// <summary>
        /// When true, commands are executed within Visual FoxPro IDE capable of debugging.
        /// </summary>
        public static bool DebugMode { get; set; }
        /// <summary>
        /// Seconds before inactive Fox object is released from pool.
        /// </summary>
        public static int FoxTimeout { get; set; }
        /// <summary>
        /// If an existing Fox object is not found with the specified key, optionally recycle one of the other objects in the pool with a different key.
        /// Recycling can reduce loading times, but it can also cause problems if the FoxPro app is not prepared to handle a different key.
        /// Default: false.
        /// </summary>
        public static bool RecycleOtherKeys { get; set; }
        /// <summary>
        /// Name of the global FoxPro Object.Property that contains the latest error message. 
        /// It must be a property on a global object. A global string variable is not sufficient.
        /// Default: "_Screen.cErrorMessage"
        /// </summary>
        public static string ErrorPropertyName { get; set; }
        /// <summary>
        /// Type/class of FoxApp object with Start/End hooks containing application specific code. Set with SetFoxAppType().
        /// </summary>
        private static Type FoxAppType { get; set; }

        static FoxPool()
        {
            pool = new ConcurrentDictionary<string, Fox>();
            // Set defaults
            PoolSize = Environment.ProcessorCount;
            DebugMode = false;
            FoxTimeout = 30;
            RecycleOtherKeys = false;
            ErrorPropertyName = "_Screen.cErrorMessage";
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }

        /// <summary>
        /// Get Fox object from pool.
        /// Automatically calls Fox.StartRequest() and FoxApp startup hooks.
        /// </summary>
        /// <param name="key">Key used to differentiate Fox object within pool.</param>
        /// <returns>Fox object from pool.</returns>
        public static Fox GetObject(string key)
        {
            Fox fox = null;

            while (true)
            {
                // Try to use existing object in pool
                if (poolEnabled && pool.Count > 0)
                {
                    // Prefer objects that start with key
                    fox = GetObjectFromPool(key);

                    // If none found, optionally recycle one of the other objects in the pool with a different key
                    // Recycling can reduce loading times, but it can also cause problems if the Fox app is not prepared 
                    // to handle a different key.
                    if (fox == null && RecycleOtherKeys)
                    {
                        fox = GetObjectFromPool();
                    }
                }

                // If no objects are available, create a new one
                if (fox == null)
                {
                    if (instanceCount < PoolSize)
                    {
                        instanceCount++;
                        Debug.WriteLine("GetObject() not found: " + key);
                        IFoxApp foxApp = CreateFoxAppObject();
                        fox = new Fox(key, foxApp, FoxTimeout, DebugMode, true, ErrorPropertyName);
                        break;
                    }
                    else if (instanceCount >= PoolSize && !RecycleOtherKeys && pool.Count > 0)
                    {
                        // If all instance slots used, but there is one available in pool with a different key,
                        //  steal its slot, even though we're not recycling keys
                        fox = GetObjectFromPool();
                        if (fox != null)
                        {
                            // Instance with the same key might have been returned to pool since previous check above
                            // If it's the same, no need to drop and recreate instance
                            if (fox.GetKey() != key)
                            {
                                Debug.WriteLine("Stealing pool slot from a different key: " + key);
                                fox.RemoveFromPool();
                                IFoxApp foxApp = CreateFoxAppObject();
                                fox = new Fox(key, foxApp, FoxTimeout, DebugMode, true, ErrorPropertyName);
                            }
                            break;
                        }
                    }

                    if (fox == null)
                    {
                        // All instances are created and busy, so wait for one to become available
                        Thread.Sleep(50);
                    }
                }
                else
                {
                    poolCount++;
                    break;
                }
            }

            fox.StartRequest(key);
            return fox;
        }

        /// <summary>
        /// Get Fox object from pool.
        /// Automatically calls Fox.StartRequestAsync() and FoxApp async startup hooks.
        /// </summary>
        /// <param name="key">Key used to differentiate Fox object within pool.</param>
        /// <returns>Fox object from pool.</returns>
        // Same as GetObject except it calls StartRequestAsync() and it uses Task.Delay() instead of Thread.Sleep()
        public static async Task<Fox> GetObjectAsync(string key)
        {
            Fox fox = null;

            while (true)
            {
                // Try to use existing object in pool
                if (poolEnabled && pool.Count > 0)
                {
                    // Prefer objects that start with key
                    fox = GetObjectFromPool(key);

                    // If none found, optionally recycle one of the other objects in the pool with a different key
                    // Recycling can reduce loading times, but it can also cause problems if the Fox app is not prepared 
                    // to handle a different key.
                    if (fox == null && RecycleOtherKeys)
                    {
                        fox = GetObjectFromPool();
                    }
                }

                // If no objects are available, create a new one
                if (fox == null)
                {
                    if (instanceCount < PoolSize)
                    {
                        instanceCount++;
                        Debug.WriteLine("GetObject() not found: " + key);
                        IFoxApp foxApp = CreateFoxAppObject();
                        fox = new Fox(key, foxApp, FoxTimeout, DebugMode, true, ErrorPropertyName);
                        break;
                    }
                    else if (instanceCount >= PoolSize && !RecycleOtherKeys && pool.Count > 0)
                    {
                        // If all instance slots used, but there is one available in pool with a different key,
                        //  steal its slot, even though we're not recycling keys
                        fox = GetObjectFromPool();
                        if (fox != null)
                        {
                            // Instance with the same key might have been returned to pool since previous check above
                            // If it's the same, no need to drop and recreate instance
                            if (fox.GetKey() != key)
                            {
                                Debug.WriteLine("Stealing pool slot from a different key: " + key);
                                fox.RemoveFromPool();
                                IFoxApp foxApp = CreateFoxAppObject();
                                fox = new Fox(key, foxApp, FoxTimeout, DebugMode, true, ErrorPropertyName);
                            }
                            break;
                        }
                    }

                    if (fox == null)
                    {
                        // All instances are created and busy, so wait for one to become available
                        await Task.Delay(50);
                    }
                }
                else
                {
                    poolCount++;
                    break;
                }
            }

            await fox.StartRequestAsync(key);
            return fox;
        }

        /// <summary>
        /// Get object from pool with or without key. 
        /// If key not specified, will return any object that is currently in pool regardless of which key it was added with.
        /// </summary>
        /// <param name="key">Optional. Key used to differentiate Fox object within pool.</param>
        /// <returns>Fox object from pool (if available).</returns>
        private static Fox GetObjectFromPool(string key = null)
        {
            Fox fox = null;
            string findKey = null;
            if (key != null)
            {
                findKey = FormatKey(key);
            }
            foreach (var dictKey in pool.Keys)
            {
                if (findKey == null || dictKey.StartsWith(findKey))
                {
                    if (pool.TryRemove(dictKey, out fox))
                    {
                        Debug.WriteLine("GetObject() key: " + key + " Reusing: " + dictKey);
                        break;
                    }
                }
            }

            return fox;
        }

        /// <summary>
        /// Add Fox object to pool for reuse later 
        /// </summary>
        /// <param name="key">Key used to differentiate Fox object within pool.</param>
        /// <param name="fox">Fox object.</param>
        /// <returns></returns>
        public static bool AddObject(string key, Fox fox)
        {
            var added = false;
            
            // If MaxObjects already in pool, don't add
            if (poolEnabled && pool.Count < PoolSize)
            {
                // Add GUID to key to make it unique
                var dictKey = FormatKey(key) + fox.Id;
                Debug.WriteLine("AddObject(): " + dictKey);
                added = pool.TryAdd(dictKey, fox);
                Debug.WriteLine("AddObject(): " + added.ToString() + " Count: " + pool.Count.ToString() + " " + dictKey);
            }
            else
            {
                Debug.WriteLine("AddObject() pool full  Count: " + pool.Count.ToString() + " " + key + " " + fox.Id);
            }

            return added;
        }

        /// <summary>
        /// Release all Fox objects from pool.
        /// </summary>
        public static void ClearPool()
        {
            Debug.WriteLine("ClearPool()");

            try
            {
                // Disable pool while clearing
                poolEnabled = false;

                // Clear pool and dispose objects
                Fox fox;
                foreach (var key in pool.Keys)
                {
                    if (pool.TryRemove(key, out fox))
                    {
                        fox.Dispose();
                    }
                }
                pool.Clear();
            }
            finally
            {
                // Reenable pool
                instanceCount = 0;
                poolEnabled = true;
            }
        }

        /// <summary>
        /// Set type/class for FoxApp containing hooks to be created by FoxPool.
        /// </summary>
        /// <typeparam name="TFoxApp">FoxApp type/class.</typeparam>
        public static void SetFoxAppType<TFoxApp>()
            where TFoxApp: IFoxApp, new()
        {
            FoxAppType = typeof(TFoxApp);
        }

        /// <summary>
        /// Reset FoxAppType to null, so that no FoxApp object will be created
        /// </summary>
        public static void ResetFoxAppType()
        {
            FoxAppType = null;
        }

        /// <summary>
        /// Create FoxApp object when Fox object is added to pool.
        /// </summary>
        /// <returns>FoxApp object</returns>
        private static IFoxApp CreateFoxAppObject()
        {
            IFoxApp foxApp = null;
            if (!(FoxAppType is null))
            {
                foxApp = (IFoxApp)Activator.CreateInstance(FoxAppType);
            }

            return foxApp;
        }

        /// <summary>
        /// Format object key consistently.
        /// </summary>
        /// <param name="objectKey">Object key. Used for DotNet2Fox internal debugging.</param>
        /// <returns>Formatted object key.</returns>
        private static string FormatKey(string objectKey)
        {
            var formattedKey = "[" + objectKey.ToLower().Trim() + "]";
            return formattedKey;
        }
        
        /// <summary>
        /// Clear pool when application quits.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            // Clear pool when application quits
            // Unforunately, this isn't called when you Stop Debugging in Visual Studio
            ClearPool();
        }
    }
}
