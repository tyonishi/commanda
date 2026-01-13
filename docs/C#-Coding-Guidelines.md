# C# Coding Guidelines (Microsoft Official Conventions)

## 🧱 Purpose and Scope
- This document provides guidelines for C# developers and .NET library designers to align with Microsoft official conventions.
- Can be used as unified review standards across Visual Studio, VS Code, and codex CLI.

## ⚙️ Naming Conventions
- Use `PascalCase` for public members, types, and namespaces.
- `camelCase` is the default for local variables and non-private members.
- Prefix private fields with `_camelCase` (recommended); `static readonly` fields are always `PascalCase` without exception.
- Constants (`const`) are `PascalCase`; enumeration members are also `PascalCase`.
- Maintain the same naming conventions as public APIs even for non-public interfaces and abstract classes (e.g., prefix with `I`).

## 🧱 Access Modifiers and Declaration Order
- Arrange modifiers in order: `public` → `protected` → `internal` → `private` so readers immediately understand scope.
- Place class members in order: fields → constructors → properties → events → methods; group related members together.
- Maintain the same order in each file for `partial` classes to ensure API surface consistency.

## ⚙️ Code Formatting
- Use 4 spaces for indentation; do not use tabs.
- Always place braces `{}` on new lines; never omit them even for empty statements.
- Aim for 120 characters or less per line; break long expressions appropriately for readability.
- Follow Visual Studio `.editorconfig` defaults for attributes, lambda expressions, full namespaces, etc.

## 📚 Commenting Policy
- Provide XML documentation comments for public members; include `<summary>`, `<param>`, and `<returns>` or `<remarks>` as needed.
- XML comments serve documentation generation and IntelliSense support; document user-perspective information, not implementation details.
- Keep inline comments minimal; briefly state reasons and intent for complex logic.
- Manage `TODO` comments with trackable work item numbers.

## 🧪 var Usage Guidelines
- Use `var` only when type is clearly distinguishable from initialization expression (e.g., `var stream = new FileStream(...)`).
- Explicitly specify type when `new()` omission syntax makes type ambiguous.
- Actively use `var` for loop variables and anonymous types to avoid redundant type names.

## 🚀 Async Method Naming Conventions
- Suffix `async` methods with `Async` to clearly indicate asynchronous nature (e.g., `LoadDataAsync`).
- Event handlers and other methods with established signatures do not get `Async`; limit `async void` usage to events.
- Follow team standards for `ConfigureAwait(false)` usage; always consider in library code.

## ⚙️ Sample Code

Good example:
```csharp
public sealed class ConfigurationService
{
    private readonly IConfigurationProvider _provider;

    public ConfigurationService(IConfigurationProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var json = await _provider.GetConfigurationAsync(cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<AppSettings>(json)
               ?? throw new InvalidOperationException("Failed to load settings.");
    }
}
```

Bad example:
```csharp
public class configservice {
    IConfigurationProvider Provider;
    public async void Load() {
        var result = await Provider.GetConfigurationAsync();
        if(result == null) throw new Exception("Error");
    }
}
```

## 📎 References
- https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/
- https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/
- https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions/
- https://learn.microsoft.com/en-us/dotnet/standard/collections/performance/

*Generated on 2025-10-29*
