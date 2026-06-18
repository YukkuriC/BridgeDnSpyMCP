// 生成于 GLM-5V-Turbo

using System;

namespace BDSM
{
    /// <summary>
    /// 用户使用错误异常（参数错误、资源不存在等）。
    /// 捕获后仅输出 Message，不输出类名和堆栈跟踪。
    /// </summary>
    public class UserException : Exception
    {
        public UserException(string message) : base(message) { }
    }
}
