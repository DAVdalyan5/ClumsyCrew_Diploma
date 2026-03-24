using System.IO;
using UnityEngine;

namespace HeistNSeek.Core.Enemy.Diagnostics
{
    /// <summary>Session NDJSON logs for debug mode (session 83c568).</summary>
    public static class EnemyRangedDebugNdjson
    {
        private const string SessionId = "83c568";
        private const string FileName = "debug-83c568.log";

        public static void Log(string hypothesisId, string location, string message, string dataJson = "{}",
            string runId = "pre-fix")
        {
            try
            {
                var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", FileName));
                long ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                string esc = message.Replace("\\", "\\\\").Replace("\"", "\\\"");
                string escRun = runId.Replace("\\", "\\\\").Replace("\"", "\\\"");
                var line =
                    $"{{\"sessionId\":\"{SessionId}\",\"hypothesisId\":\"{hypothesisId}\",\"location\":\"{location}\",\"message\":\"{esc}\",\"data\":{dataJson},\"timestamp\":{ts},\"runId\":\"{escRun}\"}}\n";
                File.AppendAllText(path, line);
            }
            catch
            {
                // ignore logging failures
            }
        }
    }
}
