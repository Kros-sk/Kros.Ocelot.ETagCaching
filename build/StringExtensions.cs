using Nuke.Common.Tooling;
using Nuke.Common.Utilities;

static class StringExtensions
{
    public static void AppendParam(ref this ArgumentStringHandler handler, string param)
        => handler.AppendLiteral(" " + param);

    public static void AppendParam(ref this ArgumentStringHandler handler, string param, string value)
    {
        handler.AppendLiteral(" " + param + " ");
        handler.AppendDoubleQuoted(value);
    }

    public static void AppendParamIfNotNull(ref this ArgumentStringHandler handler, string param, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            handler.AppendParam(param, value);
        }
    }

    public static void AppendDoubleQuoted(ref this ArgumentStringHandler handler, string value)
        => handler.AppendLiteral(value.DoubleQuote());
}
