using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace DotNet2Fox.Tests
{
    [TestClass()]
    public class FoxPoolTests
    {
        [TestMethod()]
        public void PoolGetObjectTest()
        {
            FoxPool.DebugMode = true;
            using (Fox fox = FoxPool.GetObject("FoxTests"))
            {
                var result = fox.Eval("1+1");
                Assert.AreEqual(result, 2);
            }
            FoxPool.ClearPool();
        }

        private void LoadTest(int iterations)
        {
            Debug.WriteLine("==== Begin LoadTest: " + iterations.ToString() + " ===");
            FoxPool.ClearPool();
            Parallel.For(1, iterations, (i, loopState) =>
            {
                using (Fox fox = FoxPool.GetObject("FoxTests"))
                {
                    //fox.DoCmd("? 'Load Test', " + i.ToString());
                    //fox.DoCmd($"Wait Window NoWait 'Load Test {i}'");
                    var result = fox.Eval("1+1");
                    Assert.AreEqual(result, 2);
                }
            });
            Debug.WriteLine("==== End LoadTest: " + iterations.ToString() + " ===");
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
        public void PoolHeavyLoadNoDebugTest()
        {
            FoxPool.DebugMode = false;
            LoadTest(1000);
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
        public void PoolHeavyLoadFoxAppNoDebugTest()
        {
            FoxPool.DebugMode = false;
            FoxPool.SetFoxAppType<FoxTestApp>();
            LoadTest(500);
            FoxPool.ResetFoxAppType();
        }

        private void TimeoutTest()
        {
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
            FoxPool.DebugMode = false;
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

    }
}