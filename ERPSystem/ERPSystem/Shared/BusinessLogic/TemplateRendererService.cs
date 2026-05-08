namespace ERPSystem.Shared.BusinessLogic;

public class TemplateRendererService
{
    public string Render(string template, Dictionary<string, string?> values)
    {
        foreach (var item in values)
        {
            template = template.Replace($"{{{{{item.Key}}}}}", item.Value ?? "");
        }

        return template;
    }
}