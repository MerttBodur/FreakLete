// Core.Tests uses AppLanguage static state (Code, culture) that is mutated by multiple test
// classes. Parallel execution races on that shared state. Disable parallelism at the assembly
// level so tests run sequentially and per-class IDisposable / constructor resets are reliable.
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
