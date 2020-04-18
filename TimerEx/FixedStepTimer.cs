using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace TimerEx
{
    /// <summary>
    /// 固定周期でイベントを着火するタイマーです。
    /// 通常のタイマークラスを利用するよりもイベント着火毎のドリフトが少なくなっています。
    /// </summary>
    /// <remarks>
    /// 以下を参考にしました。
    /// https://stackoverflow.com/a/3250747
    /// </remarks>
    /// <example>
    /// <code>
    /// var t = new FixedStepTimer(TimeSpan.FromSeconds(2));
    /// t.Tick += FixedStepTimer_Tick;
    /// t.Start();
    ///
    /// ２秒周期でTickイベントが発生します
    ///
    /// t.Stop();
    /// t.Close();
    /// </code>
    /// </example>
    public class FixedStepTimer
    {
        /// <summary>
        /// <see cref="FixedStepTimer.Tick"/> イベントのイベント引数です。
        /// </summary>
        public class TickEventArgs : EventArgs
        {
            /// <summary>
            /// イベントが発生した時間
            /// </summary>
            public DateTime SignalTime { get; }
            
            /// <summary>
            /// <see cref="FixedStepTimer.Start"/> してから何回目のイベントかを示します。
            /// </summary>
            public long TickCount { get; }

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="signalTime">イベントが発生した時間</param>
            /// <param name="tickCount">何回目のイベントなのかを示す値</param>
            internal TickEventArgs(DateTime signalTime, long tickCount)
            {
                this.SignalTime = signalTime;
                this.TickCount = tickCount;
            }
        }

        /// <summary>
        /// タイマーが発動した場合に発生するイベントです。
        /// </summary>
        public event EventHandler<TickEventArgs> Tick;

        /// <summary>
        /// 内部で利用しているタイマーオブジェクト
        /// </summary>
        private readonly Timer _timer;
        
        /// <summary>
        /// インターバル (TimeSpan.Ticks の値)
        /// </summary>
        private readonly long _step;
        
        /// <summary>
        /// 次のタイマーイベントまでの時間 (TimeSpan.Ticks の値)
        /// </summary>
        private long _nextTick;
        
        /// <summary>
        /// 現在までの <see cref="FixedStepTimer.Tick"/> イベント発生回数
        /// </summary>
        private long _tickCount;
        
        /// <summary>
        /// <see cref="FixedStepTimer.Start"/> した際に即座に最初のイベントを発生させるかどうか
        /// </summary>
        private bool _firstCallRunImmediate;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="interval"></param>
        /// <exception cref="ArgumentOutOfRangeException">intervalが無効な値の場合</exception>
        public FixedStepTimer(TimeSpan interval)
        {
            if (interval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(interval));
            }

            this._timer = new Timer {AutoReset = true};
            this._timer.Elapsed += this.TimerOnElapsed;

            this._step = interval.Ticks;
        }

        /// <summary>
        /// <see cref="Start"/> してからTickイベントが発生した回数
        /// </summary>
        public long TickCount => Thread.VolatileRead(ref this._tickCount);
        
        /// <summary>
        /// 開始します。
        /// </summary>
        /// <param name="firstCallRunImmediate">開始と同時に最初のイベントを発生させるかどうか</param>
        /// <remarks>
        /// <see cref="firstCallRunImmediate"/>がtrueに指定されている場合
        /// 即座に最初のイベントが発生します。その後、コンストラクタにて指定したインターバル後に
        /// 周期イベントが発生します。falseを指定している場合は、インターバル後に最初のイベントが
        /// 発生します。
        /// </remarks>
        public void Start(bool firstCallRunImmediate = false)
        {
            this._firstCallRunImmediate = firstCallRunImmediate;

            var now = DateTime.Now;
            var nowTicks = now.Ticks;
            this._nextTick = nowTicks + (this._step - (this._nextTick % this._step));

            this._timer.Interval = this.CalcTimerInterval().TotalMilliseconds;
            this._timer.Start();

            if (this._firstCallRunImmediate)
            {
                Task.Run(async () =>
                {
                    await Task.Yield();

                    var count = Thread.VolatileRead(ref this._tickCount);
                    count += 1;
                    Thread.VolatileWrite(ref this._tickCount, count);

                    this.Tick?.Invoke(this, new TickEventArgs(now, count));
                });
            }
        }

        /// <summary>
        /// 停止します。
        /// </summary>
        public void Stop()
        {
            this._timer.Stop();
            this._nextTick = DateTime.Now.Ticks % this._step;
        }

        /// <summary>
        /// 本タイマーを閉じて、内部リソースも開放します。
        /// </summary>
        public void Close()
        {
            this._timer.Close();
        }
        
        /// <summary>
        /// 次のインターバルを計算して返します。
        /// </summary>
        /// <returns>次のインターバル</returns>
        private TimeSpan CalcTimerInterval()
        {
            var interval = this._nextTick - DateTime.Now.Ticks;
            if (interval > 0)
            {
                return new TimeSpan(interval);
            }

            return TimeSpan.Zero;
        }

        /// <summary>
        /// <see cref="System.Timers.Timer.Elapsed"/> が発生した際に呼ばれます。
        /// </summary>
        /// <param name="sender">イベント送信元</param>
        /// <param name="e">イベント引数</param>
        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            if (DateTime.Now.Ticks >= this._nextTick)
            {
                var count = Thread.VolatileRead(ref this._tickCount);
                count += 1;
                Thread.VolatileWrite(ref this._tickCount, count);

                this.Tick?.Invoke(this, new TickEventArgs(e.SignalTime, count));

                var next = Thread.VolatileRead(ref this._nextTick);
                next += this._step;
                Thread.VolatileWrite(ref this._nextTick, next);
            }

            var interval = this.CalcTimerInterval();
            this._timer.Interval = interval.TotalMilliseconds;
        }
    }
}