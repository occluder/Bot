namespace Bot.Utils;

internal static class Extensions
{
    public static Version GetAssemblyVersion(this Type type) => type.Assembly.GetName().Version ?? new Version();
}
