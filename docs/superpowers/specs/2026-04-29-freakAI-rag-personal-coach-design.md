# FreakAI RAG Personal Coach — Design Spec

**Date:** 2026-04-29  
**Status:** Approved

---

## 1. Problem

FreakAI currently relies on Gemini tool calls to fetch user context (profile, goals, equipment, training history). This creates two problems:

1. **Latency:** 2–4 round trips before Gemini can generate a useful response.
2. **Incompleteness:** Gemini sometimes skips tool calls and responds without full context.

Result: sport-specific coaching (e.g. separating gym sessions from sprint/field sessions for an athlete) requires Gemini to correctly decide to call multiple tools and synthesize them. This is unreliable.

---

## 2. Solution

A RAG layer that proactively enriches every Gemini request with relevant user context before the first call.

**Flow:**
```
Request
  → IntentClassifier (existing, unchanged)
  → ContextBuilder.BuildAsync(userId, intent, message)
      → structured user context (always)
      → vector similarity results (when embeddings exist)
  → FreakAiSystemPrompt.Build(context) → enriched system prompt
  → Gemini tool-call loop (unchanged)
  → Response
```

Gemini still retains all tools for dynamic queries (create_program, adjust_program, get_recent_workouts, calculate_one_rm, etc.). Basic profile tools become secondary — the data is already injected.

---

## 3. Architecture Overview

### New Components

| Component | Responsibility |
|-----------|---------------|
| `ContextBuilder` | Fetches and assembles user context per intent |
| `FreakAiContext` | DTO carrying structured context + vector results |
| `WorkoutEmbedding` (entity) | pgvector row per workout session |
| `UserSnapshotEmbedding` (entity) | pgvector row per user (latest snapshot) |
| `EmbeddingBackgroundService` | Async worker — generates embeddings off the hot path |
| `IUserSnapshotEventSink` | Interface for triggering snapshot re-embedding from domain events |
| `EmbeddingTextFormatter` | Formats workout/user data into embedding-ready text |

### Modified Components

| Component | Change |
|-----------|--------|
| `FreakAiOrchestrator` | Calls ContextBuilder before Gemini; injects enriched prompt |
| `FreakAiSystemPrompt` | New `Build(FreakAiContext?)` method; static prompt is fallback |
| `WorkoutController` | Fires `IUserSnapshotEventSink` after workout save |
| `PrController` | Fires `IUserSnapshotEventSink` after PR entry |
| `ProfileController` | Fires `IUserSnapshotEventSink` after profile update |
| `ProgramController` | Fires `IUserSnapshotEventSink` after program create/complete |

### Unchanged

- `IntentClassifier` — same classification logic
- `FreakAiToolExecutor` — all tools retained, no removals
- `IFreakAiOrchestrator` — external contract unchanged

---

## 4. Data Layer

### pgvector Extension

Enable on Railway PostgreSQL:
```sql
CREATE EXTENSION IF NOT EXISTS vector;
```

### WorkoutEmbedding Entity

```csharp
public class WorkoutEmbedding
{
    public int Id { get; set; }
    public int WorkoutId { get; set; }
    public int UserId { get; set; }
    public float[] Embedding { get; set; } = [];  // 768-dim, text-embedding-004
    public string TextSnapshot { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

**Text snapshot format:**
```
Sport: Football | Position: Wide Receiver | Focus: Strength
Exercises: Squat 4x5 @120kg, Bench Press 3x8 @80kg, RDL 3x10 @90kg
Notes: Felt strong on squat. First session after deload.
Date: 2026-04-20
```

### UserSnapshotEmbedding Entity

```csharp
public class UserSnapshotEmbedding
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public float[] Embedding { get; set; } = [];
    public string TextSnapshot { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
```

**Text snapshot format:**
```
Sport: Football | Position: Wide Receiver | Experience: Intermediate
Primary Goal: Athletic Performance | Secondary Goal: Muscle Gain
Training Days: 4/week | Session Duration: 75 min
Equipment: Full gym, resistance bands
Physical Limitations: None | Injury History: Left knee sprain (2024)
Top PRs: Squat 140kg, Bench 100kg, Deadlift 160kg
Active Program: 4-Day Athletic Hypertrophy (Week 3 of 8)
```

### EF Core Configuration

Register `vector` column type via `HasColumnType("vector(768)")`. Add migration after entities are defined.

---

## 5. ContextBuilder

`ContextBuilder` is the core RAG service. It decides what to fetch based on intent and assembles `FreakAiContext`.

```csharp
public class FreakAiContext
{
    public string? UserProfile { get; init; }       // sport, position, experience, body stats
    public string? Goals { get; init; }             // primary + secondary goals
    public string? Equipment { get; init; }
    public string? PhysicalLimitations { get; init; }
    public string? CurrentProgram { get; init; }
    public string? RecentPrSummary { get; init; }
    public List<string> SimilarWorkouts { get; init; } = [];   // top-3 vector results
    public string? UserSnapshotContext { get; init; }          // snapshot vector result
}
```

### Intent → Context Mapping

| Intent | Fetched |
|--------|---------|
| `program_generate` | Full profile, goals, equipment, limitations, current program, recent PRs, snapshot |
| `program_analyze` | Full profile, goals, current program, recent PRs, similar workouts |
| `nutrition_guidance` | Profile (weight, body fat, goals), dietary preference |
| `general_chat` | Minimal profile (sport, position, goals only) |

### Sport-Specific Intelligence

No static `SportCoachingHints` class. Clean user context is injected (sport, position, goals, experience level) and Gemini applies its own pre-trained sport knowledge. This avoids:
- Manual maintenance of sport-specific rules
- Incomplete coverage
- Duplication of Gemini's existing knowledge

---

## 6. Embedding Generation Pipeline

### WorkoutEmbedding — per workout, async

Triggered after each workout save via `EmbeddingBackgroundService`:
1. `WorkoutController` enqueues job
2. `EmbeddingTextFormatter.FormatWorkout(workout)` builds text snapshot
3. `GeminiClient.EmbedAsync(text)` → 768-dim float array
4. Upsert `WorkoutEmbedding` row

### UserSnapshotEmbedding — per user, event-driven

`IUserSnapshotEventSink` fires on:
- Workout saved
- PR entry recorded
- Profile updated
- Program created or completed

On each event:
1. Re-fetch full user context
2. `EmbeddingTextFormatter.FormatUserSnapshot(user, prs, activeProgram)` builds text
3. `GeminiClient.EmbedAsync(text)` → 768-dim float array
4. Upsert single `UserSnapshotEmbedding` row (one row per user)

### Failure Handling

If embedding API fails → silent fail → no retry on same request. Next domain event will re-trigger. FreakAI continues with structured injection only.

---

## 7. FreakAiOrchestrator Changes

### Before

```
Request → Gemini (tool calls loop) → Response
```

### After

```
Request
  → ContextBuilder.BuildAsync(userId, intent, message)
  → FreakAiSystemPrompt.Build(context)
  → Gemini (tool calls loop)
  → Response
```

**Code change in `ChatAsync`:**
```csharp
var context = await _contextBuilder.BuildAsync(userId, intent, userMessage);
var enrichedSystemPrompt = FreakAiSystemPrompt.Build(context);
```

`FreakAiSystemPrompt.Build(FreakAiContext? context)`:
- `context == null` → returns existing static system prompt (unchanged fallback)
- `context != null` → prepends structured context block to static prompt

`IFreakAiOrchestrator` signature is **unchanged**. External callers unaffected.

---

## 8. Error Handling and Fallback Chain

| Layer | Failure | Fallback |
|-------|---------|----------|
| pgvector query | DB connection error | Structured injection only; no vector results |
| Gemini embedding API | Rate limit / timeout | Skip embedding generation; retry on next domain event |
| ContextBuilder (full exception) | Any unhandled error | Context injection skipped; static system prompt used |
| Tool calls | Existing behavior | Unchanged |

No error surfaces to the user. All failures logged server-side.

---

## 9. Testing

### Unit Tests

- `ContextBuilder` — mock repositories; assert correct context blocks per intent
- `IntentClassifier` — existing tests + new intent-context mapping coverage
- `EmbeddingTextFormatter` — deterministic text format for workout and snapshot

### Integration Tests

- `WorkoutController` save → `EmbeddingBackgroundService` triggered → `WorkoutEmbedding` row created
- `IUserSnapshotEventSink.OnUserUpdated` → `UserSnapshotEmbedding` updated
- `FreakAiOrchestrator.ChatAsync` end-to-end → context block present in system prompt

### Regression

Existing `FreakAI` tests pass without modification — orchestrator external contract unchanged.

---

## 10. Out of Scope

- Cross-user similarity (no shared embeddings between users)
- Nutrition macro calculation (existing tool retained, not replaced by RAG)
- Mobile-side changes (all changes are backend-only)
- Multi-turn conversation memory (separate future feature)
