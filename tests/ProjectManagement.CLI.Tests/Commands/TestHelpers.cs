using Spectre.Console.Cli;

namespace ProjectManagement.CLI.Tests.Commands;

// Helper class for CommandContext instantiation in tests
internal class TestRemainingArguments : IRemainingArguments
{
    public IReadOnlyList<string> Remaining { get; } = Array.Empty<string>();
    public ILookup<string, string?> Parsed { get; } = new Lookup<string, string?>();
    public IReadOnlyList<string> Raw { get; } = Array.Empty<string>();

    public TestRemainingArguments(string[] remaining)
    {
        Remaining = remaining;
        Raw = remaining;
    }
}

// Helper class for empty ILookup implementation
internal class Lookup<TKey, TElement> : ILookup<TKey, TElement>
{
    public int Count => 0;
    public bool Contains(TKey key) => false;
    public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator() => Enumerable.Empty<IGrouping<TKey, TElement>>().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerable<TElement> this[TKey key] => Enumerable.Empty<TElement>();
}

// Helper factory for creating CommandContext in tests
internal static class TestCommandContextFactory
{
    public static CommandContext Create(string commandName, string[] remainingArguments = null)
    {
        return new CommandContext(
            Array.Empty<string>(),
            new TestRemainingArguments(remainingArguments ?? Array.Empty<string>()),
            commandName,
            null);
    }
}
