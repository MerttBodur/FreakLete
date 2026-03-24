using System.Globalization;

namespace FreakLete.Services;

/// <summary>
/// Client-side language helper based on device UI culture.
/// Provides localized strings for FreakAI page and other UI surfaces.
/// </summary>
public static class AppLanguage
{
	/// <summary>
	/// Returns the user's language code based on device culture ("tr", "en", etc.).
	/// </summary>
	public static string Code
	{
		get
		{
			string twoLetter = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
			return twoLetter switch
			{
				"tr" => "tr",
				"de" => "de",
				"fr" => "fr",
				"es" => "es",
				_ => "en"
			};
		}
	}

	public static bool IsTurkish => Code == "tr";

	// ── FreakAI Quick Action Button Labels ──────────────────────

	public static string QuickGenerateProgram => IsTurkish ? "Program Oluştur" : "Generate Program";
	public static string QuickViewProgram => IsTurkish ? "Programı Görüntüle" : "View Program";
	public static string QuickAnalyzeTraining => IsTurkish ? "Antrenman Analizi" : "Analyze Training";
	public static string QuickNutritionHelp => IsTurkish ? "Beslenme Yardımı" : "Nutrition Help";

	// ── FreakAI Quick Action Prompts ────────────────────────────

	public static string PromptGenerateProgram => IsTurkish
		? "Profilime, hedeflerime, ekipmanlarıma ve mevcut performans verilerime göre kişiselleştirilmiş bir antrenman programı yaz."
		: "Write me a personalized training program based on my profile, goals, equipment, and current performance data.";

	public static string PromptViewProgram => IsTurkish
		? "Mevcut aktif antrenman programımı detaylı olarak göster."
		: "Show me my current active training program in detail.";

	public static string PromptAnalyzeTraining => IsTurkish
		? "Son antrenman verilerimi analiz et. Güçlü ve zayıf yönlerim neler, sırada neye odaklanmalıyım?"
		: "Analyze my recent training data. What are my strengths, weaknesses, and what should I focus on next?";

	public static string PromptNutritionHelp => IsTurkish
		? "Profilime, hedeflerime ve antrenman yükümü göz önünde bulundurarak kişiselleştirilmiş beslenme önerileri ver."
		: "Based on my profile, goals, and training load, give me personalized nutrition guidance.";

	// ── FreakAI Loading Phases ──────────────────────────────────

	public static string[] LoadingPhasesHeavy => IsTurkish
		? ["FreakAI düşünüyor...", "Profilin analiz ediliyor...", "Verilerin kontrol ediliyor...", "Programın hazırlanıyor...", "Neredeyse bitti..."]
		: ["FreakAI is thinking...", "Analyzing your profile...", "Checking your data...", "Building your program...", "Almost there..."];

	public static string[] LoadingPhasesLight => IsTurkish
		? ["FreakAI düşünüyor...", "Verilerin getiriliyor...", "Yanıt hazırlanıyor..."]
		: ["FreakAI is thinking...", "Fetching your data...", "Preparing response..."];

	public static string LoadingDefault => IsTurkish ? "FreakAI düşünüyor..." : "FreakAI is thinking...";

	// ── FreakAI Error Messages ──────────────────────────────────

	public static string ErrorConnectionFailed => IsTurkish
		? "Bağlantı hatası. Lütfen internet bağlantınızı kontrol edip tekrar deneyin."
		: "Connection error. Please check your internet and try again.";

	public static string ErrorNoResponse => IsTurkish
		? "Yanıt alınamadı. Lütfen tekrar deneyin."
		: "Failed to get response. Please try again.";

	// ── FreakAI Welcome Text ────────────────────────────────────

	public static string WelcomeTitle => IsTurkish ? "FreakAI Koç" : "FreakAI Coach";

	public static string WelcomeBody => IsTurkish
		? "Kişisel hibrit antrenman koçun. Antrenman programı yazabilir, geri bildirimlerine göre ayarlayabilir, sakatlık/rehabilitasyon sorularına yardım edebilir ve beslenme rehberliği sunabilirim — hepsi senin verilerine göre kişiselleştirilmiş."
		: "Your personal hybrid training coach. I can write training programs, adjust them based on your feedback, help with injury/rehab questions, and provide nutrition guidance — all personalized to your data.";

	public static string WelcomeHint => IsTurkish
		? "Dene: 'Bana bir program yaz' veya 'Squat yaparken dizim ağrıyor' veya 'Beslenme konusunda yardım et'"
		: "Try: 'Write me a program' or 'My knee hurts during squats' or 'Help me with my nutrition'";

	// ── FreakAI Input Placeholder ───────────────────────────────

	public static string InputPlaceholder => IsTurkish ? "FreakAI'ya sor..." : "Ask FreakAI...";

	// ── Active Program Card ─────────────────────────────────────

	public static string ActiveProgramLabel => IsTurkish ? "AKTİF PROGRAM" : "ACTIVE PROGRAM";

	public static string FormatProgramDetails(string goal, int daysPerWeek, int weekCount) => IsTurkish
		? $"{goal} · Haftada {daysPerWeek} gün · {weekCount} hafta"
		: $"{goal} · {daysPerWeek} days/week · {weekCount} weeks";
}
