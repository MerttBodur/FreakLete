namespace FreakLete.Api.Services;

/// <summary>
/// Lightweight heuristic language detector for user messages.
/// Uses Unicode script ranges and common word patterns.
/// No external dependencies — good enough for the top ~15 languages FreakLete users write in.
/// </summary>
public static class LanguageDetector
{
    /// <summary>
    /// Detects the dominant language of <paramref name="text"/>.
    /// Returns an IETF-style tag (e.g. "tr", "en", "de") or "en" as fallback.
    /// </summary>
    public static string Detect(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "en";

        // Script-based detection (non-Latin scripts are easy to identify)
        int cyrillic = 0, arabic = 0, cjk = 0, hangul = 0, devanagari = 0, latin = 0;

        foreach (char c in text)
        {
            if (c is >= '\u0400' and <= '\u04FF') cyrillic++;
            else if (c is >= '\u0600' and <= '\u06FF') arabic++;
            else if (c is (>= '\u4E00' and <= '\u9FFF') or (>= '\u3040' and <= '\u30FF')) cjk++;
            else if (c is >= '\uAC00' and <= '\uD7AF') hangul++;
            else if (c is >= '\u0900' and <= '\u097F') devanagari++;
            else if (c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or
                         (>= '\u00C0' and <= '\u024F') or // Latin extended (ö, ü, ş, ç, ğ, ı, ñ, etc.)
                         (>= '\u0100' and <= '\u017F'))    // Latin Extended-A
                latin++;
        }

        int total = cyrillic + arabic + cjk + hangul + devanagari + latin;
        if (total == 0) return "en";

        // Non-Latin script dominance
        if (cyrillic > total * 0.3) return "ru";
        if (arabic > total * 0.3) return "ar";
        if (cjk > total * 0.3) return DetectCjk(text);
        if (hangul > total * 0.3) return "ko";
        if (devanagari > total * 0.3) return "hi";

        // Latin-script — use keyword heuristics
        return DetectLatinLanguage(text);
    }

    /// <summary>
    /// Returns a human-readable language name for system prompt injection.
    /// </summary>
    public static string GetLanguageName(string code) => code switch
    {
        "tr" => "Turkish",
        "en" => "English",
        "de" => "German",
        "fr" => "French",
        "es" => "Spanish",
        "pt" => "Portuguese",
        "it" => "Italian",
        "nl" => "Dutch",
        "ru" => "Russian",
        "ar" => "Arabic",
        "zh" => "Chinese",
        "ja" => "Japanese",
        "ko" => "Korean",
        "hi" => "Hindi",
        "pl" => "Polish",
        "sv" => "Swedish",
        "no" => "Norwegian",
        "da" => "Danish",
        "fi" => "Finnish",
        "ro" => "Romanian",
        "hu" => "Hungarian",
        "cs" => "Czech",
        _ => "English"
    };

    // ── Private helpers ────────────────────────────────────────

    private static string DetectCjk(string text)
    {
        // Hiragana/Katakana → Japanese
        foreach (char c in text)
        {
            if (c is >= '\u3040' and <= '\u309F') return "ja"; // Hiragana
            if (c is >= '\u30A0' and <= '\u30FF') return "ja"; // Katakana
        }
        return "zh"; // Default CJK → Chinese
    }

    private static string DetectLatinLanguage(string text)
    {
        string lower = text.ToLowerInvariant();

        // Turkish-specific characters + common words
        // Turkish has: ç, ş, ğ, ı (dotless-i), ö, ü, İ
        if (ContainsTurkishChars(lower) || MatchesTurkishWords(lower))
            return "tr";

        // German
        if (ContainsAny(lower, "ß", "ä") ||
            MatchesWords(lower, "ich", "und", "ist", "das", "ein", "nicht", "auch", "wie", "mit"))
            return "de";

        // French
        if (ContainsAny(lower, "ê", "è", "ë", "ç") && ContainsAny(lower, "je", "le", "les", "des", "une") ||
            MatchesWords(lower, "je", "est", "les", "des", "une", "pas", "que", "pour", "dans"))
            return "fr";

        // Spanish
        if (ContainsAny(lower, "ñ") ||
            MatchesWords(lower, "que", "por", "los", "las", "una", "como", "pero", "más", "esta"))
            return "es";

        // Portuguese
        if (MatchesWords(lower, "não", "uma", "como", "mais", "para", "com", "são", "está"))
            return "pt";

        // Italian
        if (MatchesWords(lower, "che", "una", "per", "sono", "con", "come", "anche", "della"))
            return "it";

        // Dutch
        if (MatchesWords(lower, "het", "een", "van", "dat", "niet", "ik", "zijn", "ook"))
            return "nl";

        // Polish
        if (ContainsAny(lower, "ą", "ę", "ł", "ź", "ż", "ń", "ś", "ć") ||
            MatchesWords(lower, "nie", "jest", "się", "jak", "ale", "już"))
            return "pl";

        // Romanian
        if (ContainsAny(lower, "ă", "â", "î", "ț", "ș") ||
            MatchesWords(lower, "este", "sunt", "pentru", "care", "din"))
            return "ro";

        // Hungarian
        if (MatchesWords(lower, "egy", "nem", "van", "ezt", "hogy", "ami", "meg"))
            return "hu";

        // Czech
        if (ContainsAny(lower, "ř", "ů", "ě") ||
            MatchesWords(lower, "není", "jsou", "jako", "ale", "pro"))
            return "cs";

        // Swedish
        if (MatchesWords(lower, "och", "att", "det", "som", "för", "inte", "med"))
            return "sv";

        // Default: English
        return "en";
    }

    private static bool ContainsTurkishChars(string text)
    {
        foreach (char c in text)
        {
            if (c is 'ş' or 'ğ' or 'ı' or 'İ')
                return true;
        }
        return false;
    }

    private static bool MatchesTurkishWords(string lower)
    {
        return MatchesWords(lower, "bir", "için", "olan", "ben", "benim", "bana", "nasıl",
            "var", "yok", "ama", "ile", "çok", "daha", "gibi", "kadar",
            "mı", "mi", "mu", "mü", "ne", "şu", "bu", "nedir",
            "yapabilir", "antrenman", "program", "ağrı", "bölge");
    }

    private static bool ContainsAny(string text, params string[] needles)
    {
        foreach (string n in needles)
        {
            if (text.Contains(n, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private static bool MatchesWords(string text, params string[] words)
    {
        // Split once
        var tokens = text.Split([' ', ',', '.', '!', '?', '\n', '\r', '\t', ':', ';'],
            StringSplitOptions.RemoveEmptyEntries);

        int matches = 0;
        foreach (string token in tokens)
        {
            foreach (string word in words)
            {
                if (string.Equals(token, word, StringComparison.OrdinalIgnoreCase))
                {
                    matches++;
                    if (matches >= 2) return true; // 2+ keyword hits = strong signal
                    break;
                }
            }
        }
        return false;
    }
}
