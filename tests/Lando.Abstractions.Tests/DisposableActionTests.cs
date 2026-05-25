using System;

namespace Lando.Tests;

public class DisposableActionTests
{
    [Fact]
    public void Dispose_InvokesTheSuppliedAction()
    {
        var called = false;
        var sut = new DisposableAction(() => called = true);

        sut.Dispose();

        called.ShouldBeTrue();
    }

    [Fact]
    public void Dispose_CalledTwice_InvokesActionExactlyOnce()
    {
        var count = 0;
        var sut = new DisposableAction(() => count++);

        sut.Dispose();
        sut.Dispose();

        count.ShouldBe(1);
    }

    [Fact]
    public void Dispose_WorksInsideUsingStatement()
    {
        var called = false;

        using (new DisposableAction(() => called = true))
        { }

        called.ShouldBeTrue();
    }

    [Fact]
    public void Dispose_ActionThrows_ExceptionPropagates()
    {
        var sut = new DisposableAction(() => throw new InvalidOperationException("boom"));

        Should.Throw<InvalidOperationException>(() => sut.Dispose())
            .Message.ShouldBe("boom");
    }

    [Fact]
    public void Dispose_AfterActionThrew_InvokesActionAgain()
    {
        // disposed is set to true only after action() returns successfully.
        // If the action throws, disposed stays false, so a subsequent Dispose
        // re-runs the action (and re-throws).
        var count = 0;
        var sut = new DisposableAction(() =>
        {
            count++;
            throw new InvalidOperationException();
        });

        Should.Throw<InvalidOperationException>(() => sut.Dispose());
        Should.Throw<InvalidOperationException>(() => sut.Dispose());

        count.ShouldBe(2);
    }
}
