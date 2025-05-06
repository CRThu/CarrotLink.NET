using NLog;

namespace CarrotLink.Logging.NLogLogger
{
    public class NLogWrapper : CarrotLink.Core.Logging.ILogger
    {
        private readonly NLog.Logger _logger;

        public NLogWrapper(bool toconsole = false, string? tofile = default)
        {
            NLog.LogManager.Setup().LoadConfiguration(builder =>
            {
                if (toconsole)
                {
                    builder.ForLogger().FilterMinLevel(LogLevel.Trace).WriteToConsole();
                }
                if (tofile != default)
                {
                    builder.ForLogger().FilterMinLevel(LogLevel.Trace).WriteToFile(tofile);
                }
            });

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
    }
}
