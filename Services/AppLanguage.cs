using System.Globalization;

namespace FreakLete.Services;

/// <summary>
/// Client-side language service with persistence.
/// Provides localized strings for all high-impact UI surfaces.
/// Default language: English. Supported: en, tr.
/// </summary>
public static class AppLanguage
{
	private const string PreferenceKey = "app_language";

	/// <summary>
	/// Returns the active language code ("en" or "tr").
	/// </summary>
	public static string Code { get; private set; } = "en";

	public static bool IsTurkish => Code == "tr";

	/// <summary>
	/// Restore persisted language on app startup.
	/// Defaults to "en" when no preference exists.
	/// </summary>
	public static void Initialize()
	{
		Code = Preferences.Default.Get(PreferenceKey, "en");
	}

	/// <summary>
	/// Switch language, persist, and update culture.
	/// </summary>
	public static void SetLanguage(string code)
	{
		Code = code is "tr" ? "tr" : "en";
		Preferences.Default.Set(PreferenceKey, Code);

		var culture = new CultureInfo(Code);
		CultureInfo.DefaultThreadCurrentCulture = culture;
		CultureInfo.DefaultThreadCurrentUICulture = culture;
	}

	// ── Settings Page ───────────────────────────────────────────

	public static string SettingsTitle => IsTurkish ? "Ayarlar" : "Settings";
	public static string SettingsChangePassword => IsTurkish ? "Şifre Değiştir" : "Change Password";
	public static string SettingsChangePasswordDesc => IsTurkish ? "Hesap şifrenizi güncelleyin" : "Update your account password";
	public static string SettingsLanguage => IsTurkish ? "Dil" : "Language";
	public static string SettingsLanguageDesc => IsTurkish ? "Uygulama dilini değiştirin" : "Change app language";
	public static string SettingsLanguageCurrent => IsTurkish ? "Türkçe" : "English";
	public static string SettingsLeaveReview => IsTurkish ? "Yorum Bırak" : "Leave Review";
	public static string SettingsLeaveReviewDesc => IsTurkish ? "Uygulamamızı değerlendirin" : "Rate our app";
	public static string SettingsDonate => IsTurkish ? "Bağış Yap" : "Donate";
	public static string SettingsDonateDesc => IsTurkish ? "Geliştirmeyi destekleyin" : "Support development";
	public static string SettingsSubscribe => IsTurkish ? "Abone Ol" : "Subscribe";
	public static string SettingsSubscribeDesc => IsTurkish ? "Premium özelliklerin kilidini açın" : "Unlock premium features";
	public static string SettingsComingSoon => IsTurkish ? "Yakında" : "Soon";
	public static string SettingsComingSoonToast => IsTurkish ? "Bu özellik yakında eklenecek!" : "This feature is coming soon!";
	public static string SettingsSelectLanguage => IsTurkish ? "Dil Seçin" : "Select Language";
	public static string SettingsCancel => IsTurkish ? "İptal" : "Cancel";
	public static string SettingsLanguageChanged => IsTurkish ? "Dil Türkçe olarak ayarlandı." : "Language set to English.";

	// ── Login Page ──────────────────────────────────────────────

	public static string LoginWelcome => IsTurkish ? "Tekrar hoş geldin." : "Welcome back.";
	public static string LoginSubtitle => IsTurkish
		? "Antrenmanlarına, PR'larına ve performans kayıtlarına devam etmek için giriş yap."
		: "Login to continue building workouts, PRs, and performance records.";
	public static string LoginEmail => IsTurkish ? "E-posta" : "Email";
	public static string LoginEmailPlaceholder => IsTurkish ? "E-posta adresinizi girin" : "Enter your email";
	public static string LoginPassword => IsTurkish ? "Şifre" : "Password";
	public static string LoginPasswordPlaceholder => IsTurkish ? "Şifrenizi girin" : "Enter your password";
	public static string LoginButton => IsTurkish ? "Giriş Yap" : "Login";
	public static string LoginSignUp => IsTurkish ? "Kayıt Ol" : "Sign Up";
	public static string LoginErrorEmpty => IsTurkish ? "Lütfen e-posta ve şifrenizi girin." : "Please enter your email and password.";
	public static string LoginErrorFailed => IsTurkish ? "Giriş başarısız." : "Login failed.";

	// ── Register Page ───────────────────────────────────────────

	public static string RegisterTitle => IsTurkish ? "Kayıt Ol" : "Register";
	public static string RegisterHeadline => IsTurkish ? "Antrenman profilini oluştur." : "Start your training profile.";
	public static string RegisterSubtitle => IsTurkish
		? "Antrenmanlarını, PR'larını ve atletik performans verilerini kaydetmek için hesap oluştur."
		: "Create your account to save workouts, PRs, and athletic performance data.";
	public static string RegisterFirstName => IsTurkish ? "Ad" : "First Name";
	public static string RegisterFirstNamePlaceholder => IsTurkish ? "Adınızı girin" : "Enter your first name";
	public static string RegisterLastName => IsTurkish ? "Soyad" : "Last Name";
	public static string RegisterLastNamePlaceholder => IsTurkish ? "Soyadınızı girin" : "Enter your last name";
	public static string RegisterConfirmPassword => IsTurkish ? "Şifre Tekrar" : "Confirm Password";
	public static string RegisterConfirmPasswordPlaceholder => IsTurkish ? "Şifrenizi tekrar girin" : "Confirm your password";
	public static string RegisterPasswordRules => IsTurkish
		? "Şifre kuralları: en az 8 karakter, 1 büyük harf ve 1 özel karakter."
		: "Password rules: at least 8 characters, 1 uppercase letter, and 1 special character.";
	public static string RegisterButton => IsTurkish ? "Kayıt Ol" : "Sign Up";
	public static string RegisterErrorEmpty => IsTurkish ? "Lütfen tüm alanları doldurun." : "Please fill in all fields.";
	public static string RegisterErrorEmail => IsTurkish ? "Geçerli bir e-posta adresi girin." : "Please enter a valid email address.";
	public static string RegisterErrorPasswordLength => IsTurkish ? "Şifre en az 8 karakter olmalıdır." : "Password must be at least 8 characters.";
	public static string RegisterErrorPasswordUpper => IsTurkish ? "Şifre en az 1 büyük harf içermelidir." : "Password must include at least 1 uppercase letter.";
	public static string RegisterErrorPasswordSpecial => IsTurkish ? "Şifre en az 1 özel karakter içermelidir." : "Password must include at least 1 special character.";
	public static string RegisterErrorPasswordMismatch => IsTurkish ? "Şifreler eşleşmiyor." : "Passwords do not match.";
	public static string RegisterSuccessTitle => IsTurkish ? "Hesap oluşturuldu" : "Account created";
	public static string RegisterSuccessMessage => IsTurkish ? "Artık yeni hesabınızla giriş yapabilirsiniz." : "You can now log in with your new account.";
	public static string RegisterSuccessButton => IsTurkish ? "Girişe Git" : "Go to Login";
	public static string RegisterErrorFailed => IsTurkish ? "Kayıt başarısız." : "Registration failed.";

	// ── Change Password Page ────────────────────────────────────

	public static string ChangePasswordTitle => IsTurkish ? "Şifre Değiştir" : "Change Password";
	public static string ChangePasswordEmail => IsTurkish ? "E-posta" : "Email";
	public static string ChangePasswordEmailPlaceholder => IsTurkish ? "E-posta adresiniz" : "Your email address";
	public static string ChangePasswordCurrent => IsTurkish ? "Mevcut Şifre" : "Current Password";
	public static string ChangePasswordCurrentPlaceholder => IsTurkish ? "Mevcut şifrenizi girin" : "Enter your current password";
	public static string ChangePasswordNew => IsTurkish ? "Yeni Şifre" : "New Password";
	public static string ChangePasswordNewPlaceholder => IsTurkish ? "Yeni şifrenizi girin" : "Enter your new password";
	public static string ChangePasswordRepeat => IsTurkish ? "Yeni Şifre Tekrar" : "New Password Repeat";
	public static string ChangePasswordRepeatPlaceholder => IsTurkish ? "Yeni şifrenizi tekrar girin" : "Repeat your new password";
	public static string ChangePasswordRules => RegisterPasswordRules;
	public static string ChangePasswordButton => IsTurkish ? "Şifreyi Değiştir" : "Change Password";
	public static string ChangePasswordErrorEmpty => IsTurkish ? "Tüm alanları doldurun." : "Please fill in all fields.";
	public static string ChangePasswordErrorEmail => RegisterErrorEmail;
	public static string ChangePasswordErrorLength => RegisterErrorPasswordLength;
	public static string ChangePasswordErrorUpper => RegisterErrorPasswordUpper;
	public static string ChangePasswordErrorSpecial => RegisterErrorPasswordSpecial;
	public static string ChangePasswordErrorMismatch => IsTurkish ? "Yeni şifreler eşleşmiyor." : "New passwords do not match.";
	public static string ChangePasswordSuccess => IsTurkish ? "Şifre başarıyla değiştirildi." : "Password changed successfully.";
	public static string ChangePasswordErrorFailed => IsTurkish ? "Şifre değiştirilemedi." : "Failed to change password.";

	// ── Bottom Nav Bar ──────────────────────────────────────────

	public static string NavHome => IsTurkish ? "Ana Sayfa" : "Home";
	public static string NavWorkout => IsTurkish ? "Antrenman" : "Workout";
	public static string NavFreakAi => "FreakAI";
	public static string NavCalc => IsTurkish ? "Hesapla" : "Calc";
	public static string NavProfile => IsTurkish ? "Profil" : "Profile";

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
