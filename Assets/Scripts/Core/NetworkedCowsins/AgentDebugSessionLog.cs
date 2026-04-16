using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace HeistNSeek.Core.NetworkedCowsins
{
    /// <summary>
    /// Session debug: append one NDJSON line per call to workspace debug-ffb983.log (project root).
    /// </summary>
    internal static class AgentDebugSessionLog
    {
        private const string SessionId = "ffb983";
        private const string FileName = "debug-ffb983.log";

        public static void Write(string hypothesisId, string location, string message, string dataObjectInner = "", string runId = "pre-fix")
        {
            // #region agent log
            try
            {
                var data = string.IsNullOrEmpty(dataObjectInner) ? "{}" : "{" + dataObjectInner + "}";
                var sb = new StringBuilder(384);
                sb.Append("{\"sessionId\":\"").Append(SessionId).Append("\",");
                sb.Append("\"hypothesisId\":\"").Append(Escape(hypothesisId)).Append("\",");
                sb.Append("\"location\":\"").Append(Escape(location)).Append("\",");
                sb.Append("\"message\":\"").Append(Escape(message)).Append("\",");
                sb.Append("\"runId\":\"").Append(Escape(runId)).Append("\",");
                sb.Append("\"timestamp\":").Append(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()).Append(",");
                sb.Append("\"data\":").Append(data).Append("}\n");
                var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", FileName));
                File.AppendAllText(path, sb.ToString());
            }
            catch
            {
                /* intentional: never break gameplay */
            }
            // #endregion
        }

        internal static string EscapeForJson(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
        }

        private static string Escape(string s) => EscapeForJson(s);
    }
}
