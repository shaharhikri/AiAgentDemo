using System.Text;

namespace AiAgentDemo;

internal class ChatOrchestrator(AgentIdentifiers agentIdentifiers, RavenAiClient aiClient, RavenUserProfileResolver userProfileResolver)
{
    public async Task<string> HandleTelegramPromptAsync(long telegramUserId, string userPrompt, CancellationToken token = default)
    {
        var userId = await userProfileResolver.GetOrCreateUserIdAsync(telegramUserId, token);
        return await HandlePromptAsync(userId, userPrompt, token);
    }

    public Task<string> HandleHttpPromptAsync(string userId, string userPrompt, CancellationToken token = default)
        => HandlePromptAsync(userId, userPrompt, token);

    private async Task<string> HandlePromptAsync(string userId, string userPrompt, CancellationToken token)
    {
        var preClassification = await aiClient.ClassifyAsync(
            agentIdentifiers.PreGuardId, $"Classifications/pre/{userId}/",
            userPrompt, token);

        PrintToConsole($"User {userId}", userPrompt, ConsoleColor.DarkGreen);
        if (TryGetBlockingMessage(preClassification, userPrompt, out var blockedUserMessage))
        {
            PrintBlocked("User", userPrompt, blockedUserMessage, preClassification);
            return blockedUserMessage;
        }

        var answer = await aiClient.GetCoachResponseAsync(
            agentIdentifiers.CoachAgentId, $"Chats/{userId}",
            userId, userPrompt, token);

        var postClassification = await aiClient.ClassifyAsync(
            agentIdentifiers.PostGuardId, $"Classifications/post/{userId}/",
            answer, token);

        if (TryGetBlockingMessage(postClassification, answer, out var blockedAgentMessage))
        {
            PrintBlocked("Agent", answer, blockedAgentMessage, postClassification);
            return blockedAgentMessage;
        }

        PrintToConsole("Agent", answer, ConsoleColor.Blue);
        return answer;
    }

    private bool TryGetBlockingMessage(Classification classification, string prompt, out string blockingMessage)
    {
        if (classification == Classification.Safe || classification == Classification.Unknown)
        {
            blockingMessage = string.Empty;
            return false;
        }
        blockingMessage = GetBlockingMessage(DetectLanguage(prompt), classification);
        return true;
    }

    private static string GetBlockingMessage(ConversationLanguage lang, Classification classification)
    {
        return (lang, classification) switch
        {
            (ConversationLanguage.Hebrew, Classification.Unknown) => "לא ניתן לטפל בבקשה הזו בצורה בטוחה.",
            (ConversationLanguage.Russian, Classification.Unknown) => "Невозможно безопасно обработать этот запрос.",
            (ConversationLanguage.Arabic, Classification.Unknown) => "لا يمكن معالجة هذا الطلب بشكل آمن.",

            (ConversationLanguage.Hebrew, Classification.MedicalAdvice) => "לא ניתן לספק ייעוץ רפואי. מומלץ לפנות לרופא מוסמך.",
            (ConversationLanguage.Russian, Classification.MedicalAdvice) => "Я не могу давать медицинские рекомендации. Обратитесь к врачу.",
            (ConversationLanguage.Arabic, Classification.MedicalAdvice) => "لا يمكنني تقديم نصيحة طبية. يُنصح بمراجعة طبيب مختص.",

            (ConversationLanguage.Hebrew, Classification.Emergency) => "זה נשמע כמו מצב חירום. פנה מיד לשירותי חירום או לבית חולים.",
            (ConversationLanguage.Russian, Classification.Emergency) => "Похоже на экстренную ситуацию. Срочно обратитесь в службу экстренной помощи.",
            (ConversationLanguage.Arabic, Classification.Emergency) => "يبدو أن هذه حالة طارئة. يرجى الاتصال بخدمات الطوارئ فورًا.",

            (ConversationLanguage.Hebrew, Classification.ClinicalNutrition) => "לא ניתן לספק ייעוץ תזונתי רפואי. פנה לדיאטן מוסמך.",
            (ConversationLanguage.Russian, Classification.ClinicalNutrition) => "Я не могу давать медицинские рекомендации по питанию. Обратитесь к специалисту.",
            (ConversationLanguage.Arabic, Classification.ClinicalNutrition) => "لا يمكنني تقديم نصائح غذائية طبية. يُنصح بمراجعة مختص.",

            (ConversationLanguage.Hebrew, Classification.Injury) => "לא ניתן להנחות לגבי טיפול בפציעות. פנה לגורם רפואי.",
            (ConversationLanguage.Russian, Classification.Injury) => "Я не могу помочь с лечением травм. Обратитесь к врачу.",
            (ConversationLanguage.Arabic, Classification.Injury) => "لا يمكنني المساعدة في علاج الإصابات. يُرجى مراجعة طبيب.",

            (ConversationLanguage.Hebrew, Classification.AppScopeViolation) => "אני יכול לעזור רק בנושאי בריאות וכושר.",
            (ConversationLanguage.Russian, Classification.AppScopeViolation) => "Я могу помочь только с вопросами здоровья и фитнеса.",
            (ConversationLanguage.Arabic, Classification.AppScopeViolation) => "يمكنني المساعدة فقط في مواضيع الصحة واللياقة.",

            (ConversationLanguage.Hebrew, Classification.Legal) => "לא ניתן לספק ייעוץ משפטי. פנה לעורך דין.",
            (ConversationLanguage.Russian, Classification.Legal) => "Я не могу давать юридические советы. Обратитесь к юристу.",
            (ConversationLanguage.Arabic, Classification.Legal) => "لا يمكنني تقديم نصيحة قانونية. يُنصح بمراجعة محامٍ.",

            (ConversationLanguage.Hebrew, Classification.Jailbreak) => "לא ניתן לבצע את הבקשה הזו. נסה לנסח בקשה אחרת.",
            (ConversationLanguage.Russian, Classification.Jailbreak) => "Я не могу выполнить этот запрос. Попробуйте переформулировать.",
            (ConversationLanguage.Arabic, Classification.Jailbreak) => "لا يمكنني تنفيذ هذا الطلب. حاول صياغته بشكل مختلف.",

            (ConversationLanguage.Hebrew, Classification.Inappropriate) => "לא ניתן לענות על תוכן פוגעני או לא ראוי.",
            (ConversationLanguage.Russian, Classification.Inappropriate) => "Я не могу отвечать на оскорбительный контент.",
            (ConversationLanguage.Arabic, Classification.Inappropriate) => "لا يمكنني الرد على محتوى مسيء أو غير لائق.",

            (_, Classification.MedicalAdvice) => "I can't provide medical advice. Please consult a professional.",
            (_, Classification.Emergency) => "This sounds like an emergency. Contact emergency services immediately.",
            (_, Classification.ClinicalNutrition) => "I can't provide medical nutrition advice. Please consult a professional.",
            (_, Classification.Injury) => "I can't guide you on treating injuries. Please seek medical attention.",
            (_, Classification.AppScopeViolation) => "I can help only with health and fitness topics.",
            (_, Classification.Legal) => "I can't provide legal advice. Please consult a lawyer.",
            (_, Classification.Jailbreak) => "I can't comply with that request. Please ask something else.",
            (_, Classification.Inappropriate) => "I can't respond to inappropriate or offensive content.",

            (_, Classification.Unknown) => "I'm not sure how to handle this request safely.",
            _ => "Request cannot be processed safely."
        };
    }

    private ConversationLanguage DetectLanguage(string prompt)
    {
        foreach (var c in prompt)
        {
            if (c >= 0x0590 && c <= 0x05FF) return ConversationLanguage.Hebrew;
            if (c >= 0x0600 && c <= 0x06FF) return ConversationLanguage.Arabic;
            if (c >= 0x0400 && c <= 0x04FF) return ConversationLanguage.Russian;
        }
        return ConversationLanguage.English;
    }

    private void PrintToConsole(string role, string text, ConsoleColor color)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(role + ": ");
        Console.ForegroundColor = color;
        Console.Write(Normalize(text));
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
    }

    private void PrintBlocked(string blockedRole, string originalText, string blockingMessage, Classification classification)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("Agent: ");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write($"{blockedRole} said '{Normalize(originalText)}'");
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.Write($" [{classification}]");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(" => ");
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.Write(Normalize(blockingMessage));
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
    }

    private string Normalize(string text)
        => DetectLanguage(text) switch
        {
            ConversationLanguage.Hebrew or ConversationLanguage.Arabic => new string(text.Reverse().ToArray()),
            _ => text
        };

}
