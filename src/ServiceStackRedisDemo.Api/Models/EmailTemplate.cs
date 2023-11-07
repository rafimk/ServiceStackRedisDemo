namespace ServiceStackRedisDemo.Api.Models;

public class EmailTemplate
{
    public string TemplateName { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }

    public EmailTemplate(string templateName, string subject, string body)
    {
        TemplateName = templateName;
        Subject = subject;
        Body = body;
    }
}