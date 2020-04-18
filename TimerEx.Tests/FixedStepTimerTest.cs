using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TimerEx.Tests
{
    [TestClass]
    public class FixedStepTimerTest
    {
        [TestMethod]
        public void TestTimerIntervalInvalid()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                new FixedStepTimer(TimeSpan.Zero);
            });
        }
        
        [TestMethod]
        public async Task TestTimer1sInterval()
        {
            const int interval = 1;
            var sut = new FixedStepTimer(TimeSpan.FromSeconds(interval));

            var tickResults = new List<DateTime>();
            var firstSignalTime = DateTime.MinValue;
            sut.Tick += (s, e) =>
            {
                var signalTime = new DateTime(
                    1,1,1,
                    e.SignalTime.Hour,
                    e.SignalTime.Minute,
                    e.SignalTime.Second
                );
                
                if (e.TickCount == 1)
                {
                    firstSignalTime = signalTime;
                }
                
                tickResults.Add(signalTime);
            };

            try
            {
                sut.Start(true);
                await Task.Delay(TimeSpan.FromSeconds(10));
                sut.Stop();
            }
            finally
            {
                sut.Close();
            }

            for (var i = 0; i < tickResults.Count; i++)
            {
                var want = firstSignalTime.AddSeconds(interval*i);
                var got = tickResults[i];
                
                var w = want.ToString("HH:mm:ss.fff");
                var g = got.ToString("HH:mm:ss.fff");
                Trace.WriteLine($"want:[{w}]\tgot:[{g}]");
                
                Assert.AreEqual(want, got);
            }
            
            Assert.AreEqual(11, sut.TickCount);
        }
        
        [TestMethod]
        public async Task TestTimer100msInterval()
        {
            const int interval = 100;
            var sut = new FixedStepTimer(TimeSpan.FromMilliseconds(interval));

            var tickResults = new List<DateTime>();
            var firstSignalTime = DateTime.MinValue;
            sut.Tick += (s, e) =>
            {
                var signalTime = new DateTime(
                    1,1,1,
                    e.SignalTime.Hour,
                    e.SignalTime.Minute,
                    e.SignalTime.Second,
                    e.SignalTime.Millisecond
                );
                
                if (e.TickCount == 1)
                {
                    firstSignalTime = signalTime;
                }
                
                tickResults.Add(signalTime);
            };

            try
            {
                sut.Start(true);
                await Task.Delay(TimeSpan.FromSeconds(1));
                sut.Stop();
            }
            finally
            {
                sut.Close();
            }

            const int tolerance = 10;
            for (var i = 0; i < tickResults.Count; i++)
            {
                var want = firstSignalTime.AddMilliseconds(interval*i);
                var got = tickResults[i];

                var w = want.ToString("HH:mm:ss.fff");
                var g = got.ToString("HH:mm:ss.fff");
                var diff = got - want;
                Trace.WriteLine($"want:[{w}]\tgot:[{g}] {diff.Milliseconds}ms");

                var wantMillisecond = want.Millisecond;
                var gotMillisecond = got.Millisecond;

                var lowerLimit = wantMillisecond;
                var upperLimit = wantMillisecond + tolerance;

                bool ok = lowerLimit <= gotMillisecond && gotMillisecond <= upperLimit;
                if (!ok)
                {
                    Assert.Fail($"want:[{w}]\tgot:[{g}]");
                }
            }
            
            Assert.AreEqual(11, sut.TickCount);
        }
    }
}
