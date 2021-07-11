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
    public class FoxPool
    {
        private static ConcurrentDictionary<string, Fox> pool;
        private static volatile bool poolEnabled = true;
        public static volatile int instCount = 0;
        public static volatile int poolCount = 0;

        public static int PoolSize { get; set; }
        public static bool DebugMode { get; set; }
        // Seconds before inactive FoxCOM object is released
        public static int FoxTimeout { get; set; }
        public static bool RecycleOtherKeys { get; set; }
        private static Type FoxAppType { get; set; }

        static FoxPool()
        {
            pool = new ConcurrentDictionary<string, Fox>();
            // Set defaults
            PoolSize = Environment.ProcessorCount;
            DebugMode = false;
            FoxTimeout = 30;
            RecycleOtherKeys = false;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }

        //static readonly Finalizer finalizer = new Finalizer();

        //sealed class Finalizer
        //{
        //    ~Finalizer()
        //    {
        //        ClearPool();
        //    }
        //}

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            // Clear pool when application quits
            // Unforunately, this isn't called when you Stop Debugging in Visual Studio
            ClearPool();
        }

        // Get Fox object from pool
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
                    if (instCount < PoolSize)
                    {
                        instCount++;
                        Debug.WriteLine("GetObject() not found: " + key);
                        IFoxApp foxApp = CreateFoxAppObject();
                        fox = new Fox(key, foxApp, FoxTimeout, DebugMode, true);
                        break;
                    }
                    else
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

        // Get object from pool with or without key
        private static Fox GetObjectFromPool(string key = null)
        {
            Fox item = null;
            string findKey = null;
            if (key != null)
            {
                findKey = FormatKey(key);
            }
            foreach (var dictKey in pool.Keys)
            {
                if (findKey == null || dictKey.StartsWith(findKey))
                {
                    if (pool.TryRemove(dictKey, out item))
                    {
                        Debug.WriteLine("GetObject() key: " + key + " Reusing: " + dictKey);
                        break;
                    }
                }
            }

            return item;
        }

        // Add object to pool for reuse later
        public static bool AddObject(string key, Fox item)
        {
            var added = false;
            
            // If MaxObjects already in pool, don't add
            if (poolEnabled && pool.Count < PoolSize)
            {
                // Add GUID to key to make it unique
                var dictKey = FormatKey(key) + item.id;
                Debug.WriteLine("AddObject(): " + dictKey);
                added = pool.TryAdd(dictKey, item);
                Debug.WriteLine("AddObject(): " + added.ToString() + " Count: " + pool.Count.ToString() + " " + dictKey);
            }
            else
            {
                Debug.WriteLine("AddObject() pool full  Count: " + pool.Count.ToString() + " " + key + " " + item.id);
            }

            return added;
        }

        public static void ClearPool()
        {
            Debug.WriteLine("ClearPool()");

            try
            {
                // Disable pool while clearing
                poolEnabled = false;

                // Clear pool and dispose objects
                Fox item;
                foreach (var key in pool.Keys)
                {
                    if (pool.TryRemove(key, out item))
                    {
                        try
                        {
                            item.Dispose();
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                }
                pool.Clear();
            }
            catch (Exception e)
            {

                throw e;
            }
            finally
            {
                // Reenable pool
                instCount = 0;
                poolEnabled = true;
            }
        }

        // Set type/class for FoxApp to be created later
        public static void SetFoxAppType<TFoxApp>()
            where TFoxApp: IFoxApp, new()
        {
            FoxAppType = typeof(TFoxApp);
        }

        // Reset FoxAppType to null, so that no FoxApp object will be created
        public static void ResetFoxAppType()
        {
            FoxAppType = null;
        }

        // Create FoxApp object when Fox object is added to pool
        private static IFoxApp CreateFoxAppObject()
        {
            IFoxApp foxApp = null;
            if (!(FoxAppType is null))
            {
                foxApp = (IFoxApp)Activator.CreateInstance(FoxAppType);
            }

            return foxApp;
        }

        // Format object key consistently so it can be 
        private static string FormatKey(string objectKey)
        {
            var formattedKey = "[" + objectKey.ToLower().Trim() + "]";
            return formattedKey;
        }

    }
}
