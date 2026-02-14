namespace CMMS.Core.Configuration;

public class AiAssistantSettings
{
    public const string SectionName = "AiAssistant";

    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "http://192.168.1.111:11434/v1";
    public string Model { get; set; } = "llama3";
    public int TimeoutSeconds { get; set; } = 120;
    public string SystemPrompt { get; set; } = "You are a maintenance management AI assistant for a CMMS (Computerized Maintenance Management System). You help maintenance teams analyze equipment health, predict failures, prioritize work orders, and optimize maintenance schedules. Be concise, practical, and data-driven in your responses. When analyzing maintenance data, highlight actionable insights and prioritize by criticality and safety impact.";
}
