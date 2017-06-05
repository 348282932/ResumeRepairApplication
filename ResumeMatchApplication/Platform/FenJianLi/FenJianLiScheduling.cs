using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ResumeMatchApplication.Common;
using ResumeMatchApplication.Models;

namespace ResumeMatchApplication.Platform.FenJianLi
{
    /// <summary>
    /// 招聘狗调度
    /// </summary>
    public class FenJianLiScheduling
    {
        public static List<FenJianLiScheduling> services = new List<FenJianLiScheduling>();

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

        /// <summary>
        /// 开始任务
        /// </summary>
        public static void Run()
        {
            // 任务初始化

            var workLists = new List<Tuple<Func<DataResult>, int, string, bool>>
            {
                new Tuple<Func<DataResult>, int, string, bool>(new RegisterSpider().Init, 5 * 1000, "注册", false),
                new Tuple<Func<DataResult>, int, string, bool>(new ActivationSpider().Init, 10 * 1000, "激活", false)
                //new Tuple<Func<DataResult>, int, string, bool>(new ContactInformationSpider().Init, 10 * 1000, "下载", false),
            };

            var threadCount = 0;

            while (threadCount < Global.ThreadCount)
            {
                Parallel.ForEach(workLists,
                    new ParallelOptions { MaxDegreeOfParallelism = 1 },
                    task =>
                    {
                        var service = new FenJianLiScheduling { _taskAction = task.Item1, _interval = task.Item2, _taskName = task.Item3, _isInitStart = task.Item4 };

                        service.DoWork();

                        services.Add(service);
                    });

                ++threadCount;
            }
        }

        /// <summary>
        /// 停止任务
        /// </summary>
        public static void Stop(string taskName = "")
        {
            if (!string.IsNullOrWhiteSpace(taskName))
            {
                var tasks = services.Where(w => w._taskName == taskName);

                foreach (var task in tasks)
                {
                    task._workTimer.Stop();

                    task._workTimer.Close();
                }
            }
            else
            {
                foreach (var item in services)
                {
                    item._workTimer.Stop();

                    item._workTimer.Close();
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
                var tasks = services.Where(w => w._taskName == taskName);

                foreach (var task in tasks)
                {
                    task._workTimer.Start();
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
            _workTimer = new System.Timers.Timer
            {
                Interval = 1000,
                AutoReset = false
            };

            _workTimer.Elapsed += TimerHanlder;

            if(_isInitStart) _workTimer.Start(); // 初始化执行
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

                const int interval = 1000; // 处理失败，1秒后重试

                const int maxTimes = 0; // 最大重试次数

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
                    LogFactory.Error(dataResult.ErrorMsg, MessageSubjectEnum.FenJianLi); ;
                }

                _workTimer.Interval = _interval;

                //if(!isStop)_workTimer.Start();
            }
        }
    }
}
