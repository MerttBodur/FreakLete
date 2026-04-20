# Overengineering Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove duplicated helpers, near-identical event handlers, and manual UI construction across the repo — keep behavior identical, cut line count and cognitive load.

**Architecture:** Six surgical refactors targeting *confirmed* duplication: one shared ColorResources helper, one ApiClient.ExecuteAsync wrapper for the 4 fully-identical try/catch endpoints, removal of duplicate in-line mapping that re-implements existing helpers, an OptionPicker push helper that collapses 7 near-identical ProfilePage handlers, a SetEntryTextSilently helper, and swapping BuildHighlights manual Border construction for BindableLayout + DataTemplate. No new abstractions without current duplication behind them.

**Tech Stack:** .NET MAUI (C#, XAML), ASP.NET Core (C#), xUnit

---

## Scope & Non-Goals

**In scope:** Duplications with exact copies across ≥3 files, OR identical catch-block patterns ≥4 instances, OR event handlers that differ only in literal arguments.

**Non-goals (do NOT touch):**
- `Services/AppLanguage.cs` — 865 lines but consistent `isTurkish ? "..." : "..."` pattern; refactoring risks translation regressions for no structural win.
- `Services/FreakAiOrchestrator.cs`, `Services/ToolExecutor.cs` — load-bearing complexity driving multi-turn AI tool use.
- XAML file splitting / namespace reorganization — out of scope.
- ViewModels — not currently duplicated.

---

## File Map

| File | Change |
|------|--------|
| `Services/ColorResources.cs` | Create — single `GetColor(key, fallback)` helper |
| `CodeBehind/WorkoutPage.xaml.cs` | Delete local `GetColor`, call `ColorResources.GetColor` |
| `CodeBehind/ProgramDetailPage.xaml.cs` | Delete local `GetColor`, call `ColorResources.GetColor` |
| `CodeBehind/SessionPickerPage.xaml.cs` | Delete local `GetColor`, call `ColorResources.GetColor` |
| `CodeBehind/WorkoutPreviewPage.xaml.cs` | Delete local `GetColor`, call `ColorResources.GetColor` |
| `CodeBehind/StartWorkoutSessionPage.xaml.cs` | Delete local `GetColor`, call `ColorResources.GetColor` |
| `Helpers/ExerciseInputRowBuilder.cs` | Delete local `GetColor`, call `ColorResources.GetColor` |
| `CodeBehind/ProfilePage.xaml.cs` | Delete `GetProfileColor`; use `ColorResources.GetColor`. Task 3–6 changes below. |
| `Services/ApiClient.cs` | Add private `ExecuteAsync<T>` wrapper, collapse 4 identical endpoints |

---

## Task 1: Extract shared `ColorResources.GetColor` helper

**Files:**
- Create: `Services/ColorResources.cs`
- Modify: `CodeBehind/WorkoutPage.xaml.cs`, `CodeBehind/ProgramDetailPage.xaml.cs`, `CodeBehind/SessionPickerPage.xaml.cs`, `CodeBehind/WorkoutPreviewPage.xaml.cs`, `CodeBehind/StartWorkoutSessionPage.xaml.cs`, `Helpers/ExerciseInputRowBuilder.cs`, `CodeBehind/ProfilePage.xaml.cs`

Seven files contain byte-identical copies of a `GetColor(string key, string fallback)` helper (ProfilePage names it `GetProfileColor`). Extract once, delete six copies.

- [ ] **Step 1: Create `Services/ColorResources.cs`**

```csharp
namespace FreakLete.Services;

public static class ColorResources
{
    public static Color GetColor(string key, string fallback)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color)
            return color;
        return Color.FromArgb(fallback);
    }
}
```

- [ ] **Step 2: Delete local `GetColor` from `CodeBehind/WorkoutPage.xaml.cs`**

Remove the entire `private static Color GetColor(...)` method (around line 408). Add `using FreakLete.Services;` if missing, then replace every `GetColor(` call site in the file with `ColorResources.GetColor(`.

- [ ] **Step 3: Repeat Step 2 for each remaining file**

Apply the same delete + call-site rewrite to:
- `CodeBehind/ProgramDetailPage.xaml.cs` (helper ~line 298)
- `CodeBehind/SessionPickerPage.xaml.cs` (helper ~line 151)
- `CodeBehind/WorkoutPreviewPage.xaml.cs` (helper ~line 173)
- `CodeBehind/StartWorkoutSessionPage.xaml.cs` (helper ~line 258)
- `Helpers/ExerciseInputRowBuilder.cs` (helper ~line 453)

For `CodeBehind/ProfilePage.xaml.cs` only: the method is named `GetProfileColor` (around line 429). Delete it, then find/replace `GetProfileColor(` → `ColorResources.GetColor(` in that file.

- [ ] **Step 4: Build**

```bash
dotnet build FreakLete.csproj -f net10.0-android
```

Expected: 0 errors.

- [ ] **Step 5: Commit**

```bash
git add Services/ColorResources.cs CodeBehind/WorkoutPage.xaml.cs CodeBehind/ProgramDetailPage.xaml.cs CodeBehind/SessionPickerPage.xaml.cs CodeBehind/WorkoutPreviewPage.xaml.cs CodeBehind/StartWorkoutSessionPage.xaml.cs CodeBehind/StartWorkoutSessionPage.xaml.cs Helpers/ExerciseInputRowBuilder.cs CodeBehind/ProfilePage.xaml.cs
git commit -m "refactor: extract ColorResources.GetColor, remove 7 duplicate helpers"
```

---

## Task 2: Extract `ApiClient.ExecuteAsync<T>` wrapper

**Files:**
- Modify: `Services/ApiClient.cs`

Nine methods in `ApiClient` repeat an identical try/catch with `catch (Exception ex) { return new ApiResult<T> { Success = false, ErrorMessage = $"Bağlantı hatası: {ex.Message}" }; }`. Four of them (`GetAsync`, `PostAsync`, `PutWithResponseAsync`, `UploadProfilePhotoAsync`) differ only in the `HttpResponseMessage` producer — collapse those four.

The other five (`PutAsync`, `DeleteAsync`, `ChangePasswordAsync`, `GetProfilePhotoAsync`, `DeleteAccountAsync`) each have unique branches (distinct return type, 404 fallback, etc.) — **leave them alone**.

- [ ] **Step 1: Add private helper inside `ApiClient`**

In `Services/ApiClient.cs`, add this private method near the existing helpers (just above `GetAsync`):

```csharp
private async Task<ApiResult<T>> ExecuteAsync<T>(Func<Task<HttpResponseMessage>> send)
{
    try
    {
        using var response = await send();
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return new ApiResult<T> { Success = false, ErrorMessage = content };

        var data = JsonSerializer.Deserialize<T>(content, _jsonOptions);
        return new ApiResult<T> { Success = true, Data = data };
    }
    catch (Exception ex)
    {
        return new ApiResult<T> { Success = false, ErrorMessage = $"Bağlantı hatası: {ex.Message}" };
    }
}
```

Note: confirm the JSON options field name matches the existing one in the file (`_jsonOptions` vs `_options`) before writing.

- [ ] **Step 2: Collapse `GetAsync<T>`**

Replace the body of `GetAsync<T>(string endpoint)` (around lines 354-366) with:

```csharp
public Task<ApiResult<T>> GetAsync<T>(string endpoint) =>
    ExecuteAsync<T>(() => _httpClient.GetAsync(endpoint));
```

- [ ] **Step 3: Collapse `PostAsync<T>`**

Replace the body of `PostAsync<T>(string endpoint, object payload)` (around lines 368-380):

```csharp
public Task<ApiResult<T>> PostAsync<T>(string endpoint, object payload)
{
    var json = JsonSerializer.Serialize(payload, _jsonOptions);
    var body = new StringContent(json, Encoding.UTF8, "application/json");
    return ExecuteAsync<T>(() => _httpClient.PostAsync(endpoint, body));
}
```

- [ ] **Step 4: Collapse `PutWithResponseAsync<T>`**

Replace the body of `PutWithResponseAsync<T>(string endpoint, object payload)` (around lines 382-394):

```csharp
public Task<ApiResult<T>> PutWithResponseAsync<T>(string endpoint, object payload)
{
    var json = JsonSerializer.Serialize(payload, _jsonOptions);
    var body = new StringContent(json, Encoding.UTF8, "application/json");
    return ExecuteAsync<T>(() => _httpClient.PutAsync(endpoint, body));
}
```

- [ ] **Step 5: Collapse `UploadProfilePhotoAsync`**

Replace the body of `UploadProfilePhotoAsync(...)` (around lines 131-148) so the multipart content is prepared outside, and the send is passed to `ExecuteAsync`. Example:

```csharp
public Task<ApiResult<ProfilePhotoResponse>> UploadProfilePhotoAsync(Stream stream, string fileName, string contentType)
{
    var content = new MultipartFormDataContent();
    var fileContent = new StreamContent(stream);
    fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
    content.Add(fileContent, "file", fileName);
    return ExecuteAsync<ProfilePhotoResponse>(() => _httpClient.PostAsync("api/profile/photo", content));
}
```

(Confirm the exact return type — if the existing signature returns a different DTO, keep that DTO.)

- [ ] **Step 6: Build & run existing API tests**

```bash
dotnet build FreakLete.Api
dotnet test FreakLete.Api.Tests
```

Expected: 0 build errors; existing tests unchanged.

- [ ] **Step 7: Commit**

```bash
git add Services/ApiClient.cs
git commit -m "refactor: extract ApiClient.ExecuteAsync wrapper, collapse 4 duplicate endpoints"
```

---

## Task 3: Remove duplicate athletic/goal mapping in `LoadProfileAsync`

**Files:**
- Modify: `CodeBehind/ProfilePage.xaml.cs` (lines ~254-316)

`LoadProfileAsync` currently contains ~60 lines of inline athletic/goal → chip mapping that is **already implemented** by `LoadAthleticPerformancesAsync` (line ~738) and `LoadMovementGoalsAsync` (line ~770). Delete the inline version; call the helpers.

- [ ] **Step 1: Read current `LoadProfileAsync` block**

Read `CodeBehind/ProfilePage.xaml.cs` lines 240-330 to confirm the duplicated mapping block is still present.

- [ ] **Step 2: Replace inline mapping with helper calls**

Delete the duplicated inline code (approx lines 254-316 — the block that populates chips from `_vm.AthleticPerformances`/`_vm.MovementGoals` manually) and replace with:

```csharp
await LoadAthleticPerformancesAsync();
await LoadMovementGoalsAsync();
```

Keep every other line of `LoadProfileAsync` unchanged.

- [ ] **Step 3: Build & smoke test**

```bash
dotnet build FreakLete.csproj -f net10.0-android
```

Manual: open the app, navigate Profile → verify athletic performance and movement goal chips still render.

- [ ] **Step 4: Commit**

```bash
git add CodeBehind/ProfilePage.xaml.cs
git commit -m "refactor: reuse LoadAthleticPerformancesAsync/LoadMovementGoalsAsync, drop inline duplicate"
```

---

## Task 4: Extract `PushOptionPickerAsync` helper for 7 selector handlers

**Files:**
- Modify: `CodeBehind/ProfilePage.xaml.cs`

Seven `OnXxxTapped` handlers between lines ~592-734 differ only in: (a) the picker title, (b) the options list, (c) the current value, (d) which `_vm` property and selector label to update. Collapse into one helper.

- [ ] **Step 1: Add the helper method**

Add this method to `ProfilePage` (near the bottom, before the closing `}` of the class):

```csharp
private async Task PushOptionPickerAsync(
    string title,
    IList<string> options,
    string current,
    Label selectorLabel,
    string placeholder,
    Action<string> assignToVm)
{
    await Navigation.PushAsync(
        new OptionPickerPage(title, options, current, async val =>
        {
            assignToVm(val);
            SetSelectorValue(selectorLabel, val, placeholder);
            var success = await SaveFieldAsync();
            if (success) _skipNextProfileReload = true;
        }), true);
}
```

- [ ] **Step 2: Rewrite each of the 7 handlers**

For each handler, replace the body with a single `PushOptionPickerAsync` call. Example for `OnGymExperienceTapped`:

```csharp
private async void OnGymExperienceTapped(object? sender, TappedEventArgs e)
{
    if (_vm is null) return;
    await PushOptionPickerAsync(
        title: AppLanguage.IsTurkish ? "Deneyim Seviyesi" : "Experience Level",
        options: GymExperienceOptions,
        current: _vm.GymExperience ?? "",
        selectorLabel: GymExperienceValueLabel,
        placeholder: AppLanguage.IsTurkish ? "Seç" : "Select",
        assignToVm: v => _vm.GymExperience = v);
}
```

Repeat for: `OnSexSelectorTapped`, `OnTrainingDaysTapped`, `OnSessionDurationTapped`, `OnPrimaryGoalTapped`, `OnSecondaryGoalTapped`, `OnDietaryPreferenceTapped`, `OnEquipmentSelectorTapped`. For each, copy the exact title/options/current/label/placeholder/assignment that is already in the current code — **do not change any strings or property names**.

- [ ] **Step 3: Build**

```bash
dotnet build FreakLete.csproj -f net10.0-android
```

- [ ] **Step 4: Manual smoke test**

Run app → Profile → tap each of the 8 selectors (gym experience, sex, training days, session duration, primary goal, secondary goal, dietary pref, equipment) → change value → verify it persists on reload.

- [ ] **Step 5: Commit**

```bash
git add CodeBehind/ProfilePage.xaml.cs
git commit -m "refactor: extract PushOptionPickerAsync, collapse 7 ProfilePage selector handlers"
```

---

## Task 5: Extract `SetEntryTextSilently` helper

**Files:**
- Modify: `CodeBehind/ProfilePage.xaml.cs` (lines ~227-235 and ~853-861)

Two places in ProfilePage unwire `TextChanged`, set `Entry.Text`, rewire `TextChanged`. Identical 4-line block duplicated.

- [ ] **Step 1: Add helper**

Add near other private helpers in `ProfilePage`:

```csharp
private void SetEntryTextSilently(Entry entry, string? value, EventHandler<TextChangedEventArgs> handler)
{
    entry.TextChanged -= handler;
    entry.Text = value ?? "";
    entry.TextChanged += handler;
}
```

- [ ] **Step 2: Replace both sites**

At each of the two duplicated blocks (lines ~227-235 and ~853-861), replace the unwire/set/rewire triplet for each Entry with a single `SetEntryTextSilently(entryRef, valueRef, handlerRef);` call. Keep the exact Entry/value/handler references already in the code.

- [ ] **Step 3: Build & commit**

```bash
dotnet build FreakLete.csproj -f net10.0-android
git add CodeBehind/ProfilePage.xaml.cs
git commit -m "refactor: extract SetEntryTextSilently helper, drop duplicated wire/unwire block"
```

---

## Task 6: Replace `BuildHighlights` manual UI with BindableLayout + DataTemplate

**Files:**
- Modify: `Xaml/ProfilePage.xaml`, `CodeBehind/ProfilePage.xaml.cs` (lines ~323-419)

`BuildHighlights` constructs ~60 lines of `Border` / `Label` / `Grid` in C#. A `BindableLayout` with a `DataTemplate` gives the same output declaratively.

- [ ] **Step 1: Define a `ProfileHighlight` record**

Add to `CodeBehind/ProfilePage.xaml.cs` (top of the file, after usings):

```csharp
public record ProfileHighlight(string Title, string Value, string IconGlyph, Color AccentColor);
```

- [ ] **Step 2: Expose `Highlights` collection**

In `ProfilePage` (code-behind), add a public `ObservableCollection<ProfileHighlight> Highlights { get; } = new();`. In the existing `BuildHighlights` method, instead of constructing Borders, clear `Highlights` and add one `ProfileHighlight` per row using the same title/value/icon/color that the current manual code uses. Remove the Border/Label construction after the collection is populated.

- [ ] **Step 3: Add BindableLayout to `Xaml/ProfilePage.xaml`**

Find the existing container that used to host the manually-built Borders (the `HighlightsContainer` or similar named StackLayout/VerticalStackLayout). Replace its children with:

```xml
<VerticalStackLayout x:Name="HighlightsContainer"
                     BindableLayout.ItemsSource="{Binding Highlights, Source={x:Reference ProfilePageRoot}}"
                     Spacing="8">
    <BindableLayout.ItemTemplate>
        <DataTemplate>
            <Border StrokeShape="RoundRectangle 12"
                    BackgroundColor="{DynamicResource SurfaceRaised}"
                    Padding="12">
                <Grid ColumnDefinitions="Auto,*,Auto" ColumnSpacing="12">
                    <Label Grid.Column="0" Text="{Binding IconGlyph}" TextColor="{Binding AccentColor}" FontSize="20"/>
                    <Label Grid.Column="1" Text="{Binding Title}" TextColor="{DynamicResource TextMuted}" FontSize="12"/>
                    <Label Grid.Column="2" Text="{Binding Value}" TextColor="{DynamicResource Text}" FontSize="14"/>
                </Grid>
            </Border>
        </DataTemplate>
    </BindableLayout.ItemTemplate>
</VerticalStackLayout>
```

Confirm: (a) the `x:Name` / binding source name matches the existing page root name, (b) the resource keys used in DynamicResource lookups (`SurfaceRaised`, `TextMuted`, `Text`) match the keys actually defined in `Resources/Styles/Colors.xaml`. If any key differs, use the existing one verbatim.

- [ ] **Step 4: Delete the old imperative block**

Remove the ~60 lines of `new Border { ... }` / `new Label { ... }` / `container.Add(...)` in `BuildHighlights` that built the Borders manually. Keep only the `Highlights.Clear(); Highlights.Add(new ProfileHighlight(...));` calls.

- [ ] **Step 5: Build**

```bash
dotnet build FreakLete.csproj -f net10.0-android
```

Expected: 0 errors.

- [ ] **Step 6: Manual smoke test**

Run app → Profile → verify highlight rows render with same titles, values, icons, colors as before.

- [ ] **Step 7: Commit**

```bash
git add Xaml/ProfilePage.xaml CodeBehind/ProfilePage.xaml.cs
git commit -m "refactor: BindableLayout DataTemplate for profile highlights, drop manual Border construction"
```

---

## Self-Review

**Spec coverage:**
- [x] Repo-wide overengineering detection → 6 tasks each target a confirmed duplication (color helper x7, catch block x4, inline mapping x1, selector handlers x7, wire/unwire x2, manual UI x1).
- [x] Each refactor preserves behavior (Task 3 calls existing helpers that produce the same chips; Task 4/5 keep every title/option/handler reference byte-identical; Task 6 renders the same data via DataTemplate).
- [x] Non-goals explicitly listed (AppLanguage, FreakAi*) so executor doesn't widen scope.

**Placeholder scan:** No "TBD" / "add error handling" / "similar to Task N". Every step shows code or exact commands.

**Type consistency:** `ColorResources.GetColor` signature in Task 1 matches call-site usage in Tasks 1 and 3. `PushOptionPickerAsync` and `SetEntryTextSilently` signatures match their call-site rewrites. `ProfileHighlight` record fields (`Title`, `Value`, `IconGlyph`, `AccentColor`) match the `DataTemplate` binding names in Task 6.

**Risk:** Task 6 is the highest-risk single task (touches XAML and binding context). If time-boxed, ship Tasks 1-5 first (pure C# refactors), land Task 6 as a follow-up.
