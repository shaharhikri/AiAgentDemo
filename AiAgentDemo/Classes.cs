using System;
using System.Collections.Generic;
using System.Text;

namespace AiAgentDemo.Classes;

public class User
{
    public string Id { get; set; }

    public string FirstName { get; set; }
    public string LastName { get; set; }

    public int Age { get; set; }
    public double HeightCm { get; set; }
    public double WeightKg { get; set; }

    public Gender Gender { get; set; }

    public FitnessGoals? FitnessGoals { get; set; }
}

public enum Gender
{
    Male,
    Female,
    Other
}

public class FitnessGoals
{
    public int DailyStepsTarget { get; set; }
    public double DailyCaloriesTarget { get; set; }
    public double TargetWeight { get; set; }
}

public enum ConversationLanguage
{
    Hebrew,
    English,
    Russian,
    Arabic
}

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
    public static readonly GuardReply Instance = new GuardReply() { Classification = Classification.MedicalAdvice };
    public Classification Classification { get; set; }
}

public class CoachResponse
{
    public static readonly CoachResponse Instance = new CoachResponse() { Answer = "Agent answer" };

    public string Answer { get; set; }
}