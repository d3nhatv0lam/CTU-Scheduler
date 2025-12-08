using System;
using System.IO;
using System.Text;

namespace CTUScheduler.Core.Helpers;

public class ActionTextWriter: TextWriter
{
    private readonly Action<string> _action;
    
    public ActionTextWriter(Action<string> action)
    {
        _action = action;
    }

    public override Encoding Encoding => Encoding.UTF8;
    
    public override void Write(char value) => _action(value.ToString());
    
    public override void Write(string? value) => _action(value ?? "");

    public override void WriteLine(string? value) => _action((value ?? "") + Environment.NewLine);
}
