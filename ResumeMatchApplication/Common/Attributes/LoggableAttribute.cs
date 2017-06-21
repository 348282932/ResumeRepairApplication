using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using log4net;
using Newtonsoft.Json;
using PostSharp.Aspects;
using PostSharp.Extensibility;
using PostSharp.Serialization;

namespace ResumeMatchApplication.Common
{
    [PSerializable]
    public class LoggableAttribute : OnMethodBoundaryAspect
    {
        private static readonly ILog _logger;

        private string _methodName;

        [NonSerialized]private int _hashCode;

        static LoggableAttribute()
        {
            if (!PostSharpEnvironment.IsPostSharpRunning)
            {
                _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            }
        }

        public override void CompileTimeInitialize(MethodBase method, AspectInfo aspectInfo)
        {
            _methodName = method.DeclaringType?.Name + "." + method.Name;
        }

        public override void RuntimeInitialize(MethodBase method)
        {
            _hashCode = GetHashCode();
        }

        /// <summary>
        /// 方法进入的时候执行
        /// </summary>
        /// <param name="args"></param>
        public override void OnEntry(MethodExecutionArgs args)
        {
            var parameters = args.Method.GetParameters();

            var arguments = args.Arguments;

            var dictionary = new Dictionary<string, string>();

            for (var i = 0; arguments != null && i < parameters.Length; i++)
            {
                dictionary[parameters[i].Name] = JsonConvert.SerializeObject(arguments[i]);
            }

            _logger.DebugFormat(">>> Entry [{0}] {1} {2}", _hashCode, _methodName, JsonConvert.SerializeObject(dictionary));
        }

        /// <summary>
        /// 方法退出的时候执行
        /// </summary>
        /// <param name="args"></param>
        public override void OnExit(MethodExecutionArgs args)
        {
            _logger.DebugFormat("<<< Exit [{0}] {1} {2}", _hashCode, _methodName, JsonConvert.SerializeObject(args.ReturnValue));
        }

        /// <summary>
        /// 方法异常的时候执行
        /// </summary>
        /// <param name="args"></param>
        public override void OnException(MethodExecutionArgs args)
        {
            var sb = new StringBuilder();

            sb.AppendLine();
            
            sb.AppendLine("异常类名：" + args.Method.DeclaringType?.FullName);

            sb.AppendLine("异常方法：" + args.Method.Name);

            sb.AppendLine("异常信息：" + args.Exception);

            sb.Append("=========================================================================================");

            sb.AppendLine("=====================================================================================");

            args.FlowBehavior = FlowBehavior.Continue;

            LogFactory.Error(sb.ToString());

            //_logger.Error(sb.ToString());

            //throw args.Exception;
        }
    }
}