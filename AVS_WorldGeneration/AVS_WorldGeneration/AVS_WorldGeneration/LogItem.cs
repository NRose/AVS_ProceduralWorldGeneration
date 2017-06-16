using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace AVS_WorldGeneration
{
    public class LogItem
    {
        private string m_sLogMessage;
        private Brush m_kLogColor;
        private LogLevel m_eLogLevel;

        public string sLogMessage
        {
            get { return m_sLogMessage; }
            set { m_sLogMessage = value; }
        }

        public Brush kLogColor
        {
            get { return m_kLogColor; }
            set { m_kLogColor = value; }
        }

        public LogLevel eLogLevel
        {
            get { return m_eLogLevel; }
            set { m_eLogLevel = value; }
        }

        public LogItem()
        {

        }

        public LogItem(string sLogMessage, LogLevel eLogLevel)
        {
            m_eLogLevel = eLogLevel;

            switch (m_eLogLevel)
            {
                case LogLevel.NONE:
                    m_sLogMessage = "[NONE]\t\t--- " + sLogMessage + " ---";
                    m_kLogColor = Brushes.Gray;
                    break;
                case LogLevel.DEBUG:
                    m_sLogMessage = "[DEBUG]\t[" + DateTime.Now + "]\t" + sLogMessage;
                    m_kLogColor = Brushes.Black;
                    break;
                case LogLevel.INFO:
                    m_sLogMessage = "[INFO]\t[" + DateTime.Now + "]\t### " + sLogMessage + " ###";
                    m_kLogColor = Brushes.Green;
                    break;
                case LogLevel.WARN:
                    m_sLogMessage = "[WARN]\t[" + DateTime.Now + "]\t>> " + sLogMessage + " <<";
                    m_kLogColor = Brushes.Orange;
                    break;
                case LogLevel.ERROR:
                    m_sLogMessage = "[ERROR]\t[" + DateTime.Now + "]\t>> " + sLogMessage + " <<";
                    m_kLogColor = Brushes.OrangeRed;
                    break;
                case LogLevel.CRITICAL:
                    m_sLogMessage = "[CRITICAL]\t[" + DateTime.Now + "]\t>> " + sLogMessage + " <<";
                    m_kLogColor = Brushes.Red;
                    break;
                case LogLevel.FATAL:
                    m_sLogMessage = "[FATAL]\t[" + DateTime.Now + "]\t>> " + sLogMessage + " <<";
                    m_kLogColor = Brushes.DarkRed;
                    break;
                default:
                    break;
            }
        }
    }

    public enum LogLevel
    {
        NONE = 0,       // default
        DEBUG = 1,      // debug, not included in release build
        INFO = 2,       // like debug, but included in release build
        WARN = 3,       // not provocated event, does not harm the program lifecycle --> should fixed in release build
        ERROR = 4,      // not provocated event, could harm the program lifecycle --> must fixed in release build
        CRITICAL = 5,   // not provocated event, harms the program lifecycle, blocks main features --> must fixed in release build
        FATAL = 6       // not provocated event, crashes the program --> must fixed in release build
    }
}
