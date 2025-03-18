namespace NotificationService.Business.Renderers;

public interface ITemplateRenderer
{
    string RenderTemplate(string template, Dictionary<string, string> data);
}