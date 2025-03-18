using System.Text.RegularExpressions;

namespace NotificationService.Business.Renderers;

public class SimpleTemplateRenderer : ITemplateRenderer
{
    public string RenderTemplate(string template, Dictionary<string, string> data)
    {
        if (string.IsNullOrEmpty(template))
            return string.Empty;

        if (data == null || data.Count == 0)
            return template;

        return data.Aggregate(template,
            (current, item) =>
                Regex.Replace(current, $"{{{{\\s*{item.Key}\\s*}}}}", item.Value, RegexOptions.IgnoreCase));
    }
}