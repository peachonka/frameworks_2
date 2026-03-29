namespace Pr2.ModulesAndDi.Core;

public sealed class ModuleLoadException : Exception
{
    public ModuleLoadException(string message)
        : base(message)
    {
    }
}
