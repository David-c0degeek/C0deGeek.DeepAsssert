using System.Runtime.ExceptionServices;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace C0deGeek.DeepAssert;

/// <summary>
/// Provides a scope in which multiple assertions can be collected and reported together.
/// </summary>
public class AssertionScope : IDisposable
{
    private static readonly AsyncLocal<Stack<AssertionScope>> CurrentScopes = new();
    private readonly List<(string Message, ExceptionDispatchInfo ExceptionInfo)> _failures = new();
    private bool _disposed;

    /// <summary>
    /// Gets the current assertion scope, or null if no scope is active.
    /// </summary>
    public static AssertionScope? Current => CurrentScopes.Value?.Count > 0 
        ? CurrentScopes.Value.Peek() 
        : null;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssertionScope"/> class.
    /// </summary>
    public AssertionScope()
    {
        CurrentScopes.Value ??= new Stack<AssertionScope>();

        CurrentScopes.Value.Push(this);
    }

    /// <summary>
    /// Records a test assertion failure within the scope.
    /// </summary>
    /// <param name="exception">The assertion exception that occurred.</param>
    internal void RecordFailure(AssertFailedException exception)
    {
        if (!_disposed)
        {
            // Avoid adding duplicate failures
            if (_failures.Any(f => f.Message == exception.Message))
            {
                return;
            }

            _failures.Add((exception.Message, ExceptionDispatchInfo.Capture(exception)));
        }
        else
        {
            throw new ObjectDisposedException(nameof(AssertionScope));
        }
    }
        
    /// <summary>
    /// Records a test assertion failure within the scope using a message.
    /// </summary>
    /// <param name="message">The failure message.</param>
    internal void RecordFailure(string message)
    {
        var exception = new AssertFailedException(message);
        RecordFailure(exception);
    }

    /// <summary>
    /// Ends the assertion scope and throws an exception if any assertions failed.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (CurrentScopes.Value?.Count > 0)
        {
            CurrentScopes.Value.Pop();
        }

        _disposed = true;

        switch (_failures.Count)
        {
            case 0:
                return;
            case 1:
                // If there's only one failure, just rethrow it to preserve the stack trace
                _failures[0].ExceptionInfo.Throw();
                break;
        }

        // Multiple failures - build a consolidated message
        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine($"{_failures.Count} assertion(s) failed:");
            
        for (var i = 0; i < _failures.Count; i++)
        {
            var message = _failures[i].Message.TrimEnd();
            messageBuilder.AppendLine($"{i + 1}) {message}");
        }
            
        throw new AssertFailedException(messageBuilder.ToString());
    }
}

/// <summary>
/// Helper class for executing assertions within an AssertionScope.
/// </summary>
internal static class AssertInternal
{
    /// <summary>
    /// Executes an assertion and handles assertion failures within the current assertion scope.
    /// </summary>
    public static void Execute(Action assertion)
    {
        if (AssertionScope.Current == null)
        {
            // No scope, just execute directly
            assertion();
            return;
        }
            
        try
        {
            assertion();
        }
        catch (AssertFailedException ex)
        {
            AssertionScope.Current.RecordFailure(ex);
        }
    }

    /// <summary>
    /// Fails an assertion with the specified message.
    /// </summary>
    public static void Fail(string message)
    {
        var scope = AssertionScope.Current;
        if (scope != null)
        {
            // When in a scope, record the failure without throwing
            scope.RecordFailure(message);
        }
        else
        {
            // When not in a scope, fail immediately
            Assert.Fail(message);
        }
    }
}