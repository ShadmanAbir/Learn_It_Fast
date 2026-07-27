using System.Reflection;

namespace Lif.Domain;

/// <summary>Marker for assembly scanning and architecture tests.</summary>
public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
