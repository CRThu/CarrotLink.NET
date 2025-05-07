using CarrotLink.Core.Protocols.Models;
using NLog;

namespace CarrotLink.Logging.NLogLogger
{
    /// <summary>
    /// TODO: multi devices not supported
    /// </summary>
    public class NLogWrapper : CarrotLink.Core.Logging.ILogger
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
                config.AddRule(LogLevel.Trace, LogLevel.Fatal, logconsole);
            }

            if (tofile != default)
            {
                var logfile = new NLog.Targets.FileTarget("logfile")
                {
                    FileName = tofile,
                    Layout = layout,
                };
                config.AddRule(LogLevel.Trace, LogLevel.Fatal, logfile);
            }
            NLog.LogManager.Configuration = config;     // initial

            _logger = LogManager.GetCurrentClassLogger();
        }

        public void LogDebug(string message)
        {
            _logger.Debug(message);
        }

        public void LogError(string message, Exception? ex = null)
        {
            _logger.Error(ex, message);
        }

        public void LogInfo(string message)
        {
            _logger.Info(message);
        }

        public void LogInfo(IPacket packet)
        {
            _logger.Info(packet);
        }
    }
}
