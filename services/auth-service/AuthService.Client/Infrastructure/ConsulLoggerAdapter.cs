// namespace AuthService.Client.Infrastructure;
//
// public class SerilogToMicrosoftLoggerAdapter<T> : ILogger<T>
// {
//     private readonly Serilog.ILogger _serilogLogger;
//
//     public SerilogToMicrosoftLoggerAdapter(Serilog.ILogger serilogLogger)
//     {
//         _serilogLogger = serilogLogger.ForContext<T>();
//     }
//
//     public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;
//
//     public bool IsEnabled(LogLevel logLevel) => true;
//
//     public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
//     {
//         if (!IsEnabled(logLevel))
//             return;
//
//         var message = formatter(state, exception);
//
//         switch (logLevel)
//         {
//             case LogLevel.Trace:
//             case LogLevel.Debug:
//                 _serilogLogger.Debug(exception, message);
//                 break;
//             case LogLevel.Information:
//                 _serilogLogger.Information(exception, message);
//                 break;
//             case LogLevel.Warning:
//                 _serilogLogger.Warning(exception, message);
//                 break;
//             case LogLevel.Error:
//                 _serilogLogger.Error(exception, message);
//                 break;
//             case LogLevel.Critical:
//                 _serilogLogger.Fatal(exception, message);
//                 break;
//             default:
//                 _serilogLogger.Information(exception, message);
//                 break;
//         }
//     }
// }