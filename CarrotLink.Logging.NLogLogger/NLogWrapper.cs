using CarrotLink.Core.Logging;
using CarrotLink.Core.Protocols.Models;
using NLog;
using NLog.Targets.Wrappers;

namespace CarrotLink.Logging.NLogLogger
{
    /// <summary>
    /// TODO: multi devices not supported
    /// </summary>
    public class NLogWrapper : CarrotLink.Core.Logging.ILogger, CarrotLink.Core.Logging.IPacketLogger
    {
        private readonly NLog.Logger _logger;

        public NLogWrapper(bool toconsole = false, string? tofile = default)
        {
            string layout = "${longdate}|${level:uppercase=true}|${message:withexception=true}";
            var config = new NLog.Config.LoggingConfiguration();

            if (toconsole)
            {
                var logconsole = new NLog.Targets.ColoredConsoleTarget("logconsole")
                {
                    Layout = layout,
                };
                var asyncLogConsole = new AsyncTargetWrapper(logconsole)
                {
                    TimeToSleepBetweenBatches = 100,
                    QueueLimit = 1000000,
                    OverflowAction = AsyncTargetWrapperOverflowAction.Block,
                };
                config.AddRule(LogLevel.Trace, LogLevel.Fatal, asyncLogConsole);
            }

            if (tofile != default)
            {
                var logfile = new NLog.Targets.FileTarget("logfile")
                {
                    FileName = tofile,
                    Layout = layout,
                    DeleteOldFileOnStartup = true,   // delete log on startup
                    KeepFileOpen = true,
                    BufferSize = 1 * 1024 * 1024,
                };
                var asyncLogFile = new AsyncTargetWrapper(logfile)
                {
                    TimeToSleepBetweenBatches = 100,
                    QueueLimit = 1000000,
                    OverflowAction = AsyncTargetWrapperOverflowAction.Grow,
                };
                config.AddRule(LogLevel.Trace, LogLevel.Fatal, asyncLogFile);
            }
            NLog.LogManager.Configuration = config;     // initial

            _logger = LogManager.GetCurrentClassLogger();
        }

        public void HandlePacket(IPacket packet)
        {
            _logger.Info(packet.ToString());
        }

        public void HandleRuntime(string message, LoggerLevel level = LoggerLevel.Info, Exception ex = null)
        {
            switch (level)
            {
                case LoggerLevel.Debug:
                    _logger.Debug(message);
                    break;
                case LoggerLevel.Info:
                    _logger.Info(message);
                    break;
                case LoggerLevel.Warn:
                    _logger.Warn(message);
                    break;
                case LoggerLevel.Error:
                    _logger.Error(ex, message);
                    break;

            }
        }
        public void Dispose()
        {
        }
    }
}
