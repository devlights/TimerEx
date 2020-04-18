using System;
using System.Linq;

namespace TimerEx
{
    /// <summary>
    /// 指定された条件に合致するシステム時間にイベントを着火するタイマーです。
    /// </summary>
    /// <remarks>
    /// 以下を参考にしました。
    /// https://stackoverflow.com/a/10896628
    /// </remarks>
    /// <example>
    /// <code>
    /// var t = new SystemClockTimer(SystemClockTimer.TickAtEvery.Minute, new []{30, 35, 40, 55});
    /// t.Tick += SysClockTimer_Tick;
    /// t.Start();
    ///
    /// 毎分 30,35,40,55秒にTickイベントが発生します
    ///
    /// t.Stop();
    /// t.Close();
    /// </code>
    /// </example>
    public class SystemClockTimer
    {
        /// <summary>
        /// <see cref="SystemClockTimer.Tick"/> イベントのイベント引数です。
        /// </summary>
        public class TickEventArgs : EventArgs
        {
            /// <summary>
            /// イベントが発生した時間
            /// </summary>
            public DateTime SignalTime { get; }

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="signalTime">イベントが発生した時間</param>
            internal TickEventArgs(DateTime signalTime)
            {
                this.SignalTime = signalTime;
            }
        }

        /// <summary>
        /// どの時間単位で <see cref="SystemClockTimer.Tick"/> を発生させるかを表します。
        /// </summary>
        public enum TickAtEvery
        {
            /// <summary>
            /// 毎時
            /// </summary>
            Hour,

            /// <summary>
            /// 毎分
            /// </summary>
            Minute
        }

        /// <summary>
        /// タイマーが発動した場合に発生するイベントです。
        /// </summary>
        public event EventHandler<TickEventArgs> Tick;

        /// <summary>
        /// 内部で利用しているタイマーオブジェクト
        /// </summary>
        private readonly FixedStepTimer _timer;

        /// <summary>
        /// 直近で発生した <see cref="Tick"/> イベントの時間
        /// </summary>
        private volatile string _last;

        /// <summary>
        /// 最小値を表す
        /// </summary>
        /// <remarks>
        /// 初回のイベント発生かどうかを判別するために利用されています。
        /// </remarks>
        private readonly string _minLast;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="every">時間単位</param>
        /// <param name="values">時間単位内でイベントを発生させたい値のリスト</param>
        /// <remarks>
        /// たとえば、毎時１０分でイベントを発生させたい場合は
        /// <code>
        /// var t = new SystemClockTimer(SystemClockTimer.TickAtEvery.Hour, new []{10});
        /// </code>
        /// と指定します。
        ///
        /// 毎分 30,35,40,55秒でイベントを発生させたい場合は
        /// <code>
        /// var t = new SystemClockTimer(SystemClockTimer.TickAtEvery.Minute, new []{30,35,40,55});
        /// </code>
        /// と指定します。
        /// </remarks>
        public SystemClockTimer(TickAtEvery every, int[] values)
        {
            this.Every = every;
            this.Values = values;
            this._timer = new FixedStepTimer(TimeSpan.FromMilliseconds(1000));
            this._timer.Tick += this.TimerOnTick;
            this._minLast = DateTime.MinValue.ToString("HH:mm:ss");
        }

        /// <summary>
        /// 時間単位
        /// </summary>
        public TickAtEvery Every { get; }

        /// <summary>
        /// 時間単位内でイベントを発生させたい時間の値
        /// </summary>
        public int[] Values { get; }

        /// <summary>
        /// 開始します。
        /// </summary>
        public void Start()
        {
            this._timer.Start();
        }

        /// <summary>
        /// 停止します。
        /// </summary>
        public void Stop()
        {
            this._timer.Stop();
        }

        /// <summary>
        /// 本タイマーを閉じて、内部リソースも開放します。
        /// </summary>
        public void Close()
        {
            this._timer.Close();
        }

        /// <summary>
        /// <see cref="FixedStepTimer.Tick"/> イベントが発生した際に呼ばれます。
        /// </summary>
        /// <param name="sender">イベント送信元</param>
        /// <param name="e">イベント引数</param>
        private void TimerOnTick(object sender, FixedStepTimer.TickEventArgs e)
        {
            var sigTime = e.SignalTime;
            var time =
                new DateTime(1, 1, 1, sigTime.Hour, sigTime.Minute, sigTime.Second);

            Func<DateTime, bool> fn = d => this.Values.Contains(d.Minute);
            var timeStr = time.ToString("HH:mm:00");
            if (this.Every == TickAtEvery.Minute)
            {
                fn = d => this.Values.Contains(d.Second);
                timeStr = time.ToString("HH:mm:ss");
            }

            if (!fn(time))
            {
                return;
            }

            if (timeStr != this._minLast && timeStr == this._last)
            {
                return;
            }

            this._last = timeStr;
            this.Tick.Invoke(this, new TickEventArgs(sigTime));
        }
    }
}