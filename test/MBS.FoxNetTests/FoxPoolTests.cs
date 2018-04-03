﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using MBS.FoxPro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace MBS.FoxPro.Tests
{
    [TestClass()]
    public class FoxPoolTests
    {
        [TestMethod()]
        public void PoolGetObjectTest()
        {
            FoxPool.DebugMode = true;
            using (FoxNet fox = FoxPool.GetObject("FoxNetTests"))
            {
                var result = fox.Eval("1+1");
                Assert.AreEqual(result, 2);
            }
            FoxPool.ClearPool();
        }

        private void LoadTest(int iterations)
        {
            Parallel.For(1, iterations, (i, loopState) =>
            {
                using (FoxNet fox = FoxPool.GetObject("FoxNetTests"))
                {
                    fox.DoCmd("? 'Load Test', " + i.ToString());
                    var result = fox.Eval("1+1");
                    Assert.AreEqual(result, 2);
                }
            });
            FoxPool.ClearPool();
        }

        [TestMethod()]
        public void PoolLowLoadTest()
        {
            FoxPool.DebugMode = true;
            LoadTest(50);
        }

        [TestMethod()]
        public void PoolHeavyLoadTest()
        {
            FoxPool.DebugMode = true;
            LoadTest(1000);
        }

        [TestMethod()]
        public void PoolLowLoadFoxAppTest()
        {
            FoxPool.DebugMode = true;
            FoxPool.FoxApp = new FoxNetTestApp();
            LoadTest(50);
            FoxPool.FoxApp = null;
        }

        [TestMethod()]
        public void PoolHeavyLoadFoxAppTest()
        {
            FoxPool.DebugMode = true;
            FoxPool.FoxApp = new FoxNetTestApp();
            LoadTest(500);
            FoxPool.FoxApp = null;
        }

        [TestMethod()]
        public void PoolHeavyLoadFoxAppNoDebugTest()
        {
            FoxPool.DebugMode = false;
            FoxPool.FoxApp = new FoxNetTestApp();
            LoadTest(500);
            FoxPool.FoxApp = null;
        }

        private void TimeoutTest()
        {
            FoxPool.FoxTimeout = 1;
            var lastThreadID = 0;
            var newThreadID = 0;
            for (int i = 0; i < 5; i++)
            {
                using (FoxNet fox = FoxPool.GetObject("FoxNetTests"))
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
            FoxPool.FoxApp = new FoxNetTestApp();
            TimeoutTest();
            FoxPool.FoxApp = null;
        }

        [TestMethod()]
        public void PoolTimeoutFoxAppNoDebugTest()
        {
            FoxPool.DebugMode = false;
            FoxPool.FoxApp = new FoxNetTestApp();
            TimeoutTest();
            FoxPool.FoxApp = null;
        }

    }
}