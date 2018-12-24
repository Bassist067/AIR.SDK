using System;
using System.Diagnostics;
using System.Reflection;

namespace AIR.SDK.Workflow
{
	////[Serializable]
	internal class LoggerRequest
	{
		//internal string LogMessage;
		internal StackFrame CurrentFrame;
		internal MethodBase CurrentMethod;
		internal string MethodName;
		internal string ClassName;
		internal string FileName;
		internal int LineNumber;

		/// <summary>
		/// Dumps data. 
		/// </summary>
		/// <returns></returns>
		internal string dd()
		{
			return $"{ClassName}.{MethodName}\n";
		}
	}

	internal class Logger
	{
		private static NLog.Logger _logger = NLog.LogManager.GetLogger("AWS.SWF");

		private static string Request
		{
			get
			{
				var request = new LoggerRequest();

				StackTrace trace = new StackTrace(new Exception(), true);

				try
				{
					if (trace.GetFrames()[1] != null)
					{
						request.CurrentFrame = trace.GetFrames()[1];
						request.CurrentMethod = request.CurrentFrame.GetMethod();
						request.MethodName = request.CurrentMethod.Name;
						request.ClassName = request.CurrentMethod.DeclaringType.Name;
						request.FileName = request.CurrentFrame.GetFileName().Substring(request.CurrentFrame.GetFileName().LastIndexOf("\\") + 1);
						request.LineNumber = request.CurrentFrame.GetFileLineNumber();
					}
				}
				catch (Exception)
				{
					// DO NOTHING
				}

				return request.dd();
			}
		}

		internal static void Debug(string message)
		{
			_logger.Debug(Request + message);
		}
		internal static void Debug(string message, params object[] args)
		{
			_logger.Debug(Request + message, args);
		}
		internal static void Debug(Exception exception, string message, params object[] args)
		{
			_logger.Debug(exception, Request + message, args);
		}

		internal static void Error(string message)
		{
			_logger.Error(Request + message);
		}
		internal static void Error(string message, params object[] args)
		{
			_logger.Error(Request + message, args);
		}
		internal static void Error(Exception exception, string message, params object[] args)
		{
			_logger.Error(exception, Request + message, args);
		}

		internal static void Fatal(string message)
		{
			_logger.Fatal(Request + message);
		}
		internal static void Fatal(string message, params object[] args)
		{
			_logger.Fatal(Request + message, args);
		}
		internal static void Fatal(Exception exception, string message, params object[] args)
		{
			_logger.Fatal(exception, Request + message, args);
		}

		internal static void Info(string message)
		{
			_logger.Info(Request + message);
		}
		internal static void Info(string message, params object[] args)
		{
			_logger.Info(Request + message, args);
		}
		internal static void Info(Exception exception, string message, params object[] args)
		{
			_logger.Info(exception, Request + message, args);
		}

		internal static void Trace(string message)
		{
			_logger.Trace(Request + message);
		}
		internal static void Trace(string message, params object[] args)
		{
			_logger.Trace(Request + message, args);
		}
		internal static void Trace(Exception exception, string message, params object[] args)
		{
			_logger.Trace(exception, Request + message, args);
		}

		internal static void Warn(string message)
		{
			_logger.Warn(Request + message);
		}
		internal static void Warn(string message, params object[] args)
		{
			_logger.Warn(Request + message, args);
		}
		internal static void Warn(Exception exception, string message, params object[] args)
		{
			_logger.Warn(exception, Request + message, args);
		}
	}
}
