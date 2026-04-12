using System.Globalization;

namespace FreakLete.Services;

/// <summary>
/// Client-side language service with persistence.
/// Provides localized strings for all user-facing UI surfaces.
/// Default language: English. Supported: en, tr.
/// </summary>
public static class AppLanguage
{
	private const string PreferenceKey = "app_language";

	public static string Code { get; private set; } = "en";
	public static bool IsTurkish => Code == "tr";

	/// <summary>
	/// Raised on the calling thread when <see cref="SetLanguage"/> changes the active language.
	/// Pages subscribe in OnAppearing and unsubscribe in OnDisappearing.
	/// </summary>
	public static event Action? LanguageChanged;

	public static void Initialize()
	{
		Code = Preferences.Default.Get(PreferenceKey, "en");
		var culture = new CultureInfo(Code);
		CultureInfo.DefaultThreadCurrentCulture = culture;
		CultureInfo.DefaultThreadCurrentUICulture = culture;
	}

	public static void SetLanguage(string code)
	{
		var newCode = code is "tr" ? "tr" : "en";
		bool changed = newCode != Code;
		Code = newCode;
		Preferences.Default.Set(PreferenceKey, Code);
		var culture = new CultureInfo(Code);
		CultureInfo.DefaultThreadCurrentCulture = culture;
		CultureInfo.DefaultThreadCurrentUICulture = culture;
		if (changed)
			LanguageChanged?.Invoke();
	}

	// ── Shared / Common ─────────────────────────────────────────
	public static string SharedBrowse => IsTurkish ? "Gözat" : "Browse";
	public static string SharedSave => IsTurkish ? "Kaydet" : "Save";
	public static string SharedCancel => IsTurkish ? "İptal" : "Cancel";
	public static string SharedDelete => IsTurkish ? "Sil" : "Delete";
	public static string SharedUpdate => IsTurkish ? "Güncelle" : "Update";
	public static string SharedContinue => IsTurkish ? "Devam" : "Continue";
	public static string SharedOk => IsTurkish ? "Tamam" : "OK";
	public static string SharedError => IsTurkish ? "Hata" : "Error";
	public static string SharedAll => IsTurkish ? "Tümü" : "All";
	public static string SharedNoMovementSelected => IsTurkish ? "Hareket seçilmedi" : "No movement selected";
	public static string SharedPleaseLogin => IsTurkish ? "Lütfen tekrar giriş yapın." : "Please log in again.";
	public static string SharedChooseMovement => IsTurkish ? "Kaydetmeden önce hareket seçin." : "Choose a movement before saving.";

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
	public static string SettingsRestorePurchases => IsTurkish ? "Satın Alımları Geri Yükle" : "Restore Purchases";
	public static string SettingsRestorePurchasesDesc => IsTurkish ? "Önceki satın alımlarınızı geri yükleyin" : "Restore your previous purchases";
	public static string SettingsManageSubscription => IsTurkish ? "Aboneliği Yönet" : "Manage Subscription";
	public static string SettingsManageSubscriptionDesc => IsTurkish ? "Google Play'de aboneliğinizi yönetin" : "Manage your subscription on Google Play";
	public static string SettingsPremiumActive => IsTurkish ? "Premium" : "Premium";
	public static string SettingsDonateChooseAmount => IsTurkish ? "Bağış Tutarı Seçin" : "Choose Donation Amount";
	public static string SettingsSubscribeChoosePlan => IsTurkish ? "Plan Seçin" : "Choose Plan";
	public static string SettingsMonthly => IsTurkish ? "Aylık" : "Monthly";
	public static string SettingsAnnual => IsTurkish ? "Yıllık" : "Annual";
	public static string SettingsPurchaseSuccess => IsTurkish ? "Satın alma başarılı!" : "Purchase successful!";
	public static string SettingsPurchaseCancelled => IsTurkish ? "Satın alma iptal edildi." : "Purchase cancelled.";
	public static string SettingsPurchaseError => IsTurkish ? "Satın alma hatası oluştu." : "Purchase error occurred.";
	public static string SettingsRestoreSuccess => IsTurkish ? "Satın alımlar geri yüklendi." : "Purchases restored.";
	public static string SettingsRestoreEmpty => IsTurkish ? "Geri yüklenecek satın alım bulunamadı." : "No purchases found to restore.";
	public static string SettingsBillingUnavailable => IsTurkish
		? "Satın alma şu anda yalnızca Android'de kullanılabilir."
		: "Purchases are currently only available on Android.";
	public static string SettingsDonationUnavailable => IsTurkish
		? "Bağış satın alımı şu anda kullanılamıyor."
		: "Donations are currently unavailable.";
	public static string SettingsSubscriptionUnavailable => IsTurkish
		? "Abonelik satın alımı şu anda kullanılamıyor."
		: "Subscriptions are currently unavailable.";
	public static string SettingsPurchaseAlreadyOwned => IsTurkish
		? "Bu ürün zaten sahip olduğunuz bir ürün."
		: "You already own this item.";
	// Donate-specific success (sync verified)
	public static string SettingsDonateSuccess => IsTurkish
		? "Bağışınız alındı, teşekkürler!"
		: "Donation received, thank you!";
	// Subscribe-specific success (sync verified)
	public static string SettingsSubscribeSuccess => IsTurkish
		? "Aboneliğiniz aktifleştirildi!"
		: "Subscription activated!";
	// Sync network/config failure
	public static string SettingsSyncFailed => IsTurkish
		? "Satın alma Play'de tamamlandı fakat sunucuya gönderilemedi. Satın alımları geri yükle seçeneğini deneyin."
		: "Purchase completed on Play but could not reach the server. Try Restore Purchases.";
	// Sync reached server but verification state was not confirmed
	public static string SettingsSyncVerificationFailed => IsTurkish
		? "Satın alma doğrulanamadı. Lütfen Satın Alımları Geri Yükle seçeneğini deneyin."
		: "Purchase could not be verified. Please try Restore Purchases.";
	// Restore: purchases found but none could be synced
	public static string SettingsRestoreAllFailed => IsTurkish
		? "Satın alımlar bulundu fakat sunucuya kaydedilemedi. Lütfen daha sonra tekrar deneyin."
		: "Purchases found but could not be synced to the server. Please try again later.";
	// Restore: some synced, some failed
	public static string SettingsRestorePartial => IsTurkish
		? "Satın alımlarınızın bir kısmı geri yüklendi."
		: "Some purchases were restored successfully.";
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
	public static string RegisterEyebrow => IsTurkish ? "HESAP OLUŞTUR" : "CREATE ACCOUNT";
	public static string RegisterHeadline => IsTurkish ? "Antrenman profilini oluştur." : "Start your training profile.";
	public static string RegisterSubtitle => IsTurkish
		? "Antrenmanlarını, PR'larını ve atletik performans verilerini kaydetmek için hesap oluştur."
		: "Create your account to save workouts, PRs, and athletic performance data.";
	public static string RegisterFirstName => IsTurkish ? "Ad" : "First Name";
	public static string RegisterFirstNamePlaceholder => IsTurkish ? "Adınızı girin" : "Enter your first name";
	public static string RegisterLastName => IsTurkish ? "Soyad" : "Last Name";
	public static string RegisterLastNamePlaceholder => IsTurkish ? "Soyadınızı girin" : "Enter your last name";
	public static string RegisterPasswordPlaceholder => IsTurkish ? "Şifre oluşturun" : "Create a password";
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

	// ── Home Page ───────────────────────────────────────────────
	public static string HomeWelcome => IsTurkish ? "HOŞ GELDİN" : "WELCOME";
	public static string HomeStartWorkout => IsTurkish ? "Antrenman Başlat" : "Start Workout";
	public static string HomeStart => IsTurkish ? "Başlat" : "Start";
	public static string HomeWorkoutsBadge => IsTurkish ? "ANTRENMAN" : "WORKOUTS";
	public static string HomeQuickWorkouts => IsTurkish ? "Hızlı Antrenmanlar" : "Quick Workouts";
	public static string HomeQuickWorkoutsDesc => IsTurkish
		? "Hızlıca başlayabileceğin hazır antrenman şablonları"
		: "Ready-made workout templates to get started quickly";
	public static string HomePickExercise1 => IsTurkish ? "Egzersiz 1 Seç" : "Pick Exercise 1";
	public static string HomePickExercise2 => IsTurkish ? "Egzersiz 2 Seç" : "Pick Exercise 2";
	public static string[] HomeDayAbbreviations => IsTurkish
		? ["Paz", "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt"]
		: ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];
	public static string FormatDaysPerWeek(int days) => IsTurkish ? $"{days} gün/hafta" : $"{days} days/week";
	public static string FormatXPerWeek(int days) => IsTurkish ? $"Haftada {days}x" : $"{days}x/week";

	// ── Workout Page ────────────────────────────────────────────
	public static string WorkoutPageTitle => IsTurkish ? "Programlar" : "Programs";
	public static string WorkoutPageSubtitle => IsTurkish ? "Antrenman planların" : "Your training plans";
	public static string WorkoutActiveProgram => IsTurkish ? "AKTİF PROGRAM" : "ACTIVE PROGRAM";
	public static string WorkoutStartWorkout => IsTurkish ? "Antrenman Başlat" : "Start Workout";
	public static string WorkoutGetStarted => IsTurkish ? "BAŞLA" : "GET STARTED";
	public static string WorkoutNoActiveProgram => IsTurkish ? "Aktif program yok" : "No active program";
	public static string WorkoutNoActiveDesc => IsTurkish
		? "FreakAI ile program oluştur veya hızlı antrenman başlat"
		: "Create a training program with FreakAI or start a quick workout";
	public static string WorkoutQuickWorkout => IsTurkish ? "Hızlı Antrenman" : "Quick Workout";
	public static string WorkoutThisWeek => IsTurkish ? "BU HAFTA" : "THIS WEEK";
	public static string WorkoutSessions => IsTurkish ? "seans" : "sessions";
	public static string WorkoutPrograms => IsTurkish ? "PROGRAMLAR" : "PROGRAMS";
	public static string WorkoutAvailable => IsTurkish ? "mevcut" : "available";
	public static string WorkoutRecommended => IsTurkish ? "Önerilen" : "Recommended";
	public static string WorkoutAllPrograms => IsTurkish ? "Tüm Programlar" : "All Programs";
	public static string WorkoutNoPrograms => IsTurkish ? "Henüz antrenman programı yok" : "No training programs yet";
	public static string WorkoutQuickAdd => IsTurkish ? "Hızlı Ekle" : "Quick Add";
	public static string WorkoutCalendar => IsTurkish ? "Takvim" : "Calendar";
	public static string WorkoutViewDetails => IsTurkish ? "Detaylar" : "View Details";
	public static string FormatThisWeek(int count) => IsTurkish ? $"Bu hafta {count}" : $"{count} this week";
	public static string FormatMinutes(int min) => $"{min} min";

	// ── Calendar Page ───────────────────────────────────────────
	public static string CalendarTitle => IsTurkish ? "Takvim" : "Calendar";
	public static string CalendarTrainingHistory => IsTurkish ? "ANTRENMAN GEÇMİŞİ" : "TRAINING HISTORY";
	public static string CalendarDesc => IsTurkish
		? "Günlük seanslarını gözden geçir ve programını takip et."
		: "Review sessions by day and keep your schedule visible.";
	public static string[] CalendarDayNames => IsTurkish
		? ["Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz"]
		: ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
	public static string CalendarSavedWorkouts => IsTurkish ? "Kayıtlı Antrenmanlar" : "Saved Workouts";
	public static string CalendarNoWorkout => IsTurkish ? "Bu tarihte kayıtlı antrenman yok." : "No workout saved for this date.";
	public static string FormatCalendarSelected(DateTime date) => IsTurkish ? $"Seçili: {date:dd MMM yyyy}" : $"Selected: {date:dd MMM yyyy}";
	public static string CalendarDeleteTitle => IsTurkish ? "Antrenmanı Sil" : "Delete Workout";
	public static string FormatCalendarDeleteConfirm(string name) => IsTurkish ? $"'{name}' silinsin mi?" : $"Delete '{name}'?";

	// ── Calculations Page ───────────────────────────────────────
	public static string CalcPageTitle => IsTurkish ? "İlerleme" : "Progress";
	public static string CalcPageSubtitle => IsTurkish ? "Kişisel rekorlarını takip et" : "Track your personal records";
	public static string CalcNoPrs => IsTurkish ? "Henüz PR kaydedilmedi" : "No PRs recorded yet";
	public static string CalcNoPrsDesc => IsTurkish
		? "İlerlemenizi görmek için aşağıdan kişisel rekor kaydedin"
		: "Save a personal record below to see your progress here";
	public static string CalcPrProgress => IsTurkish ? "PR İlerlemesi" : "PR Progress";
	public static string CalcStrengthTools => IsTurkish ? "Güç & Atletik Araçlar" : "Strength & Athletic Tools";
	public static string CalcStrengthEstimate => IsTurkish ? "Güç Tahmini" : "Strength Estimate";
	public static string CalcStrengthMovement => IsTurkish ? "Güç hareketi" : "Strength movement";
	public static string CalcNoStrengthSelected => IsTurkish ? "Güç hareketi seçilmedi" : "No strength movement selected";
	public static string CalcStrengthHint => IsTurkish ? "Ağırlıklı hareketleri gözatarak 1RM tahmini yapın." : "Browse weighted movements for the 1RM estimate.";
	public static string CalcWeightKgRange => IsTurkish ? "Ağırlık (kg): 40 - 250" : "Weight (kg): 40 - 250";
	public static string CalcRepsRange => IsTurkish ? "Tekrar: 1 - 8" : "Reps: 1 - 8";
	public static string CalcRirRange => IsTurkish ? "RIR: 0 - 5" : "RIR: 0 - 5";
	public static string CalcConcentricTime => IsTurkish ? "Konsantrik Süre (s)" : "Concentric Time (s)";
	public static string CalcCalculate => IsTurkish ? "Hesapla" : "Calculate";
	public static string CalcCalculatedRange => IsTurkish ? "Hesaplanan Aralık" : "Calculated Range";
	public static string CalcSavePr => IsTurkish ? "PR Kaydet" : "Save PR";
	public static string CalcUpdatePr => IsTurkish ? "PR Güncelle" : "Update PR";
	public static string CalcMovement => IsTurkish ? "Hareket" : "Movement";
	public static string CalcNoPrSelected => IsTurkish ? "PR hareketi seçilmedi" : "No PR movement selected";
	public static string CalcPrHint => IsTurkish ? "PR kaydetmeden önce hareketleri gözatın." : "Browse gym and athletic movements before saving a PR.";
	public static string CalcWeightKg => IsTurkish ? "Ağırlık (kg)" : "Weight (kg)";
	public static string CalcReps => IsTurkish ? "Tekrar" : "Reps";
	public static string CalcRir => "RIR";
	public static string CalcGroundContactTime => IsTurkish ? "Yer Temas Süresi (s)" : "Ground Contact Time (s)";
	public static string CalcSavedPrEntries => IsTurkish ? "Kayıtlı PR'lar" : "Saved PR Entries";
	public static string CalcNoSavedPr => IsTurkish ? "Henüz kayıtlı PR yok." : "No saved PR yet.";
	public static string CalcReactiveStrength => IsTurkish ? "Reaktif Güç İndeksi" : "Reactive Strength Index";
	public static string CalcRsiDesc => IsTurkish
		? "RSI, reaktif çıktıyı tahmin etmek için sıçrama yüksekliği ve yer temas süresini kullanır."
		: "RSI uses jump height and ground contact time to estimate reactive output.";
	public static string CalcJumpHeight => IsTurkish ? "Sıçrama Yüksekliği (cm)" : "Jump Height (cm)";
	public static string CalcGctS => IsTurkish ? "YTS (s)" : "GCT (s)";
	public static string CalcCalculateRsi => IsTurkish ? "RSI Hesapla" : "Calculate RSI";
	public static string CalcResult => IsTurkish ? "Sonuç" : "Result";
	public static string CalcNoRsiYet => IsTurkish ? "Henüz RSI hesaplanmadı." : "No RSI calculated yet.";
	public static string CalcNoOneRmYet => IsTurkish ? "Henüz 1RM hesaplanmadı." : "No 1RM calculated yet.";
	public static string CalcChooseStrength => IsTurkish ? "Güç Hareketi Seç" : "Choose Strength Exercise";
	public static string CalcChoosePr => IsTurkish ? "PR Hareketi Seç" : "Choose PR Movement";
	public static string CalcEstimated1Rm => IsTurkish ? "TAHMİNİ 1RM (KG)" : "ESTIMATED 1RM (KG)";
	public static string CalcEstimated1RmCaption => IsTurkish ? "Tahmini 1RM" : "Estimated 1RM";
	public static string CalcBestValue => IsTurkish ? "EN İYİ DEĞER" : "BEST VALUE";
	public static string FormatBestPr(DateTime date) => IsTurkish ? $"En iyi PR: {date:dd MMM yyyy}" : $"Best PR: {date:MMM dd, yyyy}";
	public static string FormatProgress(string name) => IsTurkish ? $"{name} İlerlemesi" : $"{name} Progress";
	public static string FormatMovement(string name) => $"Movement: {name}";
	public static string CalcPrSaved => IsTurkish ? "PR kaydedildi." : "Saved PR added.";
	public static string CalcPrUpdated => IsTurkish ? "PR güncellendi." : "Saved PR updated.";
	public static string CalcPrDeleted => IsTurkish ? "PR silindi." : "Saved PR deleted.";
	public static string CalcPrFailedSave => IsTurkish ? "PR kaydedilemedi." : "Failed to save PR.";
	public static string CalcPrFailedUpdate => IsTurkish ? "PR güncellenemedi." : "Failed to update PR.";
	public static string FormatEditing(string text) => IsTurkish ? $"Düzenleniyor: {text}" : $"Editing: {text}";
	public static string CalcJumpHeightError => IsTurkish ? "Sıçrama yüksekliği pozitif bir sayı olmalıdır." : "Jump height must be a positive number.";
	public static string CalcGctError => IsTurkish ? "YTS pozitif bir sayı olmalıdır." : "GCT must be a positive number.";
	public static string CalcDeletePrTitle => IsTurkish ? "PR Sil" : "Delete PR";
	public static string FormatDeleteConfirm(string text) => IsTurkish ? $"'{text}' silinsin mi?" : $"Delete '{text}'?";

	// ── FFMI ────────────────────────────────────────────────
	public static string CalcFfmiTitle => IsTurkish ? "Yağsız Kütle İndeksi" : "Fat-Free Mass Index";
	public static string CalcFfmiDesc => IsTurkish
		? "FFMI, yağsız vücut kütlenizi boy uzunluğunuza göre değerlendirir."
		: "FFMI evaluates your lean body mass relative to your height.";
	public static string CalcFfmiMissingData => IsTurkish
		? "FFMI hesaplamak için profilinizde kilo, boy ve vücut yağ oranı gereklidir."
		: "FFMI requires weight, height, and body fat percentage in your profile.";
	public static string CalcFfmiGoToProfile => IsTurkish ? "Profili Düzenle" : "Edit Profile";
	public static string CalcFfmiCalculate => IsTurkish ? "FFMI Hesapla" : "Calculate FFMI";
	public static string CalcFfmiNormalized => "FFMI";
	public static string CalcFfmiRaw => IsTurkish ? "Ham FFMI" : "Raw FFMI";
	public static string CalcFfmiLbm => IsTurkish ? "Yağsız Kütle (kg)" : "Lean Body Mass (kg)";
	public static string CalcFfmiNoResult => IsTurkish ? "Henüz FFMI hesaplanmadı." : "No FFMI calculated yet.";
	public static string CalcFfmiWeightLabel => IsTurkish ? "Kilo (kg)" : "Weight (kg)";
	public static string CalcFfmiHeightLabel => IsTurkish ? "Boy (cm)" : "Height (cm)";
	public static string CalcFfmiBodyFatLabel => IsTurkish ? "Vücut Yağ Oranı (%)" : "Body Fat (%)";
	public static string CalcFfmiWeightError => IsTurkish ? "Kilo pozitif bir sayı olmalıdır." : "Weight must be a positive number.";
	public static string CalcFfmiHeightError => IsTurkish ? "Boy pozitif bir sayı olmalıdır." : "Height must be a positive number.";
	public static string CalcFfmiBodyFatError => IsTurkish ? "Vücut yağ oranı 0 ile 100 arasında olmalıdır." : "Body fat must be between 0 and 100.";

	// ── Calculation Insights ────────────────────────────────────
	public static string InsightTitle => IsTurkish ? "Analiz" : "Analysis";

	public static string InsightBandLabel(CalculationInsightResolver.InsightBand band) => band switch
	{
		CalculationInsightResolver.InsightBand.NeedsWork => IsTurkish ? "Gelistirilmeli" : "Needs Work",
		CalculationInsightResolver.InsightBand.Adequate  => IsTurkish ? "Idare Eder"    : "Adequate",
		CalculationInsightResolver.InsightBand.Good      => IsTurkish ? "Iyi"           : "Good",
		CalculationInsightResolver.InsightBand.Elite     => IsTurkish ? "Elit"          : "Elite",
		_                                                 => ""
	};

	// 1RM
	public static string InsightOneRmSummary(CalculationInsightResolver.InsightBand band, string ratio) => band switch
	{
		CalculationInsightResolver.InsightBand.NeedsWork => IsTurkish
			? $"Vücut ağırlığına göre kaldırma oranın ({ratio}x) henüz temel eşiğin altında. Teknik ve kuvvet geliştirmeye odaklan."
			: $"Your bodyweight-relative lift ratio ({ratio}x) is below the baseline threshold. Focus on technique and progressive overload.",
		CalculationInsightResolver.InsightBand.Adequate  => IsTurkish
			? $"Kaldırma oranın ({ratio}x) antrenmanlı sporcu kitlesinde orta düzeyde. Düzenli antrenmanla ilerleyebilirsin."
			: $"Your lift ratio ({ratio}x) sits in the mid-range for trained athletes. Consistent training should push this higher.",
		CalculationInsightResolver.InsightBand.Good      => IsTurkish
			? $"Kaldırma oranın ({ratio}x) antrenmanlı popülasyonun üst diliminde. Güçlü bir taban var."
			: $"Your lift ratio ({ratio}x) places you in the upper tier of the trained population. Solid foundation.",
		CalculationInsightResolver.InsightBand.Elite     => IsTurkish
			? $"Kaldırma oranın ({ratio}x) rekabetçi sporcu eşiğinde veya üzerinde. Bu seviyeye ulaşmak ciddi antrenman gerektirir."
			: $"Your lift ratio ({ratio}x) meets or exceeds competitive athlete thresholds. Reaching this level requires serious training commitment.",
		_                                                  => ""
	};

	public static string InsightOneRmSportContext(CalculationInsightResolver.InsightBand band, string movement) => IsTurkish
		? $"{movement} için güç antrenmanı yapan sporculara göre değerlendirme."
		: $"Evaluated against strength-trained athletes working with {movement}.";

	public static string InsightOneRmGlobalContext(CalculationInsightResolver.InsightBand band) => IsTurkish
		? "Referans: Antrenman geçmişi olan geniş sporcu popülasyonu verileri."
		: "Reference: Broad trained-athlete population data.";

	// RSI
	public static string InsightRsiSummary(CalculationInsightResolver.InsightBand band, string rsiStr) => band switch
	{
		CalculationInsightResolver.InsightBand.NeedsWork => IsTurkish
			? $"RSI değerin ({rsiStr}) temel sporcu eşiğinin altında. Reaktif güç antrenmanı faydalı olacaktır."
			: $"Your RSI ({rsiStr}) is below the general athlete baseline. Reactive strength training would be beneficial.",
		CalculationInsightResolver.InsightBand.Adequate  => IsTurkish
			? $"RSI değerin ({rsiStr}) genel sporcu ortalamasına yakın. Patlayıcı çalışmalar ile gelişim mümkün."
			: $"Your RSI ({rsiStr}) is near the general athlete average. Plyometric and reactive work can move this upward.",
		CalculationInsightResolver.InsightBand.Good      => IsTurkish
			? $"RSI değerin ({rsiStr}) antrenmanlı sporcu grubunda iyi bir seviyede."
			: $"Your RSI ({rsiStr}) is at a solid level for trained athletes.",
		CalculationInsightResolver.InsightBand.Elite     => IsTurkish
			? $"RSI değerin ({rsiStr}) elit sporcu eşiğinde. Reaktif güç açısından üst dilimdesin."
			: $"Your RSI ({rsiStr}) is at the elite athlete threshold. You're in the top tier for reactive strength.",
		_                                                  => ""
	};

	public static string InsightRsiSportContext(CalculationInsightResolver.InsightBand band, string? sport) =>
		!string.IsNullOrWhiteSpace(sport)
			? (IsTurkish
				? $"{sport} sporunda reaktif güç kritik bir performans bileşenidir."
				: $"Reactive strength is a critical performance component in {sport}.")
			: (IsTurkish
				? "Reaktif güç; sıçrama, sprint ve çeviklik gerektiren sporlarda kritik bir bileşendir."
				: "Reactive strength is critical in sports requiring jumping, sprinting, and agility.");

	public static string InsightRsiGlobalContext(CalculationInsightResolver.InsightBand band) => IsTurkish
		? "Referans: Genel sporcu popülasyonu RSI bant eşikleri (1.0 / 2.0 / 3.0)."
		: "Reference: General athlete population RSI band thresholds (1.0 / 2.0 / 3.0).";

	// FFMI
	public static string InsightFfmiSummary(CalculationInsightResolver.InsightBand band, string ffmiStr) => band switch
	{
		CalculationInsightResolver.InsightBand.NeedsWork => IsTurkish
			? $"FFMI değerin ({ffmiStr}) antrenmanlı sporcu tabanının altında. Protein alımı ve hipertrofi çalışması faydalı olabilir."
			: $"Your FFMI ({ffmiStr}) is below the trained-athlete baseline. Hypertrophy-focused training and protein intake may help.",
		CalculationInsightResolver.InsightBand.Adequate  => IsTurkish
			? $"FFMI değerin ({ffmiStr}) aktif popülasyonun orta kesiminde."
			: $"Your FFMI ({ffmiStr}) sits in the middle range of the active population.",
		CalculationInsightResolver.InsightBand.Good      => IsTurkish
			? $"FFMI değerin ({ffmiStr}) antrenmanlı sporcu grubunda iyi bir yağsız kütle düzeyini gösteriyor."
			: $"Your FFMI ({ffmiStr}) reflects a solid lean mass level in the trained-athlete group.",
		CalculationInsightResolver.InsightBand.Elite     => IsTurkish
			? $"FFMI değerin ({ffmiStr}) rekabetçi sporcu tabanının üst diliminde."
			: $"Your FFMI ({ffmiStr}) is in the upper tier of the competitive athlete baseline.",
		_                                                  => ""
	};

	public static string InsightFfmiSportContext(CalculationInsightResolver.InsightBand band) => IsTurkish
		? "Yağsız kütle indeksi, güç/kütle sporlarda performansla ilişkili bir göstergedir."
		: "Lean mass index is a performance-relevant indicator in strength and physique sports.";

	public static string InsightFfmiGlobalContext(CalculationInsightResolver.InsightBand band, string? sex)
	{
		bool isFemale = sex is not null && (sex.Equals("female", StringComparison.OrdinalIgnoreCase)
			|| sex.Equals("f", StringComparison.OrdinalIgnoreCase)
			|| sex.Equals("kadin", StringComparison.OrdinalIgnoreCase)
			|| sex.Equals("kadın", StringComparison.OrdinalIgnoreCase));
		return IsTurkish
			? $"Referans: {(isFemale ? "Kadın" : "Erkek")} sporcu popülasyonu FFMI bantları."
			: $"Reference: {(isFemale ? "Female" : "Male")} athlete population FFMI bands.";
	}

	// ── Profile Page ────────────────────────────────────────────
	public static string ProfileWorkouts => IsTurkish ? "Antrenman" : "Workouts";
	public static string ProfileSavedPrs => IsTurkish ? "PR'lar" : "Saved PRs";
	public static string ProfileRecords => IsTurkish ? "Kayıtlar" : "Records";
	public static string ProfileHighlights => IsTurkish ? "Öne Çıkanlar" : "Highlights";
	public static string ProfileDetails => IsTurkish ? "Profil Detayları" : "Profile Details";
	public static string ProfileDateOfBirth => IsTurkish ? "Doğum Tarihi" : "Date of Birth";
	public static string ProfileSelectDob => IsTurkish ? "Doğum tarihini seçin" : "Select date of birth";
	public static string ProfileAge => IsTurkish ? "Yaş: -" : "Age: -";
	public static string ProfileWeightKg => IsTurkish ? "Ağırlık (kg)" : "Weight (kg)";
	public static string ProfileBodyFat => IsTurkish ? "Vücut Yağı (%)" : "Body Fat (%)";
	public static string ProfileSport => IsTurkish ? "Spor" : "Sport";
	public static string ProfileSelectSport => IsTurkish ? "Spor dalınızı seçin" : "Select your sport";
	public static string ProfilePosition => IsTurkish ? "Pozisyon / Branş" : "Position / Discipline";
	public static string ProfileSelectPosition => IsTurkish ? "Pozisyonunuzu seçin" : "Select your position";
	public static string ProfileHeightCm => IsTurkish ? "Boy (cm)" : "Height (cm)";
	public static string ProfileSex => IsTurkish ? "Cinsiyet" : "Sex";
	public static string ProfileSelectSex => IsTurkish ? "Cinsiyet seçin" : "Select sex";
	public static string ProfileSexTitle => IsTurkish ? "Cinsiyet Seç" : "Select Sex";
	public static string ProfileGymExperience => IsTurkish ? "Spor Salonu Deneyimi" : "Gym Experience";
	public static string ProfileSelectExperience => IsTurkish ? "Deneyim seviyesini seçin" : "Select experience level";
	public static string ProfileCoachProfile => IsTurkish ? "Koç Profili" : "Coach Profile";
	public static string ProfileTrainingDays => IsTurkish ? "Haftalık Antrenman Günü" : "Training Days / Week";
	public static string ProfileSelectDays => IsTurkish ? "Haftalık gün seçin" : "Select days per week";
	public static string ProfileSessionDuration => IsTurkish ? "Seans Süresi (dakika)" : "Session Duration (minutes)";
	public static string ProfileSelectDuration => IsTurkish ? "Seans süresini seçin" : "Select session duration";
	public static string ProfilePrimaryGoal => IsTurkish ? "Birincil Antrenman Hedefi" : "Primary Training Goal";
	public static string ProfileSelectPrimaryGoal => IsTurkish ? "Birincil hedefinizi seçin" : "Select your primary goal";
	public static string ProfileSecondaryGoal => IsTurkish ? "İkincil Hedef (opsiyonel)" : "Secondary Goal (optional)";
	public static string ProfileSelectSecondaryGoal => IsTurkish ? "İkincil hedef seçin" : "Select secondary goal";
	public static string ProfileEquipment => IsTurkish ? "Mevcut Ekipman" : "Available Equipment";
	public static string ProfileSelectEquipment => IsTurkish ? "Ekipman erişimini seçin" : "Select equipment access";
	public static string ProfileInjuryHistory => IsTurkish ? "Sakatlık Geçmişi" : "Injury History";
	public static string ProfileInjuryPlaceholder => IsTurkish
		? "Örn. 2023 sol diz ACL rekonstrüksiyonu, kronik omuz sıkışması..."
		: "e.g. ACL reconstruction 2023 left knee, chronic shoulder impingement...";
	public static string ProfileCurrentPain => IsTurkish ? "Mevcut Ağrı Noktaları" : "Current Pain Points";
	public static string ProfilePainPlaceholder => IsTurkish
		? "Örn. Derin squatta hafif sağ diz rahatsızlığı..."
		: "e.g. Mild right knee discomfort during deep squats...";
	public static string ProfileLimitations => IsTurkish ? "Fiziksel Kısıtlamalar" : "Physical Limitations";
	public static string ProfileLimitationsPlaceholder => IsTurkish
		? "Örn. Sınırlı overhead mobilitesi, boyun arkası hareketler yapamaz..."
		: "e.g. Limited overhead mobility, can't do behind-neck movements...";
	public static string ProfileDietaryPreference => IsTurkish ? "Beslenme Tercihi (opsiyonel)" : "Dietary Preference (optional)";
	public static string ProfileSelectDietary => IsTurkish ? "Beslenme tercihini seçin" : "Select dietary preference";
	public static string ProfileAthleticPerformance => IsTurkish ? "Atletik Performans" : "Athletic Performance";
	public static string ProfileNoPerformance => IsTurkish ? "Henüz atletik performans kaydı yok." : "No athletic performance records yet.";
	public static string ProfileBrowsePerformanceHint => IsTurkish
		? "Sprint, sıçrama, pliyometrik ve olimpik kaldırma hareketlerini gözatın."
		: "Browse sprint, jump, plyo, and Olympic lift movements.";
	public static string ProfileResult => IsTurkish ? "Sonuç" : "Result";
	public static string ProfileEnterResult => IsTurkish ? "Sonuç girin" : "Enter result";
	public static string ProfileSecondResult => IsTurkish ? "İkinci Sonuç" : "Second Result";
	public static string ProfileEnterSecondResult => IsTurkish ? "İkinci sonuç girin" : "Enter second result";
	public static string ProfileTiming => IsTurkish ? "Zamanlama" : "Timing";
	public static string ProfileTimingPlaceholder => IsTurkish ? "Opsiyonel (örn. 1.96)" : "Optional (e.g. 1.96)";
	public static string ProfileMovementGoals => IsTurkish ? "Hareket Hedefleri" : "Movement Goals";
	public static string ProfileGoalMovement => IsTurkish ? "Hedef hareketi" : "Goal movement";
	public static string ProfileGoalHint => IsTurkish
		? "Egzersiz kataloğundan bir hareket seçin ve ana metriğine hedef belirleyin."
		: "Browse the exercise catalog and set a target on the movement's main metric.";
	public static string ProfileTargetValue => IsTurkish ? "Hedef değer" : "Target value";
	public static string ProfileNoGoals => IsTurkish ? "Henüz hareket hedefi yok." : "No movement goals set yet.";
	public static string ProfileSettings => IsTurkish ? "⚙  Ayarlar" : "⚙  Settings";
	public static string ProfileLogout => IsTurkish ? "Çıkış Yap" : "Logout";
	public static string ProfileDeleteAccount => IsTurkish ? "Sil" : "Delete";
	public static string ProfileFailedLoad => IsTurkish ? "Profil yüklenemedi." : "Failed to load profile.";
	public static string ProfileSaved => IsTurkish ? "Profil kaydedildi." : "Profile saved.";
	public static string ProfileFailedSave => IsTurkish ? "Profil kaydedilemedi." : "Failed to save profile.";
	public static string ProfileCoachSaved => IsTurkish ? "Koç profili kaydedildi." : "Coach profile saved.";
	public static string ProfileCoachFailedSave => IsTurkish ? "Koç profili kaydedilemedi." : "Failed to save coach profile.";
	public static string ProfileAthleteFailedSave => IsTurkish ? "Atlet profili kaydedilemedi." : "Failed to save athlete profile.";
	public static string ProfilePerformanceAdded => IsTurkish ? "Atletik performans eklendi." : "Athletic performance added.";
	public static string ProfilePerformanceUpdated => IsTurkish ? "Atletik performans güncellendi." : "Athletic performance updated.";
	public static string ProfilePerformanceDeleted => IsTurkish ? "Atletik performans silindi." : "Athletic performance deleted.";
	public static string ProfileGoalSaved => IsTurkish ? "Hareket hedefi kaydedildi." : "Movement goal saved.";
	public static string ProfileGoalUpdated => IsTurkish ? "Hareket hedefi güncellendi." : "Movement goal updated.";
	public static string ProfileGoalDeleted => IsTurkish ? "Hareket hedefi silindi." : "Movement goal deleted.";
	public static string ProfileChooseMovementError => IsTurkish ? "Bir hareket seçip geçerli bir sonuç girin." : "Choose a movement and enter a valid result.";
	public static string ProfileGoalError => IsTurkish ? "Hedef hareketi ve değeri gereklidir, değer pozitif olmalıdır." : "Goal movement and target value are required, and target must be positive.";
	public static string ProfileFailedGeneric => IsTurkish ? "İşlem başarısız." : "Failed to save.";
	public static string ProfileFailedDelete => IsTurkish ? "Silme başarısız." : "Failed to delete.";
	public static string ProfileFailedUpdate => IsTurkish ? "Güncelleme başarısız." : "Failed to update.";
	public static string ProfileDeleteEntryTitle => IsTurkish ? "Kaydı Sil" : "Delete Entry";
	public static string ProfileDeleteGoalTitle => IsTurkish ? "Hedefi Sil" : "Delete Goal";
	public static string ProfileNoHighlights => IsTurkish
		? "Henüz öne çıkan yok. Başlamak için bir antrenman tamamlayın veya PR kaydedin!"
		: "No highlights yet. Complete a workout or save a PR to get started!";
	// Highlights
	public static string ProfileHighlightFirstWorkout => IsTurkish ? "İlk Antrenman" : "First Workout";
	public static string ProfileHighlightFirstWorkoutDesc => IsTurkish ? "İlk antrenmanını tamamladın" : "Completed your first workout";
	public static string ProfileHighlightFirstPr => IsTurkish ? "İlk PR" : "First PR";
	public static string ProfileHighlightFirstPrDesc => IsTurkish ? "İlk kişisel rekorunu kaydetttin" : "Recorded your first personal record";
	public static string ProfileHighlightConsistent => IsTurkish ? "Tutarlı" : "Consistent";
	public static string ProfileHighlightConsistentDesc => IsTurkish ? "10+ antrenman tamamladın" : "Completed 10+ workouts";
	public static string ProfileHighlightPerformance => IsTurkish ? "Performans Kaydedildi" : "Performance Logged";
	public static string ProfileHighlightPerformanceDesc => IsTurkish ? "Atletik performans verisi kaydedildi" : "Logged athletic performance data";
	public static string ProfileHighlightGoalSetter => IsTurkish ? "Hedef Koyucu" : "Goal Setter";
	public static string ProfileHighlightGoalSetterDesc => IsTurkish ? "En az bir hareket hedefi belirlendi" : "Set at least one movement goal";
	// Selectors
	public static string ProfileSelectSportTitle => IsTurkish ? "Spor Seç" : "Select Sport";
	public static string ProfileSelectPositionTitle => IsTurkish ? "Pozisyon Seç" : "Select Position";
	public static string ProfileGymExperienceTitle => IsTurkish ? "Spor Salonu Deneyimi" : "Gym Experience";
	public static string ProfileTrainingDaysTitle => IsTurkish ? "Haftalık Antrenman Günü" : "Training Days / Week";
	public static string ProfileSessionDurationTitle => IsTurkish ? "Seans Süresi" : "Session Duration";
	public static string ProfilePrimaryGoalTitle => IsTurkish ? "Birincil Hedef" : "Primary Goal";
	public static string ProfileSecondaryGoalTitle => IsTurkish ? "İkincil Hedef" : "Secondary Goal";
	public static string ProfileDietaryTitle => IsTurkish ? "Beslenme Tercihi" : "Dietary Preference";
	public static string ProfileEquipmentTitle => IsTurkish ? "Ekipman Erişimi" : "Equipment Access";
	// Ground contact / concentric errors
	public static string ProfileGctError => IsTurkish ? "Yer temas süresi pozitif bir sayı olmalıdır." : "Ground contact time must be a positive number.";
	public static string ProfileConcentricError => IsTurkish ? "Konsantrik süre pozitif bir sayı olmalıdır." : "Concentric time must be a positive number.";
	// Delete account / logout
	public static string ProfileLogoutTitle => IsTurkish ? "Çıkış" : "Logout";
	public static string ProfileLogoutConfirm => IsTurkish ? "Çıkış yapmak istediğinize emin misiniz?" : "Are you sure you want to log out?";
	public static string ProfileDeleteTitle => IsTurkish ? "Hesabı Sil" : "Delete Account";
	public static string ProfileDeleteConfirm => IsTurkish
		? "Bu işlem geri alınamaz. Tüm verileriniz silinecek."
		: "This action cannot be undone. All your data will be deleted.";
	public static string ProfileDeletePasswordPrompt => IsTurkish
		? "Devam etmek için mevcut şifrenizi girin."
		: "Enter your current password to continue.";

	// ── NewWorkout Page ─────────────────────────────────────────
	public static string NewWorkoutTitle => IsTurkish ? "Yeni Antrenman" : "New Workout";
	public static string NewWorkoutEditTitle => IsTurkish ? "Antrenmanı Düzenle" : "Edit Workout";
	public static string NewWorkoutSessionSetup => IsTurkish ? "SEANS AYARLARI" : "SESSION SETUP";
	public static string NewWorkoutAddDesc => IsTurkish
		? "Tarihi belirle, antrenmanına isim ver, ardından seansa egzersiz ekle."
		: "Set the date, name your workout, then add exercises to the session.";
	public static string NewWorkoutEditDesc => IsTurkish
		? "Antrenman detaylarını güncelle, egzersizleri düzenle ve yeni seans versiyonunu kaydet."
		: "Update the workout details, adjust exercises, and save the new session version.";
	public static string NewWorkoutDetails => IsTurkish ? "Antrenman Detayları" : "Workout Details";
	public static string NewWorkoutDate => IsTurkish ? "Tarih" : "Date";
	public static string NewWorkoutName => IsTurkish ? "Antrenman Adı" : "Workout Name";
	public static string NewWorkoutNamePlaceholder => IsTurkish ? "Örnek: Push Günü" : "Example: Push Day";
	public static string NewWorkoutExerciseBuilder => IsTurkish ? "Egzersiz Oluşturucu" : "Exercise Builder";
	public static string NewWorkoutChooseExercise => IsTurkish ? "Egzersiz seçin" : "Choose exercise";
	public static string NewWorkoutNoExercise => IsTurkish ? "Egzersiz seçilmedi" : "No exercise selected";
	public static string NewWorkoutExerciseHint => IsTurkish
		? "Hareket kütüphanenizi açmak için gözata dokunun."
		: "Tap browse to open your recommended movement library.";
	public static string NewWorkoutSetCount => IsTurkish ? "Set Sayısı" : "Set Count";
	public static string NewWorkoutRepCount => IsTurkish ? "Tekrar Sayısı" : "Rep Count";
	public static string NewWorkoutRir => IsTurkish ? "RIR" : "RIR";
	public static string NewWorkoutRestSeconds => IsTurkish ? "Dinlenme (saniye)" : "Rest Seconds";
	public static string NewWorkoutConcentricTime => IsTurkish ? "Konsantrik Süre (s)" : "Concentric Time (s)";
	public static string NewWorkoutGctLabel => IsTurkish ? "Yer Temas Süresi (s)" : "Ground Contact Time (s)";
	public static string NewWorkoutAdd => IsTurkish ? "Ekle" : "Add";
	public static string NewWorkoutSessionExercises => IsTurkish ? "Seans Egzersizleri" : "Session Exercises";
	public static string NewWorkoutNoExerciseAdded => IsTurkish ? "Henüz egzersiz eklenmedi." : "No exercise added yet.";
	public static string NewWorkoutSave => IsTurkish ? "Antrenmanı Kaydet" : "Save Workout";
	public static string NewWorkoutSaveChanges => IsTurkish ? "Değişiklikleri Kaydet" : "Save Changes";
	public static string NewWorkoutChooseExerciseTitle => IsTurkish ? "Egzersiz Seç" : "Choose Exercise";
	public static string NewWorkoutNameRequired => IsTurkish ? "Egzersiz eklemeden önce antrenman adı gereklidir." : "Workout name is required before adding exercises.";
	public static string NewWorkoutChooseFirst => IsTurkish ? "Lütfen önce bir egzersiz seçin." : "Please choose an exercise first.";
	public static string NewWorkoutWorkoutNameRequired => IsTurkish ? "Antrenman adı gereklidir." : "Workout name is required.";
	public static string NewWorkoutAddAtLeastOne => IsTurkish ? "Onaylamadan önce en az bir egzersiz ekleyin." : "Add at least one exercise before confirm.";
	public static string NewWorkoutCouldNotLoad => IsTurkish ? "Antrenman yüklenemedi." : "Workout could not be loaded.";
	public static string NewWorkoutFailedSave => IsTurkish ? "Antrenman kaydedilemedi." : "Failed to save workout.";
	public static string NewWorkoutFailedUpdate => IsTurkish ? "Antrenman güncellenemedi." : "Failed to update workout.";
	public static string NewWorkoutSetError => IsTurkish ? "Set sayısı gereklidir ve pozitif olmalıdır." : "Set count is required and must be a positive number.";
	public static string NewWorkoutRepError => IsTurkish ? "Tekrar sayısı gereklidir ve pozitif olmalıdır." : "Rep count is required and must be a positive number.";
	public static string NewWorkoutRirError => IsTurkish ? "RIR 0-5 arasında olmalıdır." : "RIR must be between 0 - 5.";
	public static string NewWorkoutRestError => IsTurkish ? "Dinlenme süresi pozitif olmalıdır." : "Rest seconds must be a positive number.";
	public static string NewWorkoutConcentricError => IsTurkish ? "Konsantrik süre pozitif olmalıdır." : "Concentric time must be a positive number.";
	public static string NewWorkoutGctError => IsTurkish ? "Yer temas süresi pozitif olmalıdır." : "Ground contact time must be a positive number.";
	public static string FormatItemCount(int count) => IsTurkish ? $"{count} öğe" : count == 1 ? "1 item" : $"{count} items";

	// ── Program Detail Page ─────────────────────────────────────
	public static string ProgramDetailTitle => IsTurkish ? "Program Detayları" : "Program Details";
	public static string ProgramDetailTrainingProgram => IsTurkish ? "ANTRENMAN PROGRAMI" : "TRAINING PROGRAM";
	public static string ProgramDetailTemplate => IsTurkish ? "ŞABLON" : "TEMPLATE";
	public static string ProgramDetailWeeks => IsTurkish ? "Hafta" : "Weeks";
	public static string ProgramDetailSessions => IsTurkish ? "Seans" : "Sessions";
	public static string ProgramDetailExercises => IsTurkish ? "Egzersiz" : "Exercises";
	public static string FormatWeek(int num) => IsTurkish ? $"Hafta {num}" : $"Week {num}";
	public static string FormatDay(int num) => IsTurkish ? $"Gün {num}" : $"Day {num}";
	public static string ProgramDetailStart => IsTurkish ? "Başla" : "Start";
	public static string ProgramDetailAddWorkout => IsTurkish ? "Antrenman Ekle" : "Add Workout";
	public static string ProgramDetailStartWorkout => IsTurkish ? "Antrenman Başla" : "Start Workout";
	public static string ProgramDetailLoadError => IsTurkish ? "Program yüklenemedi." : "Failed to load program.";
	public static string ProgramDetailDataError => IsTurkish
		? "Program verisi yüklenemedi. Lütfen geri dönüp tekrar deneyin."
		: "Program data could not be loaded. Please go back and try again.";
	public static string ProgramDetailNoSessions => IsTurkish ? "Bu programda henüz seans bulunmuyor." : "No sessions in this program yet.";

	// ── Session Picker Page ─────────────────────────────────────
	public static string SessionPickerTitle => IsTurkish ? "Seans Seç" : "Pick Session";

	// ── Add Workout From Program Page ───────────────────────────
	public static string AddFromProgramTitle => IsTurkish ? "Antrenman Ekle" : "Add Workout";
	public static string AddFromProgramDate => IsTurkish ? "Tarih:" : "Date:";
	public static string AddFromProgramSave => IsTurkish ? "Kaydet" : "Save";
	public static string AddFromProgramSaving => IsTurkish ? "Kaydediliyor..." : "Saving...";
	public static string AddFromProgramFailed => IsTurkish ? "Antrenman kaydedilemedi." : "Failed to save workout.";
	public static string AddFromProgramSetRequired => IsTurkish ? "Set sayısı gerekli." : "Set count is required.";
	public static string AddFromProgramNeedExercise => IsTurkish ? "En az bir egzersiz gerekli." : "At least one exercise is required.";

	// ── Start Workout Session Page ──────────────────────────────
	public static string LiveWorkoutTitle => IsTurkish ? "Antrenman" : "Workout";
	public static string LiveWorkoutAddExercise => IsTurkish ? "+ Egzersiz Ekle" : "+ Add Exercise";
	public static string LiveWorkoutAddExerciseTitle => IsTurkish ? "Egzersiz Ekle" : "Add Exercise";
	public static string LiveWorkoutRestStart => IsTurkish ? "Dinlenme Başlat" : "Start Rest";
	public static string LiveWorkoutRestEnd => IsTurkish ? "Dinlenme Bitir" : "End Rest";
	public static string LiveWorkoutFinish => IsTurkish ? "Bitir" : "Finish";
	public static string LiveWorkoutNeedExercise => IsTurkish ? "En az bir egzersiz gerekli." : "At least one exercise is required.";
	public static string LiveWorkoutCancelTitle => IsTurkish ? "Antrenmanı İptal Et" : "Cancel Workout";
	public static string LiveWorkoutCancelConfirm => IsTurkish
		? "Devam eden antrenmanı iptal etmek istediğinize emin misiniz?"
		: "Are you sure you want to cancel the current workout?";
	public static string LiveWorkoutYes => IsTurkish ? "Evet" : "Yes";
	public static string LiveWorkoutNo => IsTurkish ? "Hayır" : "No";
	public static string LiveWorkoutSetRequired => IsTurkish ? "Set sayısı gerekli." : "Set count is required.";

	// ── Workout Preview Page ────────────────────────────────────
	public static string PreviewTitle => IsTurkish ? "Antrenman Özeti" : "Workout Summary";
	public static string PreviewGoBack => IsTurkish ? "Geri Dön" : "Go Back";
	public static string PreviewSave => IsTurkish ? "Kaydet" : "Save";
	public static string PreviewSaving => IsTurkish ? "Kaydediliyor..." : "Saving...";
	public static string PreviewFailed => IsTurkish ? "Antrenman kaydedilemedi." : "Failed to save workout.";
	public static string FormatDuration(string formatted) => IsTurkish ? $"Süre: {formatted}" : $"Duration: {formatted}";

	// ── Exercise Comparison Chart ───────────────────────────────
	public static string ChartSubtitle => IsTurkish
		? "Son 7 gün içerisindeki maksimum ağırlıklar ve tekrar sayıları"
		: "Max weights and reps in the last 7 days";
	public static string ChartChange => IsTurkish ? "Değiştir" : "Change";
	public static string ChartNoData => IsTurkish ? "Henüz veri yok" : "No data yet";
	public static string ChartRange14Days => IsTurkish ? "14 Gün" : "14 Days";
	public static string ChartRange1Month => IsTurkish ? "1 Ay" : "1 Month";
	public static string ChartRange3Months => IsTurkish ? "3 Ay" : "3 Months";
	public static string ChartRange6Months => IsTurkish ? "6 Ay" : "6 Months";
	public static string ChartSubtitleDays14 => IsTurkish
		? "Son 14 günün en iyi değerleri"
		: "Best values in the last 14 days";
	public static string ChartSubtitleMonth1 => IsTurkish
		? "Son 1 ayın günlük en iyi değerleri"
		: "Daily best values in the last month";
	public static string ChartSubtitleMonths3 => IsTurkish
		? "Son 3 ayın haftalık en iyi değerleri"
		: "Weekly best values in the last 3 months";
	public static string ChartSubtitleMonths6 => IsTurkish
		? "Son 6 ayın aylık en iyi değerleri"
		: "Monthly best values in the last 6 months";
	public static string[] ChartMonthAbbreviations => IsTurkish
		? ["Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara"]
		: ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

	// ── Confirm Dialog ──────────────────────────────────────────
	public static string ConfirmAction => IsTurkish ? "İŞLEMİ ONAYLA" : "CONFIRM ACTION";

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

	// ── FreakAI Usage Card / Paywall ───────────────────────────
	public static string FreakAiPlanFree => IsTurkish ? "Ücretsiz Plan" : "Free Plan";
	public static string FreakAiPlanPremium => IsTurkish ? "Premium Plan" : "Premium Plan";
	public static string FreakAiPlanLabel => IsTurkish ? "PLAN" : "PLAN";
	public static string FreakAiChatRemaining => IsTurkish ? "Sohbet" : "Chat";
	public static string FreakAiGenerateRemaining => IsTurkish ? "Program Oluştur" : "Generate";
	public static string FreakAiAnalyzeRemaining => IsTurkish ? "Analiz" : "Analyze";
	public static string FreakAiNutritionAvailable => IsTurkish ? "Beslenme" : "Nutrition";
	public static string FreakAiNutritionReady => IsTurkish ? "Hazır" : "Ready";
	public static string FreakAiUnlimited => IsTurkish ? "Sınırsız" : "Unlimited";
	public static string FreakAiUpgradeCta => IsTurkish ? "Premium'a Geç" : "Go Premium";
	public static string FreakAiPremiumActive => IsTurkish ? "Premium aktif" : "Premium active";
	public static string FormatRemainingToday(int n) => IsTurkish ? $"{n}/gün" : $"{n}/day";
	public static string FormatRemainingMonth(int n) => IsTurkish ? $"{n}/ay" : $"{n}/mo";
	public static string FormatNutritionNextAt(DateTime utc)
	{
		var local = utc.ToLocalTime();
		return IsTurkish ? $"{local:dd MMM HH:mm}" : $"{local:MMM dd HH:mm}";
	}

	// ── FreakAI Quota Exhausted ────────────────────────────────
	public static string QuotaExhaustedFree => IsTurkish
		? "Ücretsiz kullanım hakkın doldu. Premium'a geçerek sınırsız erişim elde edebilirsin."
		: "You've reached your free usage limit. Upgrade to Premium for unlimited access.";
	public static string QuotaExhaustedPremium => IsTurkish
		? "Günlük kullanım limitine ulaştın. Yarın tekrar dene."
		: "You've reached the daily usage limit. Try again tomorrow.";
	public static string QuotaUpgradeButton => IsTurkish ? "Premium'a Geç" : "Go Premium";
	public static string FormatQuotaResetsAt(DateTime utc)
	{
		var local = utc.ToLocalTime();
		return IsTurkish
			? $"Limit {local:dd MMM HH:mm}'de sıfırlanır."
			: $"Limit resets at {local:MMM dd HH:mm}.";
	}

	// ── Settings Billing Details ────────────────────────────────
	public static string SettingsPlanFree => IsTurkish ? "Ücretsiz" : "Free";
	public static string SettingsPlanPremium => IsTurkish ? "Premium" : "Premium";
	public static string SettingsChoosePlan => IsTurkish ? "Plan Seçin" : "Choose Plan";
	public static string SettingsPlanMonthly => IsTurkish ? "Aylık" : "Monthly";
	public static string SettingsPlanAnnual => IsTurkish ? "Yıllık" : "Annual";
	public static string SettingsCurrentPlan => IsTurkish ? "Mevcut Plan" : "Current Plan";
	public static string SettingsCurrentPlanDesc => IsTurkish ? "Aktif abonelik durumunuz" : "Your active subscription status";
	public static string FormatRenewalDate(DateTime utc) => IsTurkish
		? $"Yenileme: {utc.ToLocalTime():dd MMM yyyy}"
		: $"Renews: {utc.ToLocalTime():MMM dd, yyyy}";
	public static string FormatExpiryDate(DateTime utc) => IsTurkish
		? $"Bitiş: {utc.ToLocalTime():dd MMM yyyy}"
		: $"Expires: {utc.ToLocalTime():MMM dd, yyyy}";

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

	// ── Program Detail – dynamic labels ─────────────────────────
	public static string ProgramDetailDeload => IsTurkish ? "DİNLENME" : "DELOAD";
	public static string FormatExercises(int count) => IsTurkish ? $"{count} egzersiz" : $"{count} exercises";
	public static string FormatDurationMinutes(int min) => $"{min} min";
	public static string FormatFrequencyPerWeek(int days) => IsTurkish ? $"Haftada {days}x" : $"{days}x/week";

	// ── Validation / error strings ──────────────────────────────
	public static string FormatMustBePositive(string label) => IsTurkish
		? $"{label} pozitif bir sayı olmalıdır."
		: $"{label} must be a positive number.";
	public static string SportCatalogLoadError => IsTurkish
		? "Spor listesi yüklenemedi."
		: "Sport list could not be loaded.";
	public static string SportCatalogRequestFailed => IsTurkish
		? "Spor kataloğu isteği başarısız oldu."
		: "Sport catalog request failed.";

	// ── Option Picker Page ──────────────────────────────────────
	public static string PickerSelect => IsTurkish ? "SEÇ" : "SELECT";
	public static string PickerChooseOption => IsTurkish ? "Seçenek Belirle" : "Choose Option";
	public static string PickerSearch => IsTurkish ? "Ara..." : "Search...";
	public static string PickerLoading => IsTurkish ? "Yükleniyor..." : "Loading...";
	public static string PickerNoOptionsFound => IsTurkish ? "Seçenek bulunamadı" : "No options found";
	public static string PickerTryDifferentSearch => IsTurkish ? "Farklı bir arama terimi deneyin" : "Try a different search term";
	public static string PickerFailedToLoad => IsTurkish ? "Yüklenemedi" : "Failed to load";
	public static string PickerRetry => IsTurkish ? "Tekrar Dene" : "Retry";
	public static string PickerNoResults => IsTurkish ? "Sonuç bulunamadı" : "No results found";
	public static string PickerNoOptions => IsTurkish ? "Mevcut seçenek yok" : "No options available";

	// ── Exercise Picker Page ────────────────────────────────────
	public static string ExPickerBadge => IsTurkish ? "EGZERSİZ TARAYICISI" : "EXERCISE BROWSER";
	public static string ExPickerChoose => IsTurkish ? "Egzersiz Seç" : "Choose Exercise";
	public static string ExPickerDesc => IsTurkish
		? "En çok önerilen 20 hareket varsayılan olarak gösterilir. Tam listeye ulaşmak için kategori içinde arama yapın."
		: "Top 20 recommended movements show by default. Search inside a category to reach the full list.";
	public static string ExPickerSearchPlaceholder => IsTurkish ? "Bu kategoride ara" : "Search inside this category";
	public static string ExPickerView => IsTurkish ? "Gör" : "View";

	// ── Date Selector Page ──────────────────────────────────────
	public static string DateSelectorTitle => IsTurkish ? "Doğum Tarihi" : "Date of Birth";
	public static string DateSelectorBadge => IsTurkish ? "DOĞUM TARİHİ" : "DATE OF BIRTH";
	public static string DateSelectorYear => IsTurkish ? "Yıl" : "Year";
	public static string DateSelectorMonth => IsTurkish ? "Ay" : "Month";
	public static string DateSelectorDay => IsTurkish ? "Gün" : "Day";
	public static string DateSelectorDone => IsTurkish ? "Tamam" : "Done";
	public static string[] MonthAbbreviations => IsTurkish
		? ["Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara"]
		: ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

	// ── Message Dialog Page ─────────────────────────────────────
	public static string DialogSuccess => IsTurkish ? "BAŞARILI" : "SUCCESS";
	public static string DialogContinue => IsTurkish ? "Devam" : "Continue";

	// ── Startup Page ────────────────────────────────────────────
	public static string StartupPreparing => IsTurkish ? "Oturum hazırlanıyor..." : "Preparing your session...";
}
