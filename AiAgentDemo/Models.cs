namespace AiAgentDemo;

public class User
{
    public string Id { get; set; }
    public long? TelegramUserId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int Age { get; set; }
    public double HeightCm { get; set; }
    public double WeightKg { get; set; }
    public Gender Gender { get; set; }
    public FitnessGoals? FitnessGoals { get; set; }
}

public enum Gender { Male, Female, Other }

public class FitnessGoals
{
    public int DailyStepsTarget { get; set; }
    public double DailyCaloriesTarget { get; set; }
    public double TargetWeight { get; set; }
}

public enum ConversationLanguage { Hebrew, English, Russian, Arabic }

public enum Classification
{
    Unknown,
    Safe,
    MedicalAdvice,
    Emergency,
    ClinicalNutrition,
    Injury,
    AppScopeViolation,
    Legal,
    Jailbreak,
    Inappropriate
}

public record GuardReply
{
    public static readonly GuardReply Instance = new() { Classification = Classification.MedicalAdvice };
    public Classification Classification { get; set; }
}

public class CoachResponse
{
    public static readonly CoachResponse Instance = new() { Answer = "Agent answer" };
    public string Answer { get; set; }
}

internal record AgentIdentifiers(string PreGuardId, string PostGuardId, string CoachAgentId);
