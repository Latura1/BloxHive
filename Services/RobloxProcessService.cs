using System.Diagnostics;
using BloxHive.Models;

namespace BloxHive.Services;

public class RobloxProcessService
{
    private static readonly string[] _targetProcesses = ["RobloxPlayerBeta", "RobloxStudioBeta"];

    public List<RobloxProcessInfo> GetProcesses()
    {
        var list = new List<RobloxProcessInfo>();

        foreach (var name in _targetProcesses)
        {
            try
            {
                var processes = Process.GetProcessesByName(name);
                foreach (var p in processes)
                {
                    list.Add(new RobloxProcessInfo
                    {
                        Id = p.Id,
                        MainWindowTitle = p.MainWindowTitle,
                    });
                }
            }
            catch { }
        }

        return list;
    }

    public void Kill(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            process.Kill(true);
        }
        catch { }
    }

    public void KillAll()
    {
        foreach (var p in GetProcesses())
        {
            try
            {
                var process = Process.GetProcessById(p.Id);
                process.Kill(true);
            }
            catch { }
        }
    }
}
