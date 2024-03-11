using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System;
using System.IO;
using System.Reflection;

namespace DotNet2Fox.Tests
{
    [TestClass()]
    public class FoxPoolTests
    {
        [TestMethod()]
        public void PoolGetObjectTest()
        {
            Debug.WriteLine(DateTime.Now.ToString());
            FoxPool.DebugMode = true;
            using (Fox fox = FoxPool.GetObject("FoxTests"))
            {
                var result = fox.Eval("1+1");
                Assert.AreEqual(result, 2);
            }
            FoxPool.ClearPool();
        }

        [TestMethod()]
        public async Task PoolGetObjectAsyncTest()
        {
            Debug.WriteLine(DateTime.Now.ToString());
            FoxPool.DebugMode = true;
            using (Fox fox = await FoxPool.GetObjectAsync("FoxTests"))
            {
                var result = await fox.EvalAsync("1+1");
                Assert.AreEqual(result, 2);
            }
            FoxPool.ClearPool();
        }

        [TestMethod()]
        public void PoolGetObjectRegFreeCOMTest()
        {
            Debug.WriteLine(DateTime.Now.ToString());
            FoxPool.DebugMode = false;
            FoxPool.RegFreeFoxCOMPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\FoxCOM.exe";
            using (Fox fox = FoxPool.GetObject("FoxTests"))
            {
                var result = fox.Eval("1+1");
                Assert.AreEqual(result, 2);
            }
            FoxPool.ClearPool();
            FoxPool.RegFreeFoxCOMPath = "";
        }

        [TestMethod()]
        public async Task PoolGetObjectRegFreeCOMAsyncTest()
        {
            Debug.WriteLine(DateTime.Now.ToString());
            FoxPool.DebugMode = false;
            FoxPool.RegFreeFoxCOMPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\FoxCOM.exe";
            using (Fox fox = await FoxPool.GetObjectAsync("FoxTests"))
            {
                var result = await fox.EvalAsync("1+1");
                Assert.AreEqual(result, 2);
            }
            FoxPool.ClearPool();
            FoxPool.RegFreeFoxCOMPath = "";
        }

        private void LoadTest(int iterations)
        {
            Debug.WriteLine("==== Begin LoadTest: " + iterations.ToString() + " " + DateTime.Now.ToString() + " ===");
            FoxPool.ClearPool();

            // Task.WhenAll method is similar to async test
            var tasks = new List<Task>();
            for (int i = 0; i < iterations; i++)
            {
                var j = i;  // needed for Task.Run to see variable
                tasks.Add(Task.Run(() =>
                {
                    Debug.WriteLine("********** Load Test: " + j.ToString());
                    using (Fox fox = FoxPool.GetObject("FoxTests"))
                    {
                        //fox.DoCmd("? 'Load Test', " + i.ToString());
                        // fox.DoCmd($"Wait Window NoWait 'Load Test {i}'");
                        //fox.AutomaticGarbageCollection = false;
                        var result = fox.Eval("1+1");
                        Assert.AreEqual(result, 2);
                    }
                }));
            }
            Task.WhenAll(tasks).Wait();

            // Parallel.For works too, but only for sync load test
            //Parallel.For(1, iterations, (i, loopState) =>
            //{
            //    Debug.WriteLine("********** Load Test: " + i.ToString());
            //    using (Fox fox = FoxPool.GetObject("FoxTests"))
            //    {
            //        //fox.DoCmd("? 'Load Test', " + i.ToString());
            //        //fox.DoCmd($"Wait Window NoWait 'Load Test {i}'");
            //        var result = fox.Eval("1+1");
            //        Assert.AreEqual(result, 2);
            //    }
            //});
            Debug.WriteLine("==== End LoadTest: " + iterations.ToString() + " " + DateTime.Now.ToString() + " ===");
            FoxPool.ClearPool();
        }

        private async Task LoadTestAsync(int iterations)
        {
            Debug.WriteLine("==== Begin LoadTestAsync: " + iterations.ToString() + " ===");
            FoxPool.ClearPool();
            // Parallel.For and async don't mix
            // Use Tasks.WhenAll() instead: https://stackoverflow.com/a/19292500/6388118
            var tasks = new List<Task>();
            for (int i = 0; i < iterations; i++)
            {
                var j = i;  // needed for Task.Run to see variable
                tasks.Add(Task.Run(async () =>
                {
                    Debug.WriteLine("********** Load Test: " + j.ToString());
                    using (Fox fox = await FoxPool.GetObjectAsync("FoxTests"))
                    {
                        //fox.DoCmd("? 'Load Test', " + i.ToString());
                        // fox.DoCmd($"Wait Window NoWait 'Load Test {i}'");
                        var result = await fox.EvalAsync("1+1");
                        Assert.AreEqual(result, 2);
                    }
                }));
            }
            await Task.WhenAll(tasks);

            // Enumerable.Range did not work right with async/await inside Select()
            // Replaced with method above, which is same as sync load test
            //var tasks = Enumerable.Range(0, iterations)
            //                .Select(async i =>
            //                {
            //                    Debug.WriteLine("********** Load Test Async: " + i.ToString());
            //                    using (Fox fox = await FoxPool.GetObjectAsync("FoxTests"))
            //                    {
            //                        //fox.DoCmd("? 'Load Test', " + i.ToString());
            //                        // fox.DoCmd($"Wait Window NoWait 'Load Test {i}'");
            //                        var result = await fox.EvalAsync("1+1");
            //                        Assert.AreEqual(result, 2);
            //                    }
            //                })
            //                .ToArray();
            //await Task.WhenAll(tasks);

            Debug.WriteLine("==== End LoadTestAsync: " + iterations.ToString() + " ===");
            FoxPool.ClearPool();
        }

        [TestMethod()]
        public void PoolLowLoadTest()
        {
            FoxPool.DebugMode = true;
            LoadTest(50);
        }

        [TestMethod()]
        public async Task PoolLowLoadTestAsync()
        {
            FoxPool.DebugMode = true;
            await LoadTestAsync(50);
        }

        [TestMethod()]
        public void PoolRegFreeCOMLoadTest()
        {
            // Kill any running FoxCOM.exe processes first
            foreach (var process in Process.GetProcessesByName("foxcom"))
            {
                process.Kill();
            }
            FoxPool.PoolSize = 96;
            FoxPool.DebugMode = false;
            FoxPool.RegFreeFoxCOMPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\FoxCOM.exe";
            LoadTest(100);
            FoxPool.RegFreeFoxCOMPath = "";
            // No FoxCOM.exe processes should remain open. Give them time to close first.
            Thread.Sleep(1000);
            Assert.AreEqual(0, Process.GetProcessesByName("foxcom").Length);
        }

        [TestMethod()]
        public async Task PoolRegFreeCOMLoadTestAsync()
        {
            // Kill any running FoxCOM.exe processes first
            foreach (var process in Process.GetProcessesByName("foxcom"))
            {
                process.Kill();
            }
            FoxPool.PoolSize = 96;
            FoxPool.DebugMode = false;
            FoxPool.RegFreeFoxCOMPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\FoxCOM.exe";
            await LoadTestAsync(100);
            FoxPool.RegFreeFoxCOMPath = "";
            // No FoxCOM.exe processes should remain open. Give them time to close first.
            await Task.Delay(1000);
            Assert.AreEqual(0, Process.GetProcessesByName("foxcom").Length);
        }

        [TestMethod()]
        public void PoolHeavyLoadTest()
        {
            FoxPool.DebugMode = true;
            LoadTest(1000);
        }

        [TestMethod()]
        public async Task PoolHeavyLoadTestAsync()
        {
            FoxPool.DebugMode = true;
            await LoadTestAsync(1000);
        }

        [TestMethod()]
        public void PoolHeavyLoadNoDebugTest()
        {
            FoxPool.DebugMode = false;
            LoadTest(1000);
        }

        [TestMethod()]
        public async Task PoolHeavyLoadNoDebugTestAsync()
        {
            FoxPool.DebugMode = false;
            await LoadTestAsync(1000);
        }

        [TestMethod()]
        public void PoolLowLoadFoxAppTest()
        {
            FoxPool.DebugMode = true;
            FoxPool.SetFoxAppType<FoxTestApp>();
            LoadTest(50);
            FoxPool.ResetFoxAppType();
        }

        [TestMethod()]
        public void PoolHeavyLoadFoxAppTest()
        {
            FoxPool.DebugMode = true;
            FoxPool.SetFoxAppType<FoxTestApp>();
            LoadTest(500);
            FoxPool.ResetFoxAppType();
        }

        [TestMethod()]
        public async Task PoolHeavyLoadFoxAppAsyncTest()
        {
            FoxPool.DebugMode = true;
            FoxPool.SetFoxAppType<FoxTestApp>();
            await LoadTestAsync(500);
            FoxPool.ResetFoxAppType();
        }

        [TestMethod()]
        public void PoolHeavyLoadFoxAppNoDebugTest()
        {
            FoxPool.DebugMode = false;
            FoxPool.SetFoxAppType<FoxTestApp>();
            LoadTest(500);
            FoxPool.ResetFoxAppType();
        }

        private void TimeoutTest()
        {
            Debug.WriteLine(DateTime.Now.ToString());
            FoxPool.FoxTimeout = 1;
            var lastThreadID = 0;
            var newThreadID = 0;
            for (int i = 0; i < 5; i++)
            {
                using (Fox fox = FoxPool.GetObject("FoxTests"))
                {
                    fox.DoCmd("? 'Timeout Test', " + i.ToString() + ", 'Thread', _VFP.ThreadID");
                    // Make sure Fox instance times out and starts a new instance for every request
                    newThreadID = fox.Eval("_VFP.ThreadID");
                    Assert.AreNotEqual(newThreadID, lastThreadID);
                    lastThreadID = newThreadID;
                }
                Thread.Sleep(1500);
            }
            FoxPool.ClearPool();
            FoxPool.FoxTimeout = 60;
        }


        [TestMethod()]
        public void PoolTimeoutTest()
        {
            FoxPool.DebugMode = true;
            TimeoutTest();
        }

        [TestMethod()]
        public void PoolTimeoutFoxAppTest()
        {
            FoxPool.DebugMode = true;
            FoxPool.SetFoxAppType<FoxTestApp>();
            TimeoutTest();
            FoxPool.ResetFoxAppType();
        }

        [TestMethod()]
        public void PoolTimeoutFoxAppNoDebugTest()
        {
            FoxPool.DebugMode = false;
            FoxPool.SetFoxAppType<FoxTestApp>();
            TimeoutTest();
            FoxPool.ResetFoxAppType();
        }

        [TestMethod()]
        public void PoolReleaseFormTest()
        {
            Debug.WriteLine(DateTime.Now.ToString());
            FoxPool.DebugMode = true;
            for (int i = 0; i < 5; i++)
            {
                using (Fox fox = FoxPool.GetObject("FoxTests"))
                {
                    Thread.Sleep(500);
                    var form = fox.CreateNewObject("Form", "");
                    // Adjust form in separate method to prevent dangling references 
                    //  to objects contained in form. Otherwise, form won't release.
                    AdjustForm(form, i);
                    form.Release();
                    form = null;
                    Thread.Sleep(100);
                }
                // Each form should be released here, before the next form appears
            }
            Thread.Sleep(1000);
            FoxPool.ClearPool();
        }

        private void AdjustForm(dynamic form, int i)
        {
            form.Caption = "Form Test " + i.ToString();
            form.Left = i * 50;
            form.Top = i * 50;
            form.Show();
            form.AddObject("txtTest", "TextBox");
            var txtTest = form.txtTest;
            txtTest.Visible = true;
            txtTest.Value = "Test";
        }

        [TestMethod()]
        public void PoolPerformanceTest()
        {
            // Performance when calling single Fox instance multiple times
            Debug.WriteLine(DateTime.Now.ToString());
            FoxPool.DebugMode = true;
            int iterations = 100;
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                using (Fox fox = FoxPool.GetObject("FoxTests"))
                {
                    var result = fox.Eval("1+1");
                    Assert.AreEqual(result, 2);
                }
            }
            stopwatch.Stop();
            var ms = stopwatch.ElapsedMilliseconds;
            decimal msPerCall = decimal.Divide(ms, iterations);
            Console.WriteLine($"Total time: {ms} ms");
            Console.WriteLine($"Time per call: {msPerCall} ms");
            FoxPool.ClearPool();
        }

        [TestMethod()]
        public async Task PoolPerformanceAsyncTest()
        {
            Debug.WriteLine(DateTime.Now.ToString());
            FoxPool.DebugMode = true;
            int iterations = 100;
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                using (Fox fox = await FoxPool.GetObjectAsync("FoxTests"))
                {
                    var result = await fox.EvalAsync("1+1");
                    Assert.AreEqual(result, 2);
                }
            }
            stopwatch.Stop();
            var ms = stopwatch.ElapsedMilliseconds;
            decimal msPerCall = decimal.Divide(ms, iterations);
            Console.WriteLine($"Total time: {ms} ms");
            Console.WriteLine($"Time per call: {msPerCall} ms");
            FoxPool.ClearPool();
        }

        [TestMethod()]
        public void PoolStealSlotTest()
        {
            Debug.WriteLine(DateTime.Now.ToString());
            FoxPool.DebugMode = true;
            FoxPool.RecycleOtherKeys = false;
            int poolSize = FoxPool.PoolSize;
            try
            {
                FoxPool.PoolSize = 1;
                using (Fox fox = FoxPool.GetObject("PoolKey1"))
                {
                    var result = fox.Eval("1+1");
                    Assert.AreEqual(result, 2);
                }
                // Pool is already full with PoolKey1.
                // Since it is currently in pool doing nothing, steal its slot instead of waiting.
                // Debug into GetObject() to check if it is working or look for "Stealing pool slot" in debug log.
                using (Fox fox = FoxPool.GetObject("PoolKey2"))
                {
                    var result = fox.Eval("1+1");
                    Assert.AreEqual(result, 2);
                }
            }
            finally
            {
                FoxPool.PoolSize = poolSize;
                FoxPool.ClearPool();
            }
        }

        [TestMethod()]
        public async Task PoolStealSlotTestAsync()
        {
            Debug.WriteLine(DateTime.Now.ToString());
            FoxPool.DebugMode = true;
            FoxPool.RecycleOtherKeys = false;
            int poolSize = FoxPool.PoolSize;
            try
            {
                FoxPool.PoolSize = 1;
                using (Fox fox = await FoxPool.GetObjectAsync("PoolKey1"))
                {
                    var result = fox.Eval("1+1");
                    Assert.AreEqual(result, 2);
                }
                // Pool is already full with PoolKey1.
                // Since it is currently in pool doing nothing, steal its slot instead of waiting.
                // Debug into GetObjectAsync() to check if it is working or look for "Stealing pool slot" in debug log.
                using (Fox fox = await FoxPool.GetObjectAsync("PoolKey2"))
                {
                    var result = fox.Eval("1+1");
                    Assert.AreEqual(result, 2);
                }
            }
            finally
            {
                FoxPool.PoolSize = poolSize;
                FoxPool.ClearPool();
            }
        }

    }
}