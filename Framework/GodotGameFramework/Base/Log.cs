//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using System.Diagnostics;

namespace GodotGameFramework
{
    /// <summary>
    /// 日志工具集。
    ///
    /// 对齐 UGF 的 Utility/Log.cs 设计：
    /// - 5 个日志级别：Debug / Info / Warning / Error / Fatal
    /// - 每个方法带有 [Conditional] 编译开关
    /// - 未定义对应编译符号时，调用方被编译器完全移除（零开销）
    /// - 所有日志委托给 GameFrameworkLog，通过 ILogHelper 管道统一输出
    ///
    /// 编译符号控制（与 UGF 一致）：
    /// - ENABLE_LOG           启用所有日志
    /// - ENABLE_DEBUG_LOG     启用 Debug 级别
    /// - ENABLE_INFO_LOG      启用 Info 级别
    /// - ENABLE_WARNING_LOG   启用 Warning 级别
    /// - ENABLE_ERROR_LOG     启用 Error 级别
    /// - ENABLE_FATAL_LOG     启用 Fatal 级别
    /// - ENABLE_DEBUG_AND_ABOVE_LOG   启用 Debug 及以上
    /// - ENABLE_INFO_AND_ABOVE_LOG    启用 Info 及以上
    /// - ENABLE_WARNING_AND_ABOVE_LOG 启用 Warning 及以上
    /// - ENABLE_ERROR_AND_ABOVE_LOG   启用 Error 及以上
    /// - ENABLE_FATAL_AND_ABOVE_LOG   启用 Fatal 及以上
    ///
    /// 对应 Unity 版本中的 Utility.Log。
    /// </summary>
    public static class Log
    {
        // ================================================================
        //  Debug 级别
        // ================================================================

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_DEBUG_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Debug(object message)
        {
            GameFrameworkLog.Debug(message);
        }

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_DEBUG_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Debug(string message)
        {
            GameFrameworkLog.Debug(message);
        }

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_DEBUG_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Debug<T>(string format, T arg)
        {
            GameFrameworkLog.Debug(format, arg);
        }

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_DEBUG_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Debug<T1, T2>(string format, T1 arg1, T2 arg2)
        {
            GameFrameworkLog.Debug(format, arg1, arg2);
        }

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_DEBUG_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        public static void Debug<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3)
        {
            GameFrameworkLog.Debug(format, arg1, arg2, arg3);
        }

        // ================================================================
        //  Info 级别
        // ================================================================

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_INFO_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        public static void Info(object message)
        {
            GameFrameworkLog.Info(message);
        }

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_INFO_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        public static void Info(string message)
        {
            GameFrameworkLog.Info(message);
        }

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_INFO_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        public static void Info<T>(string format, T arg)
        {
            GameFrameworkLog.Info(format, arg);
        }

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_INFO_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        public static void Info<T1, T2>(string format, T1 arg1, T2 arg2)
        {
            GameFrameworkLog.Info(format, arg1, arg2);
        }

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_INFO_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        public static void Info<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3)
        {
            GameFrameworkLog.Info(format, arg1, arg2, arg3);
        }

        // ================================================================
        //  Warning 级别
        // ================================================================

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_WARNING_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        public static void Warning(object message)
        {
            GameFrameworkLog.Warning(message);
        }

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_WARNING_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        public static void Warning(string message)
        {
            GameFrameworkLog.Warning(message);
        }

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_WARNING_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        public static void Warning<T>(string format, T arg)
        {
            GameFrameworkLog.Warning(format, arg);
        }

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_WARNING_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        public static void Warning<T1, T2>(string format, T1 arg1, T2 arg2)
        {
            GameFrameworkLog.Warning(format, arg1, arg2);
        }

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_WARNING_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        public static void Warning<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3)
        {
            GameFrameworkLog.Warning(format, arg1, arg2, arg3);
        }

        // ================================================================
        //  Error 级别
        // ================================================================

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_ERROR_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        public static void Error(object message)
        {
            GameFrameworkLog.Error(message);
        }

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_ERROR_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        public static void Error(string message)
        {
            GameFrameworkLog.Error(message);
        }

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_ERROR_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        public static void Error<T>(string format, T arg)
        {
            GameFrameworkLog.Error(format, arg);
        }

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_ERROR_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        public static void Error<T1, T2>(string format, T1 arg1, T2 arg2)
        {
            GameFrameworkLog.Error(format, arg1, arg2);
        }

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_ERROR_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        public static void Error<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3)
        {
            GameFrameworkLog.Error(format, arg1, arg2, arg3);
        }

        // ================================================================
        //  Fatal 级别
        // ================================================================

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_FATAL_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        [Conditional("ENABLE_FATAL_AND_ABOVE_LOG")]
        public static void Fatal(object message)
        {
            GameFrameworkLog.Fatal(message);
        }

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_FATAL_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        [Conditional("ENABLE_FATAL_AND_ABOVE_LOG")]
        public static void Fatal(string message)
        {
            GameFrameworkLog.Fatal(message);
        }

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_FATAL_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        [Conditional("ENABLE_FATAL_AND_ABOVE_LOG")]
        public static void Fatal<T>(string format, T arg)
        {
            GameFrameworkLog.Fatal(format, arg);
        }

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_FATAL_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        [Conditional("ENABLE_FATAL_AND_ABOVE_LOG")]
        public static void Fatal<T1, T2>(string format, T1 arg1, T2 arg2)
        {
            GameFrameworkLog.Fatal(format, arg1, arg2);
        }

        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_FATAL_LOG")]
        [Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
        [Conditional("ENABLE_INFO_AND_ABOVE_LOG")]
        [Conditional("ENABLE_WARNING_AND_ABOVE_LOG")]
        [Conditional("ENABLE_ERROR_AND_ABOVE_LOG")]
        [Conditional("ENABLE_FATAL_AND_ABOVE_LOG")]
        public static void Fatal<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3)
        {
            GameFrameworkLog.Fatal(format, arg1, arg2, arg3);
        }
    }
}
