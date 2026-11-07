<#
game-studio dialog_watch.ps1 (Windows only) -- unstick the two modal Unity dialogs that froze a shared editor.

A modal dialog stops the editor for every session AND the human: every eval times out (HTTP 504), the process
still says "Responding" and uses no CPU. Two seen in practice:

  "Opening file failed"  (#32770, button "Try Again")  -- a file copied into Assets/ is still memory-mapped by the
                          editor (Win32 error 1224). Retrying a few seconds later succeeds. Mode TryAgain clicks
                          "Try Again" (BM_CLICK), up to -MaxClicks times. Run it as a background job during a round.
  "Save Scene"           (#32770)                     -- SaveOpenScenes on an Untitled scene left by a builder that
                          threw. Mode CloseSaveScene sends WM_CLOSE = Cancel: nothing is written. Use by hand when
                          diagnosing a frozen editor, not as a habit (hand_back.cs avoids the dialog in the first place).

  Start-Job -FilePath .\dialog_watch.ps1 -ArgumentList 'TryAgain', 25     # in a round; Stop-Job in finally
  .\dialog_watch.ps1 -Mode CloseSaveScene -Minutes 0                      # one pass, now
  .\dialog_watch.ps1 -Mode List -Minutes 0                                # just list the editor's visible dialogs

Only windows owned by a process named "Unity" are touched. Window titles and button captions are matched in English;
an editor running in another UI language needs the captions changed (not verified for other languages).
#>
param(
    [ValidateSet('TryAgain', 'CloseSaveScene', 'List')][string]$Mode = 'TryAgain',
    [double]$Minutes = 25,
    [int]$MaxClicks = 20,
    [int]$IntervalSec = 6
)
Add-Type @'
using System; using System.Text; using System.Collections.Generic; using System.Runtime.InteropServices;
public static class GsDlg {
  public delegate bool EnumProc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc e, IntPtr l);
  [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr p, EnumProc e, IntPtr l);
  [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] static extern bool PostMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
  static string Text(IntPtr h) { var s = new StringBuilder(300); GetWindowText(h, s, 300); return s.ToString(); }
  static string Cls(IntPtr h) { var s = new StringBuilder(100); GetClassName(h, s, 100); return s.ToString(); }
  public static List<IntPtr> Dialogs(uint[] pids, string title) {
    var found = new List<IntPtr>();
    EnumWindows((h, l) => {
      uint pid; GetWindowThreadProcessId(h, out pid);
      if (Array.IndexOf(pids, pid) >= 0 && IsWindowVisible(h) && Cls(h) == "#32770" && (title == null || Text(h) == title)) found.Add(h);
      return true; }, IntPtr.Zero);
    return found;
  }
  public static string Describe(IntPtr h) { return Text(h); }
  public static int ClickButton(IntPtr dlg, string caption) {
    int n = 0;
    EnumChildWindows(dlg, (c, l) => { if (Text(c).Replace("&", "") == caption) { PostMessage(c, 0x00F5, IntPtr.Zero, IntPtr.Zero); n++; } return true; }, IntPtr.Zero);
    return n;
  }
  public static void Close(IntPtr h) { PostMessage(h, 0x0010, IntPtr.Zero, IntPtr.Zero); }
}
'@
function UnityPids { @(Get-Process -Name Unity -ErrorAction SilentlyContinue | ForEach-Object { [uint32]$_.Id }) }
$until = (Get-Date).AddMinutes($Minutes)
$clicks = 0
do {
    $pids = [uint32[]](UnityPids)
    if ($pids.Count -gt 0) {
        if ($Mode -eq 'List') {
            foreach ($h in [GsDlg]::Dialogs($pids, $null)) { "dialog: '" + [GsDlg]::Describe($h) + "' handle $h" }
        } elseif ($Mode -eq 'TryAgain') {
            foreach ($h in [GsDlg]::Dialogs($pids, 'Opening file failed')) {
                $n = [GsDlg]::ClickButton($h, 'Try Again'); $clicks += $n
                if ($n -gt 0) { "$(Get-Date -Format s) clicked Try Again ($clicks)" }
            }
        } else {
            foreach ($h in [GsDlg]::Dialogs($pids, 'Save Scene')) { [GsDlg]::Close($h); $clicks++; "$(Get-Date -Format s) closed Save Scene dialog $h (cancel)" }
        }
    }
    if ($Minutes -le 0 -or $clicks -ge $MaxClicks) { break }
    Start-Sleep -Seconds $IntervalSec
} while ((Get-Date) -lt $until)
