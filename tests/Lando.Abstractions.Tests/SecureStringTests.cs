using System;
using System.Text.Json;

namespace Lando.Tests;

public class SecureStringTests
{
    // ── Construction & value access ──────────────────────────────────────────

    [Fact]
    public void Value_ReturnsUnderlyingString()
    {
        var s = new SecureString("secret");

        s.Value.ShouldBe("secret");
    }

    [Fact]
    public void Value_WhenCreatedWithNull_ReturnsNull()
    {
        var s = new SecureString(null);

        s.Value.ShouldBeNull();
    }

    [Fact]
    public void ToString_ReturnsUnderlyingString()
    {
        var s = new SecureString("token-abc");

        s.ToString().ShouldBe("token-abc");
    }

    // ── Equality ─────────────────────────────────────────────────────────────

    [Fact]
    public void Equals_SameValue_ReturnsTrue()
    {
        var a = new SecureString("abc");
        var b = new SecureString("abc");

        a.Equals(b).ShouldBeTrue();
    }

    [Fact]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        var a = new SecureString("abc");
        var b = new SecureString("xyz");

        a.Equals(b).ShouldBeFalse();
    }

    [Fact]
    public void Equals_BothNull_ReturnsTrue()
    {
        var a = new SecureString(null);
        var b = new SecureString(null);

        a.Equals(b).ShouldBeTrue();
    }

    [Fact]
    public void EqualityOperator_SameValue_ReturnsTrue()
    {
        var a = new SecureString("hello");
        var b = new SecureString("hello");

        (a == b).ShouldBeTrue();
    }

    [Fact]
    public void InequalityOperator_DifferentValue_ReturnsTrue()
    {
        var a = new SecureString("foo");
        var b = new SecureString("bar");

        (a != b).ShouldBeTrue();
    }

    [Fact]
    public void GetHashCode_SameValue_ReturnsSameHash()
    {
        new SecureString("abc").GetHashCode()
            .ShouldBe(new SecureString("abc").GetHashCode());
    }

    [Fact]
    public void GetHashCode_NullValue_ReturnsZero()
    {
        new SecureString(null).GetHashCode().ShouldBe(0);
    }

    [Fact]
    public void Equals_BoxedObject_SameValue_ReturnsTrue()
    {
        var a = new SecureString("abc");
        object b = new SecureString("abc");

        a.Equals(b).ShouldBeTrue();
    }

    [Fact]
    public void Equals_BoxedObjectOfDifferentType_ReturnsFalse()
    {
        var a = new SecureString("abc");

        a.Equals("abc").ShouldBeFalse();
    }

    // ── JSON serialisation (redaction OFF) ───────────────────────────────────

    [Fact]
    public void Serialize_WithRedactionDisabled_EmitsRealValue()
    {
        SecureString.DisableRedaction();

        var json = JsonSerializer.Serialize(new SecureString("my-token"));

        json.ShouldBe("\"my-token\"");
    }

    [Fact]
    public void Serialize_NullValue_WithRedactionDisabled_EmitsJsonNull()
    {
        SecureString.DisableRedaction();

        var json = JsonSerializer.Serialize(new SecureString(null));

        json.ShouldBe("null");
    }

    // ── JSON serialisation (redaction ON) ────────────────────────────────────

    [Fact]
    public void Serialize_WithRedactionEnabled_EmitsRedactedPlaceholder()
    {
        using var _ = SecureString.WithRedactionEnabled();

        var json = JsonSerializer.Serialize(new SecureString("super-secret"));

        json.ShouldBe("\"**********\"");
    }

    [Fact]
    public void Serialize_NullValue_WithRedactionEnabled_EmitsRedactedPlaceholder()
    {
        using var _ = SecureString.WithRedactionEnabled();

        var json = JsonSerializer.Serialize(new SecureString(null));

        json.ShouldBe("\"**********\"");
    }

    // ── JSON deserialisation ─────────────────────────────────────────────────

    [Fact]
    public void Deserialize_ReturnsSecureStringWithCorrectValue()
    {
        var result = JsonSerializer.Deserialize<SecureString>("\"hello\"");

        result.Value.ShouldBe("hello");
    }

    [Fact]
    public void Deserialize_NullJson_ReturnsDefaultSecureString()
    {
        var result = JsonSerializer.Deserialize<SecureString>("null");

        result.Value.ShouldBeNull();
    }

    // ── WithRedactionEnabled scope (IDisposable overload) ────────────────────

    [Fact]
    public void WithRedactionEnabled_Scope_EnablesAndThenRestoresPriorState()
    {
        SecureString.DisableRedaction();

        using (var _ = SecureString.WithRedactionEnabled())
        {
            SecureString.IsRedactionEnabled.ShouldBeTrue();
        }

        SecureString.IsRedactionEnabled.ShouldBeFalse();
    }

    [Fact]
    public void WithRedactionEnabled_Scope_RestoresEvenWhenAlreadyEnabled()
    {
        using (SecureString.WithRedactionEnabled())
        {
            // nested scope
            using var inner = SecureString.WithRedactionEnabled();
            SecureString.IsRedactionEnabled.ShouldBeTrue();
        }

        SecureString.IsRedactionEnabled.ShouldBeFalse();
    }

    // ── WithRedactionEnabled(Action) overload ────────────────────────────────

    [Fact]
    public void WithRedactionEnabled_Action_RunsActionWithRedactionOn()
    {
        SecureString.DisableRedaction();
        bool? capturedState = null;

        SecureString.WithRedactionEnabled(() => capturedState = SecureString.IsRedactionEnabled);

        capturedState.ShouldBe(true);
    }

    [Fact]
    public void WithRedactionEnabled_Action_RestoresStateAfterAction()
    {
        SecureString.DisableRedaction();

        SecureString.WithRedactionEnabled(() => { /* no-op */ });

        SecureString.IsRedactionEnabled.ShouldBeFalse();
    }

    [Fact]
    public void WithRedactionEnabled_Action_RestoresStateWhenActionThrows()
    {
        SecureString.DisableRedaction();

        Should.Throw<InvalidOperationException>(
            () => SecureString.WithRedactionEnabled(() => throw new InvalidOperationException()));

        SecureString.IsRedactionEnabled.ShouldBeFalse();
    }

    // ── WithRedactionEnabled<T>(Func<T>) overload ────────────────────────────

    [Fact]
    public void WithRedactionEnabled_Func_ReturnsResultFromDelegate()
    {
        SecureString.DisableRedaction();

        var result = SecureString.WithRedactionEnabled(() => 42);

        result.ShouldBe(42);
    }

    [Fact]
    public void WithRedactionEnabled_Func_RestoresStateAfterDelegate()
    {
        SecureString.DisableRedaction();

        SecureString.WithRedactionEnabled(() => 0);

        SecureString.IsRedactionEnabled.ShouldBeFalse();
    }

    [Fact]
    public void WithRedactionEnabled_Func_RestoresStateWhenDelegateThrows()
    {
        SecureString.DisableRedaction();

        Should.Throw<InvalidOperationException>(
            () => SecureString.WithRedactionEnabled<int>(() => throw new InvalidOperationException()));

        SecureString.IsRedactionEnabled.ShouldBeFalse();
    }

    // ── IsRedactionEnabled ───────────────────────────────────────────────────

    [Fact]
    public void IsRedactionEnabled_AfterEnableRedaction_ReturnsTrue()
    {
        SecureString.EnableRedaction();

        SecureString.IsRedactionEnabled.ShouldBeTrue();
    }

    [Fact]
    public void IsRedactionEnabled_AfterDisableRedaction_ReturnsFalse()
    {
        SecureString.EnableRedaction();
        SecureString.DisableRedaction();

        SecureString.IsRedactionEnabled.ShouldBeFalse();
    }
}
