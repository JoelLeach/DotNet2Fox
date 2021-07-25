using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace DotNet2Fox.Tests
{
    [TestClass()]
    public class FoxTests
    {

        // Location of FoxPro code to execute
        string foxCodePath = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).ToString()).ToString() + "/FoxCode";

        [TestMethod()]
        public void DoCmdTest()
        {
            using (Fox fox = new Fox("FoxTests", null, 60, true))
            {
                fox.StartRequest("FoxTests");
                fox.DoCmd("? 'Fox Unit Test'");
            }
        }

        [TestMethod()]
        public async Task DoCmdAsyncTest()
        {
            using (Fox fox = new Fox("FoxTests", null, 60, true))
            {
                fox.StartRequest("FoxTests");
                await fox.DoCmdAsync("? 'Fox Unit Test'");
            }
        }

        [TestMethod()]
        public void EvalTest()
        {
            using (Fox fox = new Fox("FoxTests", null, 60, true))
            {
                fox.StartRequest("FoxTests");
                var result = fox.Eval("1+1");
                Assert.AreEqual(result, 2);
            }
        }

        [TestMethod()]
        public async Task EvalAsyncTest()
        {
            using (Fox fox = new Fox("FoxTests", null, 60, true))
            {
                fox.StartRequest("FoxTests");
                var result = await fox.EvalAsync("1+1");
                Assert.AreEqual(result, 2);
            }
        }

        [TestMethod()]
        public void EvalNoDebugTest()
        {
            using (Fox fox = new Fox("FoxTests", null, 60, false))
            {
                fox.StartRequest("FoxTests");
                var result = fox.Eval("1+1");
                Assert.AreEqual(result, 2);
            }
        }

        [TestMethod()]
        public void DoPRGTest()
        {
            using (Fox fox = new Fox("FoxTests", null, 60, true))
            {
                fox.StartRequest("FoxTests");
                fox.DoCmd("Set path to '" + foxCodePath + "' Additive");
                fox.Do("AddNumbers", "", 2, 3);
            }
        }

        [TestMethod()]
        public async Task DoPRGAsyncTest()
        {
            using (Fox fox = new Fox("FoxTests", null, 60, true))
            {
                fox.StartRequest("FoxTests");
                fox.DoCmd("Set path to '" + foxCodePath + "' Additive");
                await fox.DoAsync("AddNumbers", "", 2, 3);
            }
        }

        [TestMethod()]
        public void CallFunctionTest()
        {
            using (Fox fox = new Fox("FoxTests", null, 60, true))
            {
                fox.StartRequest("FoxTests");
                var result = fox.Call("Lower", "FOXTEST");
                Assert.AreEqual(result, "foxtest");
            }
        }

        [TestMethod()]
        public async Task CallAsyncFunctionTest()
        {
            using (Fox fox = new Fox("FoxTests", null, 60, true))
            {
                fox.StartRequest("FoxTests");
                var result = await fox.CallAsync("Lower", "FOXTEST");
                Assert.AreEqual(result, "foxtest");
            }
        }

        [TestMethod()]
        public void CallPRGTest()
        {
            using (Fox fox = new Fox("FoxTests", null, 60, true))
            {
                fox.StartRequest("FoxTests");
                fox.DoCmd("Set path to '" + foxCodePath + "' Additive");
                var result = fox.Call("AddNumbers", 2, 3);
                Assert.AreEqual(result, 5);
            }
        }

        [TestMethod()]
        public async Task CallAsyncPRGTest()
        {
            using (Fox fox = new Fox("FoxTests", null, 60, true))
            {
                fox.StartRequest("FoxTests");
                fox.DoCmd("Set path to '" + foxCodePath + "' Additive");
                var result = await fox.CallAsync("AddNumbers", 2, 3);
                Assert.AreEqual(result, 5);
            }
        }

        [TestMethod()]
        public void CallMethodVCXTest()
        {
            using (Fox fox = new Fox("FoxTests", null, 60, true))
            {
                fox.StartRequest("FoxTests");
                fox.DoCmd("Set path to '" + foxCodePath + "' Additive");
                var result = fox.CallMethod("AddNumbers", "FoxTest", "FoxTest.vcx", "", 2, 3);
                Assert.AreEqual(result, 5);
            }
        }

        [TestMethod()]
        public void CallMethodPRGTest()
        {
            using (Fox fox = new Fox("FoxTests", null, 60, true))
            {
                fox.StartRequest("FoxTests");
                fox.DoCmd("Set path to '" + foxCodePath + "' Additive");
                var result = fox.CallMethod("AddNumbers", "FoxTest", "FoxTest.prg", "", 2, 3);
                Assert.AreEqual(result, 5);
            }
        }

        [TestMethod()]
        public void CreateObjectVCXTest()
        {
            using (Fox fox = new Fox("FoxTests", null, 60, true))
            {
                fox.StartRequest("FoxTests");
                fox.DoCmd("Set path to '" + foxCodePath + "' Additive");
                var foxTest = fox.CreateNewObject("FoxTest", "FoxTest.vcx");
                var result = foxTest.AddNumbers(2, 3);
                Assert.AreEqual(result, 5);
            }
        }

        [TestMethod()]
        public void CreateObjectPRGTest()
        {
            using (Fox fox = new Fox("FoxTests", null, 60, true))
            {
                fox.StartRequest("FoxTests");
                fox.DoCmd("Set path to '" + foxCodePath + "' Additive");
                var foxTest = fox.CreateNewObject("FoxTest", "FoxTest.prg");
                var result = foxTest.AddNumbers(2, 3);
                Assert.AreEqual(result, 5);
            }
        }

        [TestMethod()]
        public void ExecScriptTest()
        {
            var script = @"* Test FoxPro script used for unit testing
                            Lparameters lnNum1, lnNum2
                            Local lnResult

                            lnResult = lnNum1 + lnNum2
                            ? lnResult

                            Return lnResult";

            using (Fox fox = new Fox("FoxTests", null, 60, true))
            {
                fox.StartRequest("FoxTests");
                var result = fox.ExecScript(script, 2, 3);
                Assert.AreEqual(result, 5);
            }
        }

        [TestMethod()]
        public void ErrorTest()
        {
            using (Fox fox = new Fox("FoxTests", null, 60, true))
            {
                fox.StartRequest("FoxTests");
                fox.DoCmd("Set path to '" + foxCodePath + "' Additive");
                try
                {
                    fox.Do("ErrorTest");
                }
                catch (Exception e)
                {
                    Assert.AreEqual(e.Message, "Fox Test Error");
                }
            }
        }

        [TestMethod()]
        public async Task ErrorAsyncTest()
        {
            using (Fox fox = new Fox("FoxTests", null, 60, true))
            {
                fox.StartRequest("FoxTests");
                fox.DoCmd("Set path to '" + foxCodePath + "' Additive");
                try
                {
                    await fox.DoAsync("ErrorTest");
                }
                catch (Exception e)
                {
                    Assert.AreEqual(e.Message, "Fox Test Error");
                }
            }
        }

        [TestMethod()]
        public void ErrorNoDebugTest()
        {
            using (Fox fox = new Fox("FoxTests", null, 60, false))
            {
                fox.StartRequest("FoxTests");
                fox.DoCmd("Set path to '" + foxCodePath + "' Additive");
                fox.DoCmd("Compile ErrorTest.prg");
                try
                {
                    fox.Do("ErrorTest");
                }
                catch (Exception e)
                {
                    Assert.AreEqual(e.Message, "Fox Test Error");
                }
            }
        }

        [TestMethod()]
        public async Task ErrorNoDebugAsyncTest()
        {
            using (Fox fox = new Fox("FoxTests", null, 60, false))
            {
                fox.StartRequest("FoxTests");
                fox.DoCmd("Set path to '" + foxCodePath + "' Additive");
                fox.DoCmd("Compile ErrorTest.prg");
                try
                {
                    await fox.DoAsync("ErrorTest");
                }
                catch (Exception e)
                {
                    Assert.AreEqual(e.Message, "Fox Test Error");
                }
            }
        }

        [TestMethod()]
        public void FoxAppTest()
        {
            using (Fox fox = new Fox("FoxTests", new FoxTestApp(), 60, true))
            {
                fox.StartRequest("FoxTests");
                var result = fox.Eval("2 + 3");
                Assert.AreEqual(result, 5);
            }
        }

        [TestMethod()]
        public void PerformanceTest()
        {
            using (Fox fox = new Fox("FoxTests", null, 60, false))
            {
                fox.StartRequest("FoxTests");
                int iterations = 100;
                var stopwatch = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    fox.Eval("2 + 3");
                }
                stopwatch.Stop();
                var ms = stopwatch.ElapsedMilliseconds;
                decimal msPerCall = decimal.Divide(ms, iterations);
                Console.WriteLine($"Total time excluding startup: {ms} ms");
                Console.WriteLine($"Time per call: {msPerCall} ms");
            }
        }

        [TestMethod()]
        public async Task PerformanceAsyncTest()
        {
            using (Fox fox = new Fox("FoxTests", null, 60, false))
            {
                fox.StartRequest("FoxTests");
                int iterations = 100;
                var stopwatch = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    await fox.EvalAsync("2 + 3");
                }
                stopwatch.Stop();
                var ms = stopwatch.ElapsedMilliseconds;
                decimal msPerCall = Decimal.Divide(ms, iterations);
                Console.WriteLine($"Total time excluding startup: {ms} ms");
                Console.WriteLine($"Time per call: {msPerCall} ms");
            }
        }


    }

}