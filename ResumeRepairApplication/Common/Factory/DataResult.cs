namespace ResumeRepairApplication.Common
{
    public class DataResult
    {
        /// <summary>
        /// 创建请求成功的默认返回值
        /// </summary>
        public DataResult()
        {
            this.IsSuccess = true;
        }
        /// <summary>
        /// 根据错误信息创建请求失败的返回值
        /// </summary>
        /// <param name="errorMsg"></param>
        public DataResult(string errorMsg)
        {
            this.ErrorMsg = errorMsg;
        }
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }
        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMsg { get; set; }
    }
    /// <summary>
    /// Json请求基类
    /// </summary>
    public class DataResult<T> : DataResult
    {
        /// <summary>
        /// 创建请求成功的默认返回值
        /// </summary>
        public DataResult() : base() { }

        /// <summary>
        /// 创建请求失败的默认返回值
        /// </summary>
        public DataResult(string errorMsg) : base(errorMsg) { }

        /// <summary>
        /// 根据返回的数据创建请求成功的返回值
        /// </summary>
        /// <param name="data"></param>
        public DataResult(T data)
            : base()
        {
            this.Data = data;
        }
        /// <summary>
        /// 成功返回的数据
        /// </summary>
        public T Data { get; set; }
    }
}
