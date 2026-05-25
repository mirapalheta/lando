using System;
using System.Threading;

namespace Lando;

/// <summary>
/// <see cref="IDisposable"/> that runs a delegate on first
/// <see cref="Dispose"/>. Used to bracket scope-bound state changes (ambient
/// flags, <see cref="AsyncLocal{T}"/> values, redaction
/// modes) using a <c>using</c> statement, without each call site having to
/// write a try/finally.
/// </summary>
/// <remarks>
/// Calling <see cref="Dispose"/> a second time is a no-op; the delegate runs
/// at most once. This intentionally does not implement the full
/// <c>Dispose(bool)</c> pattern — there are no unmanaged resources to clean
/// up, and a finalizer would only obscure cases where a caller forgot to
/// dispose.
/// </remarks>
/// <param name="action">The delegate to invoke on first <see cref="Dispose"/>.</param>
public class DisposableAction(Action action) : IDisposable
{
    private bool disposed;

    /// <summary>
    /// Invokes the supplied action exactly once. Subsequent calls are no-ops.
    /// </summary>
    public void Dispose()
    {
        if (!disposed)
        {
            action();
            disposed = true;
        }
    }
}
