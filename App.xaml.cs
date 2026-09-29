using System.Globalization;
using System.Windows;
using WPFLocalizeExtension.Engine;

namespace BpmMeasurer;

public partial class App : Application
{
    /// <summary>
    /// 启动时指定的音频文件路径(由 --audio= 显式参数或位置参数提供)。
    /// 为 null 表示未指定;非空时由 MainWindow.Window_Loaded 触发自动加载。
    /// </summary>
    public static string? StartupAudioPath { get; private set; }

    /// <summary>
    /// 启动时由宿主(HachimiDX)指定的 manifest 写入路径(--notify=)。
    /// 非 null 表示"嵌入模式":导出成功后,把 {config_path, audio_path} 写入该路径,
    /// 并以退出码 0 告知宿主;未导出关闭 → 1;写盘失败 → 2。
    /// </summary>
    public static string? StartupNotifyPath { get; private set; }

    /// <summary>
    /// 启动时指定的配置文件路径(--parse_config=),用于无头解析并导出 JSON 到 --notify 路径。
    /// 仅在与 --notify= 同时出现时生效;缺 --notify 时静默忽略,正常启动 GUI。
    /// </summary>
    public static string? StartupParseConfigPath { get; private set; }

    /// <summary>嵌入模式下是否已成功导出(供 OnExit 决定退出码)。</summary>
    public static bool EmbeddedExported { get; set; }

    public App()
    {
        // ── 全局崩溃日志 ──
        // 捕获所有未处理异常，写入 exe 所在目录下 logs/ 子目录的 crash_*.txt。
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                DebugLog.LogCrash("AppDomain.UnhandledException", ex);
            else
                DebugLog.Log($"AppDomain.UnhandledException (non-Exception): {e.ExceptionObject}");
        };

        DispatcherUnhandledException += (_, e) =>
        {
            DebugLog.LogCrash("DispatcherUnhandledException", e.Exception);
            // 不阻止进程退出：对于崩溃性异常，让进程自然终止。
            // 如果希望尝试恢复，可以设 e.Handled = true。
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            DebugLog.LogCrash("TaskScheduler.UnobservedTaskException", e.Exception);
            e.SetObserved(); // 防止进程被终结
        };

        DebugLog.Log("App started");

        LocalizeDictionary.Instance.SetCurrentThreadCulture = true;

        var args = Environment.GetCommandLineArgs();
        var lang = "zh-CN"; // default

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--language=", StringComparison.OrdinalIgnoreCase))
            {
                var value = args[i].Substring("--language=".Length);
                lang = value switch
                {
                    "en_us" or "en-us" or "en" => "en-US",
                    "zh_cn" or "zh-cn" or "zh" => "zh-CN",
                    _ => "zh-CN"
                };
            }
            else if (args[i].StartsWith("--audio=", StringComparison.OrdinalIgnoreCase))
            {
                StartupAudioPath = args[i].Substring("--audio=".Length);
            }
            else if (args[i].StartsWith("--notify=", StringComparison.OrdinalIgnoreCase))
            {
                StartupNotifyPath = args[i].Substring("--notify=".Length);
            }
            else if (args[i].StartsWith("--parse_config=", StringComparison.OrdinalIgnoreCase))
            {
                StartupParseConfigPath = args[i].Substring("--parse_config=".Length);
            }
        }

        // 兼容位置参数(拖入 exe 图标 / `BPM-Measurer.exe "song.mp3"`):
        // --audio= 优先,仅在其未提供时取第一个位置参数。
        if (string.IsNullOrEmpty(StartupAudioPath))
        {
            for (int i = 1; i < args.Length; i++) // 跳过 args[0] 自身路径
            {
                var arg = args[i];
                if (arg.StartsWith("--"))
                {
                    // 形如 `--language en` 这种空格分隔的选项会消费下一个 token,
                    // 简单起见跳过单个 `--` 开头 token 即可(本工具仅用 `--key=` 形式)。
                    continue;
                }
                StartupAudioPath = arg;
                break;
            }
        }

        // 空字符串视为未指定
        StartupAudioPath = string.IsNullOrWhiteSpace(StartupAudioPath) ? null : StartupAudioPath;
        StartupNotifyPath = string.IsNullOrWhiteSpace(StartupNotifyPath) ? null : StartupNotifyPath;
        StartupParseConfigPath = string.IsNullOrWhiteSpace(StartupParseConfigPath) ? null : StartupParseConfigPath;

        // 先设定 culture,确保无头模式(以及后续 GUI)的本地化文案遵循 --language。
        LocalizeDictionary.Instance.Culture = new CultureInfo(lang);

        // 无头模式:--parse_config 与 --notify 同时出现时,解析配置并把 JSON 写入 notify 文件后立即退出。
        // 缺少 --notify 时静默忽略 --parse_config,正常启动 GUI。
        if (StartupParseConfigPath is not null && StartupNotifyPath is not null)
        {
            RunHeadlessConfigExport();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // 嵌入模式:用退出码告知宿主(HachimiDX)导出结果。
        // 0 = 已成功导出(manifest 已写);1 = 用户未导出即关闭;2 = 写盘失败(由导出处理直接 Environment.Exit(2))。
        if (StartupNotifyPath is not null)
        {
            e.ApplicationExitCode = EmbeddedExported ? 0 : 1;
        }
        base.OnExit(e);
    }
}

