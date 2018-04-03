// Pool for FoxNet objects
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace MBS.FoxPro
{
    public class FoxPool
    {
        private static ConcurrentDictionary<string, FoxNet> pool;
        private static volatile bool poolEnabled = true;
        public static volatile int instCount = 0;
        public static volatile int poolCount = 0;

        public static int PoolSize { get; set; }
        public static IFoxApp FoxApp { get; set; }
        public static bool DebugMode { get; set; }
        // Seconds before inactive FoxCOM object is released
        public static int FoxTimeout { get; set; }

        static FoxPool()
        {
            pool = new ConcurrentDictionary<string, FoxNet>();
            // Set defaults
            PoolSize = Environment.ProcessorCount;
            FoxApp = null;
            DebugMode = false;
            FoxTimeout = 30;
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

        // Get FoxNet object from pool
        public static FoxNet GetObject(string key)
        {
            FoxNet foxNet = null;

            while (true)
            {
                // Try to use existing object in pool
                if (poolEnabled && pool.Count > 0)
                {
                    // Prefer objects that start with key
                    foxNet = GetObjectFromPool(key);

                    // If none found, use one of the other objects in the pool
                    if (foxNet == null)
                    {
                        foxNet = GetObjectFromPool();
                    }
                }

                // If no objects are available, create a new one
                if (foxNet == null)
                {
                    if (instCount < PoolSize)
                    {
                        instCount++;
                        Debug.WriteLine("GetObject() not found: " + key);
                        foxNet = new FoxNet(key, FoxApp, FoxTimeout, DebugMode, true);
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

            foxNet.StartRequest(key);
            return foxNet;
        }

        // Get object from pool with or without key
        private static FoxNet GetObjectFromPool(string key = null)
        {
            FoxNet item = null;
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
        public static bool AddObject(string key, FoxNet item)
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
                FoxNet item;
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
        
        // Format object key consistently so it can be 
        private static string FormatKey(string objectKey)
        {
            var formattedKey = "[" + objectKey.ToLower().Trim() + "]";
            return formattedKey;
        }

    }
}
