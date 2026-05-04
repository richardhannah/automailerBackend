using System.Text;
using System.Text.RegularExpressions;
using AutoMailerBackend.Models;

namespace AutoMailerBackend.Services;

public static partial class TemplateRenderer
{
    public static Dictionary<string, string> CustomerToVars(Customer c, string prefix = "customer")
    {
        return new Dictionary<string, string>
        {
            [$"{prefix}.firstName"] = c.FirstName,
            [$"{prefix}.lastName"] = c.LastName,
            [$"{prefix}.name"] = $"{c.FirstName} {c.LastName}",
            [$"{prefix}.email"] = c.Email,
            [$"{prefix}.iptvUser"] = c.IptvUser,
            [$"{prefix}.iptvPassword"] = c.IptvPassword ?? "",
            [$"{prefix}.notes"] = c.Notes ?? "",
            [$"{prefix}.expirationDate"] = c.ExpirationDate?.ToString("yyyy-MM-dd") ?? "",
        };
    }

    public static string Render(string template, Dictionary<string, string> vars,
        Dictionary<string, List<Dictionary<string, string>>>? collections = null)
    {
        // First, process {% for item in collection %}...{% endfor %} blocks
        var result = ForPattern().Replace(template, match =>
        {
            var itemName = match.Groups[1].Value.Trim();
            var collectionName = match.Groups[2].Value.Trim();
            var body = match.Groups[3].Value;

            if (collections == null || !collections.TryGetValue(collectionName, out var items))
                return "";

            var sb = new StringBuilder();
            foreach (var item in items)
            {
                // Prefix each var key with the loop variable name
                var scopedVars = new Dictionary<string, string>(vars);
                foreach (var (key, value) in item)
                {
                    scopedVars[$"{itemName}.{key}"] = value;
                }
                sb.Append(RenderVars(body, scopedVars));
            }
            return sb.ToString();
        });

        // Then replace any remaining top-level {{ vars }}
        return RenderVars(result, vars);
    }

    private static string RenderVars(string template, Dictionary<string, string> vars)
    {
        return VarPattern().Replace(template, match =>
        {
            var key = match.Groups[1].Value.Trim();
            return vars.TryGetValue(key, out var value) ? value : match.Value;
        });
    }

    [GeneratedRegex(@"\{%\s*for\s+(\w+)\s+in\s+(\w+)\s*%\}(.*?)\{%\s*endfor\s*%\}", RegexOptions.Singleline)]
    private static partial Regex ForPattern();

    [GeneratedRegex(@"\{\{\s*([\w.]+)\s*\}\}")]
    private static partial Regex VarPattern();
}
