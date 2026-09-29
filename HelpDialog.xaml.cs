using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using WPFLocalizeExtension.Extensions;

namespace BpmMeasurer;

public partial class HelpDialog : Window
{
    private static readonly SolidColorBrush ActiveNavBrush = new(Color.FromRgb(0x31, 0x2E, 0x81));
    private static readonly SolidColorBrush InactiveNavBrush = new(Colors.Transparent);
    private static readonly SolidColorBrush ActiveNavFg = new(Colors.White);
    private static readonly SolidColorBrush InactiveNavFg = new(Color.FromRgb(0xCC, 0xCC, 0xCC));

    public record HelpEntry(string Operation, string Description);
    public record HelpGroup(string Title, List<HelpEntry> Items);

    public HelpDialog()
    {
        InitializeComponent();

        // ── Help groups (entries 1-3 view, 4-5 ruler/segment, 6-9 shortcuts) ──
        var groups = new ObservableCollection<HelpGroup>
        {
            new(Loc("Help_Group1"), Entries(1, 2, 3)),
            new(Loc("Help_Group2"), Entries(4, 5)),
            new(Loc("Help_Group3"), Entries(6, 7, 8, 9))
        };
        HelpGroupsControl.ItemsSource = groups;

        // ── About panel metadata (all read from assembly attributes in .csproj) ──
        var asm = Assembly.GetExecutingAssembly();
        var version = asm.GetName().Version;
        var versionText = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "?";
        AboutVersionValue.Text = versionText;

        AboutAuthorValue.Text = ExtractAuthor(asm);
        var repoUrl = ExtractRepoUrl(asm);
        RepoLinkText.Text = repoUrl;
        RepoLink.NavigateUri = new Uri(repoUrl);
        AboutBuildTimeValue.Text = ExtractBuildTime(asm);

        // Default to the Help panel.
        SetActivePanel(HelpPanel);
    }

    private static List<HelpEntry> Entries(params int[] indices)
        => indices.Select(i => new HelpEntry(Loc($"Help_Op{i}"), Loc($"Help_Desc{i}"))).ToList();

    /// <summary>
    /// Extracts the author name from the assembly copyright string
    /// (csproj emits "©Simon273 yyyy.MM.dd_HH:mm:ss_UTCzzz").
    /// </summary>
    private static string ExtractAuthor(Assembly asm)
    {
        var attr = asm.GetCustomAttribute<AssemblyCopyrightAttribute>();
        var copyright = attr?.Copyright;
        if (string.IsNullOrEmpty(copyright) || copyright.Length < 2)
            return "?";
        // Strip the leading © and take characters up to the first space.
        var rest = copyright[1..];
        int space = rest.IndexOf(' ');
        return space > 0 ? rest[..space] : rest;
    }

    /// <summary>
    /// Reads the repo URL from AssemblyTitleAttribute
    /// (csproj maps &lt;AssemblyTitle&gt; to this attribute).
    /// Falls back to the hard-coded repo URL when the attribute is not set.
    /// </summary>
    private static string ExtractRepoUrl(Assembly asm)
    {
        var attr = asm.GetCustomAttribute<AssemblyTitleAttribute>();
        var title = attr?.Title;
        if (string.IsNullOrEmpty(title))
            return "https://github.com/ck2739046/BPM-Measurer";
        return title;
    }

    /// <summary>
    /// Extracts the build timestamp baked into the assembly's copyright string
    /// (csproj emits "©Simon273 yyyy.MM.dd_HH:mm:ss_UTCzzz"). Falls back to the raw
    /// copyright string when the format cannot be parsed.
    /// </summary>
    private static string ExtractBuildTime(Assembly asm)
    {
        var attr = asm.GetCustomAttribute<AssemblyCopyrightAttribute>();
        var copyright = attr?.Copyright;
        if (string.IsNullOrEmpty(copyright))
            return "—";

        int space = copyright.IndexOf(' ');
        if (space < 0 || space >= copyright.Length - 1)
            return copyright;

        var stamp = copyright.Substring(space + 1).Trim();
        if (string.IsNullOrEmpty(stamp))
            return copyright;

        // "2026.06.23_14:30:00_UTC+08:00" → "2026.06.23 14:30:00 UTC+08:00"
        int firstUnder = stamp.IndexOf('_');
        if (firstUnder <= 0)
            return stamp; // unexpected shape — show the raw stamp

        var datePart = stamp[..firstUnder];
        var rest = stamp[(firstUnder + 1)..];
        int tzIdx = rest.IndexOf("_UTC", StringComparison.Ordinal);
        var timePart = tzIdx >= 0 ? rest[..tzIdx] : rest;
        var tzPart = tzIdx >= 0 ? rest[(tzIdx + 1)..] : "";
        return string.IsNullOrWhiteSpace(tzPart)
            ? $"{datePart} {timePart}".Trim()
            : $"{datePart} {timePart} {tzPart}".Trim();
    }

    // ── Navigation ──

    private void NavHelpBtn_Click(object sender, RoutedEventArgs e) => SetActivePanel(HelpPanel);

    private void NavAboutBtn_Click(object sender, RoutedEventArgs e) => SetActivePanel(AboutPanel);

    private void SetActivePanel(ScrollViewer panel)
    {
        bool helpActive = ReferenceEquals(panel, HelpPanel);
        HelpPanel.Visibility = helpActive ? Visibility.Visible : Visibility.Collapsed;
        AboutPanel.Visibility = helpActive ? Visibility.Collapsed : Visibility.Visible;

        NavHelpBtn.Background = helpActive ? ActiveNavBrush : InactiveNavBrush;
        NavHelpBtn.Foreground = helpActive ? ActiveNavFg : InactiveNavFg;
        NavAboutBtn.Background = helpActive ? InactiveNavBrush : ActiveNavBrush;
        NavAboutBtn.Foreground = helpActive ? InactiveNavFg : ActiveNavFg;
    }

    private void RepoLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); }
        catch { /* ignore: shell-open failures (e.g. no default browser) */ }
    }

    private static string Loc(string key)
    {
        var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        var fullKey = $"{assemblyName}:Langs:{key}";
        var locExtension = new LocExtension(fullKey);
        locExtension.ResolveLocalizedValue(out string? result);
        return result ?? key;
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
