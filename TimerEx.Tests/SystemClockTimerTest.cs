using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TimerEx.Tests
{
    [TestClass]
    public class SystemClockTimerTest
    {
        [TestMethod]
        public async Task TestTimer()
        {
            var values = new[] {30, 35, 40, 55};
            var t = new SystemClockTimer(SystemClockTimer.TickAtEvery.Minute, values);

            var results = new List<DateTime>();
            t.Tick += (s, e) =>
            {
                var d = e.SignalTime.ToString("HH:mm:ss");
                Trace.WriteLine($"[Tick] {d}");
                
                results.Add(e.SignalTime);
            };

            try
            {
                t.Start();
                await Task.Delay(TimeSpan.FromMinutes(1));
                t.Stop();
            }
            finally
            {
                t.Close();                
            }

            foreach (var d in results)
            {
                var sec = d.Second;
                if (!values.Contains(sec))
                {
                    Assert.Fail($"{sec} not in {values}");
                }
            }
        }
    }
}