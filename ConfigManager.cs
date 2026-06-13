// 生成于 GLM-5V-Turbo

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BDSM
{
    /// <summary>
    /// 管理与 exe 同目录的 bdsm.ini 配置文件。
    /// 读取失败时自动删除损坏文件并生成含默认值的新文件。
    /// </summary>
    internal static class ConfigManager
    {
        private static readonly string ConfigFilePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bdsm.ini");

        // ---- 注册的配置项 ----

        private static readonly Dictionary<string, string> Defaults = new Dictionary<string, string>
        {
            { "DnSpyPath", "C:/dnSpy" },
        };

        private static readonly Dictionary<string, string> _values = new Dictionary<string, string>();

        // ---- 公开属性 ----

        public static string DnSpyPath => Get("DnSpyPath");

        // ---- 初始化 ----

        /// <summary>
        /// 启动时调用，加载配置文件。失败则重建默认配置。
        /// </summary>
        public static void Initialize()
        {
            // 先填入所有默认值
            foreach (var kv in Defaults)
                _values[kv.Key] = kv.Value;

            if (!File.Exists(ConfigFilePath))
            {
                Save();
                return;
            }

            try
            {
                ParseIni(File.ReadAllText(ConfigFilePath, Encoding.UTF8));
            }
            catch
            {
                // 文件损坏或格式异常，删除并重新生成
                try { File.Delete(ConfigFilePath); } catch { }
                Save();
            }
        }

        // ---- 读写接口 ----

        public static string Get(string key)
        {
            if (_values.TryGetValue(key, out var val))
                return val;
            if (Defaults.TryGetValue(key, out var def))
                return def;
            return null;
        }

        public static void Set(string key, string value)
        {
            _values[key] = value;
            Save();
        }

        // ---- 持久化 ----

        public static void Save()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("; BDSM Configuration");
                sb.AppendLine();

                foreach (var kv in _values)
                    sb.AppendLine(kv.Key + "=" + kv.Value);

                File.WriteAllText(ConfigFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[BDSM] Warning: failed to save config - " + ex.Message);
            }
        }

        // ---- 内部方法 ----

        private static void ParseIni(string content)
        {
            foreach (var rawLine in content.Split('\n'))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#") || line.StartsWith("["))
                    continue;

                var eqIdx = line.IndexOf('=');
                if (eqIdx <= 0)
                    continue; // 无效行，跳过

                var key = line.Substring(0, eqIdx).Trim();
                var val = line.Substring(eqIdx + 1).Trim();

                if (Defaults.ContainsKey(key))
                    _values[key] = val;
            }
        }
    }
}
