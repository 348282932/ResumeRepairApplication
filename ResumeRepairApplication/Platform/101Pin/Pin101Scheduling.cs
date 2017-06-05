using ResumeRepairApplication._101Pin;
using ResumeRepairApplication.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ResumeRepairApplication.Platform._101Pin
{
    class Pin101Scheduling
    {
        private System.Timers.Timer _workTimer;

        /// <summary>
        /// 定时周期（毫秒）
        /// </summary>
        private int _interval { get; set; }

        /// <summary>
        /// 调用方法
        /// </summary>
        private Func<DataResult> _taskAction { get; set; }

        /// <summary>
        /// 是否初始化启动
        /// </summary>
        private bool _isInitStart { get; set; }

        /// <summary>
        /// 任务名称
        /// </summary>
        public string _taskName { get; set; }

        public static List<Pin101Scheduling> services = new List<Pin101Scheduling>();

        public static SchedulingShowForm ssf;

        public static CheckNumberForm cnf;

        /// <summary>
        /// 开始任务
        /// </summary>
        public static void Start(SchedulingShowForm _ssf, CheckNumberForm _cnf)
        {
            // 任务初始化

            ssf = _ssf;

            cnf = _cnf;

            new Thread(() =>
            {
                var workLists = new List<Tuple<Func<DataResult>, int, string, bool>>
                {
                    new Tuple<Func<DataResult>, int, string, bool>(new RegisterSpider().Init, 60 * 60 * 1000, "注册", true),
                    //new Tuple<Func<DataResult>, int, string, bool>(new LoginSpider().Init, 60 * 60 * 1000, "签到",true),
                    //new Tuple<Func<DataResult>, int, string, bool>(new ActivationSpider().Init, 60 * 60 * 1000, "激活",true)
				};

                Parallel.ForEach(workLists,
                    new ParallelOptions { MaxDegreeOfParallelism = 1 },
                    task =>
                    {
                        var service = new Pin101Scheduling { _taskAction = task.Item1, _interval = task.Item2, _taskName = task.Item3, _isInitStart = task.Item4 };

                        service.DoWork();

                        services.Add(service);
                    });
            })
            {
                IsBackground = true

            }.Start();
        }

        /// <summary>
        /// 停止任务
        /// </summary>
        public static void Stop(string taskName = "")
        {
            if (!string.IsNullOrWhiteSpace(taskName))
            {
                var item = services.FirstOrDefault(w => w._taskName == taskName);

                if (item != null)
                {
                    item._workTimer.Stop();
                    item._workTimer.Close();
                }
            }
            else
            {
                foreach (var item in services)
                {
                    item._workTimer.Stop();
                }
            }

        }

        /// <summary>
        /// 开始任务
        /// </summary>
        /// <param name="taskName"></param>
        public static void Start(string taskName = "")
        {
            if (!string.IsNullOrWhiteSpace(taskName))
            {
                var item = services.FirstOrDefault(w => w._taskName == taskName);

                if (item != null)
                {
                    item._workTimer.Start();
                }
            }
            else
            {
                foreach (var item in services)
                {
                    item._workTimer.Start();
                }
            }
        }

        /// <summary>
        /// 通过计时器创建任务
        /// </summary>
        public void DoWork()
        {
            _workTimer = new System.Timers.Timer();
            _workTimer.Interval = 1000;
            _workTimer.AutoReset = false;
            _workTimer.Elapsed += TimerHanlder;
            if (_isInitStart) _workTimer.Start(); // 初始化执行
        }

        /// <summary>
        /// 任务实现
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimerHanlder(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (_workTimer)
            {
                //_workTimer.Stop();

                var interval = 1000; // 处理失败，1秒后重试

                var maxTimes = 0; // 最大重试次数

                var retryTimes = 0;

                var dataResult = new DataResult();

                do
                {
                    try
                    {
                        dataResult = _taskAction.Invoke();
                    }
                    catch (Exception ex)
                    {
                        dataResult.IsSuccess = false;

                        dataResult.ErrorMsg = string.Format("{5}纷简历 平台{0}定时任务程序异常！{1}异常消息：{2}{3} 堆栈异常信息：{4}", _taskName, Environment.NewLine, ex.Message, Environment.NewLine, ex.StackTrace, Environment.NewLine);
                    }

                    if (dataResult.IsSuccess)
                    {
                        retryTimes = maxTimes + 1;
                    }
                    else
                    {
                        retryTimes++;

                        Thread.Sleep(interval);
                    }
                }
                while (retryTimes < maxTimes + 1);

                if (!dataResult.IsSuccess)
                {
                    ssf.SetText(ssf.system_tbx_Exception, dataResult.ErrorMsg + Environment.NewLine);
                }

                _workTimer.Interval = _interval;

                //if(!isStop)_workTimer.Start();
            }
        }
    }
}

