using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Configuration;

namespace Common
{
    public class Log
    {
        private readonly object syncLock = new object();
        private LogLevel _loggingLevel;
        private List<LogItem> _logData;
        private ConcurrentQueue<LogItem> _unreadLogEvents;
        public LogLevel LoggingLevel;
        Action<string> _updateLogCallback;
        System.Timers.Timer _timer;
        int _timerTimeout;

        string _title;
        string _fileName;
        string FileName
        {
            get { return _fileName; }
        }

        /// <summary>
        /// updateLogCallback will be called every "updateSeconds" with the logging data data
        /// </summary>
        /// <param name="updateLogCallback"></param>
        /// <param name="updateMilliseconds"></param>
        public Log(Action<string> updateLogCallback, int updateMilliseconds, string title)
        {
            _title = title;
            InitLog();
            LoggingLevel = LogLevel.DATA;
            _updateLogCallback = updateLogCallback;
            _logData = new List<LogItem>();
            _unreadLogEvents = new ConcurrentQueue<LogItem>();
            _timerTimeout = updateMilliseconds;
            _timer = new System.Timers.Timer();
            _timer.Interval = updateMilliseconds;
            _timer.Elapsed += timerCallBack;
        }

        public List<LogItem> LogData
        {
            get { return _logData; }
        }

        public void Flush()
        {
            timerCallBack(null, null);
            if(_timer.Enabled) _timer.Stop();
        }

        public Action<String> UpdateLogCallback
        {
            get
            {
                return _updateLogCallback;
            } set
            {
                _updateLogCallback = value;
            }
        }

        int _logFileErrorCount = 0;
        private void timerCallBack(object sender, ElapsedEventArgs e)
        {
            if (_updateLogCallback == null) _timer.Stop();

            if (_unreadLogEvents.Count == 0)
            {
                _timer.Stop();
                return;
            }

            if (_unreadLogEvents.Count <= 0) return;

            //This should never have a deadlock unless we are in debug mode or the thread gets held up, lock it anyway for good practice
            lock (syncLock)
            {

                string message = "";
                while (_unreadLogEvents.Count > 0)
                {
                    LogItem result;
                    if (!_unreadLogEvents.TryDequeue(out result)) break;
                    message += result.ToString();
                }

                if (string.IsNullOrEmpty(message)) return;

                _updateLogCallback(message);

                //If we get multiple errors give up
                if (_logFileErrorCount < 5)
                {
                    try
                    {
                        using (var stream = new StreamWriter(_fileName, true))
                        {
                            stream.Write(message);
                            stream.Flush();
                        }
                        _logFileErrorCount = 0;
                    }
                    catch (Exception ex)
                    {
                        _updateLogCallback("Error opening/creating log file: " + _fileName + " due to: " + ex.Message + Environment.NewLine);
                        _logFileErrorCount++;
                        if (_logFileErrorCount >= 5) _updateLogCallback("Too many log file errors, disabling log file for this session." + Environment.NewLine);
                    } 
                }
            }
        }

        private void InitLog()
        {
            var location = Assembly.GetExecutingAssembly().Location;

            var config = ConfigurationManager.OpenExeConfiguration(location);

            var configValue = config.AppSettings.Settings["FileName"];

            if (configValue != null)
            {
                _fileName = configValue.Value;
            }

            if (string.IsNullOrEmpty(_fileName))
            {
                _fileName = string.Format(Path.Combine(Path.GetDirectoryName(location),
                    string.Format($"{DateTime.Now.ToShortDateString().Replace("/", "-") + " " + DateTime.Now.ToLongTimeString().Replace(":","-")} - {_title}.txt")));
            }
        }

        public void WriteLine(string msg)
        {
            LogItem logItem = new LogItem(msg + Environment.NewLine, LogLevel.INFORMATION, null);
            _logData.Add(logItem);
            _unreadLogEvents.Enqueue(logItem);

            if (!_timer.Enabled && _updateLogCallback != null)
            {
                _timer.Start();
                timerCallBack(null, null);
            }
        }


        public void WriteLineWithTimeStamp(string msg)
        {
            LogItem logItem = new LogItem(msg + Environment.NewLine, LogLevel.INFORMATION, DateTime.Now);
            _logData.Add(logItem);
            _unreadLogEvents.Enqueue(logItem);

            if (!_timer.Enabled && _updateLogCallback != null)
            {
                _timer.Start();
                timerCallBack(null, null);
            }
        }

        public void Write(string msg)
        {
            LogItem logItem = new LogItem(msg, LogLevel.INFORMATION, null);
            _logData.Add(logItem);
            _unreadLogEvents.Enqueue(logItem);

            if (!_timer.Enabled && _updateLogCallback != null)
            {
                _timer.Start();
                timerCallBack(null, null);
            }
        }


        public void WriteWithTimeStamp(string msg)
        {
            LogItem logItem = new LogItem(msg, LogLevel.INFORMATION, DateTime.Now);
            _logData.Add(logItem);
            _unreadLogEvents.Enqueue(logItem);

            if (!_timer.Enabled && _updateLogCallback != null)
            {
                _timer.Start();
                timerCallBack(null, null);
            }
        }

        public enum LogLevel
        {
            DATA = 0,
            INFORMATION = 1,
            WARNING = 2,
            ERROR = 3,
            CRITICAL_ERROR = 4
        }

        public class LogItem
        {
            //Arbitary container for data
            //public PassThruMsg Data;
            LogLevel LogLevel;
            string Message;
            DateTime? TimeStamp;

            public LogItem(string msg, LogLevel logLevel, DateTime? timeStamp) { 
                TimeStamp = timeStamp;
                LogLevel = logLevel;
                Message = msg;
            }

            public override string ToString()
            {
                
                DateTime updatedTimeStamp = TimeStamp ?? DateTime.MinValue;
                bool validTimeStamp = !DateTime.MinValue.Equals(updatedTimeStamp);

                StringBuilder sb = new StringBuilder();
                if (validTimeStamp) sb.Append(updatedTimeStamp.ToLongTimeString() + " ");

                sb.Append(Message);
                return sb.ToString();
            }
        }
    }
}
