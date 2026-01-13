# C# Best Practices (Based on Microsoft Official Guidance)

## 🧱 Exception Handling Best Practices
- Limit exceptions to error conditions, not control flow; re-throw exceptions to preserve the original stack trace (use `throw;`).
- Prioritize .NET standard exception types (e.g., `ArgumentException`, `InvalidOperationException`) in public APIs; use custom exceptions only when the scenario is clearly defined.
- Keep `try` blocks minimal; release resources in `finally` and perform state recovery and logging in `catch`.
- Include actionable information in exception messages and consider localization based on locale.

## ⚙️ IDisposable and using Pattern
- Adopt the `Dispose(bool disposing)` pattern in `IDisposable`-implementing classes and call `GC.SuppressFinalize(this)`.
- Utilize `using` declarations (C# 8.0+) to ensure resources are released when scope ends.
- Apply `await using` for async streams and I/O to respect `IAsyncDisposable`.
- Use wrapper types like `SafeHandle` even when explicit disposal of shared resources isn't required to prevent leaks.

```csharp
await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
var buffer = new byte[stream.Length];
_ = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
```

## 🚀 Thread Safety and Async Processing
- Avoid nested `Task.Run`; build natural async flows using async APIs.
- Default to `ConfigureAwait(false)` in library code; maintain `true` as needed in application UI contexts.
- Use thread-safe collections (e.g., `ConcurrentDictionary`, `Channel<T>`) and keep `lock` usage to the necessary minimum.
- Pass `CancellationToken` through public APIs and call `ThrowIfCancellationRequested()` appropriately.

## 🧭 LINQ Optimization and Performance
- Understand that `IEnumerable<T>` operations are lazy; use `ToList()` to materialize when enumerating multiple times.
- In critical paths, reduce iterations by branching within `Select` instead of chaining `Select` and `Where`.
- Maintain consistency between query syntax and method syntax; choose the approach that balances readability and performance.
- When using `AsParallel()` or `ParallelEnumerable`, evaluate thread safety and ordering maintenance costs.

## 🧱 Design Principles: Composition Over Inheritance
- In API design, combine interfaces and single-responsibility classes to facilitate implementation detail substitution.
- Limit inheritance to clear "IS-A" relationships where the base class is stable.
- Use dependency injection for object creation to enhance testability and substitutability.
- Leverage decorator or strategy patterns for behavior reuse.

## ⚙️ .NET Design Guidelines Summary
- Maintain CLS compliance in public APIs to avoid hindering multi-language usage.
- Minimize side effects in methods and document all public member contracts with XML comments.
- Prefer generics for collection types; ensure extensibility by returning result types instead of `void`.
- Follow established .NET patterns for exception, event, property, and operator design (e.g., `EventHandler<TEventArgs>`).

## 🚀 Performance Optimization: Collections and Span<T>
- Use `StringBuilder` for frequent string concatenation; specify capacity upfront when size is known.
- Utilize `Span<T>` / `ReadOnlySpan<T>` for array operations to reduce bounds checks and minimize copies.
- Leverage `ArrayPool<T>` or `MemoryPool<T>` for large-scale data processing to reduce allocations.
- Choose collections based on access patterns (e.g., `List<T>` for fixed order, `Dictionary<TKey,TValue>` for key-centric lookups) and ensure appropriate complexity.

## 🧭 API Design, Naming, and Extensibility
- Avoid breaking changes in public APIs and follow versioning policies.
- Provide method overloads mindful of future extensibility points (async APIs, stream APIs, option types).
- Naming should clearly express use cases; document parameter names, return values, and exception contracts.
- Limit extension methods to specific scenarios; place them in the same namespace as related types to avoid namespace pollution.

## 📎 References
- https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/
- https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/
- https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions/
- https://learn.microsoft.com/en-us/dotnet/standard/collections/performance/

*Generated on 2025-10-29*
