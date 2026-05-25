using System.Collections.Generic;
using System.Text.Json;
using Lando.HomeAssistant.Models;

namespace Lando.HomeAssistant.Extensions.Tests;

public class HomeAssistantEntityAttributesExtensionsTests
{
    // ── helpers ───────────────────────────────────────────────────────────────

    private static JsonElement J(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private static Dictionary<string, object> A(params (string k, object v)[] pairs)
    {
        var d = new Dictionary<string, object>();
        foreach (var (k, v) in pairs)
            d[k] = v;
        return d;
    }

    // ── GetBool / GetBool — null / missing ─────────────────────────────────

    [Fact]
    public void GetBool_NullDictionary_ReturnsNull()
    {
        IDictionary<string, object>? attrs = null;
        attrs.GetBool("key").ShouldBeNull();
    }

    [Fact]
    public void GetBool_MissingKey_ReturnsNull()
    {
        IDictionary<string, object> attrs = A(("other", (object)"x"));
        attrs.GetBool("missing").ShouldBeNull();
    }

    // ── GetBool — bool literal ─────────────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetBool_BoolValue_ReturnsThatValue(bool expected)
    {
        IDictionary<string, object> attrs = A(("k", (object)expected));
        attrs.GetBool("k").ShouldBe(expected);
    }

    // ── GetBool — JsonElement ──────────────────────────────────────────────

    [Fact]
    public void GetBool_JsonElementTrue_ReturnsTrue()
    {
        IDictionary<string, object> attrs = A(("k", (object)J("true")));
        attrs.GetBool("k").ShouldBe(true);
    }

    [Fact]
    public void GetBool_JsonElementFalse_ReturnsFalse()
    {
        IDictionary<string, object> attrs = A(("k", (object)J("false")));
        attrs.GetBool("k").ShouldBe(false);
    }

    [Fact]
    public void GetBool_JsonElementNull_ReturnsNull()
    {
        IDictionary<string, object> attrs = A(("k", (object)J("null")));
        attrs.GetBool("k").ShouldBeNull();
    }

    [Theory]
    [InlineData("\"true\"", true)]
    [InlineData("\"false\"", false)]
    [InlineData("\"True\"", true)]
    public void GetBool_JsonElementStringParseable_ReturnsParsedValue(string json, bool expected)
    {
        IDictionary<string, object> attrs = A(("k", (object)J(json)));
        attrs.GetBool("k").ShouldBe(expected);
    }

    [Fact]
    public void GetBool_JsonElementStringUnparseable_ReturnsNull()
    {
        IDictionary<string, object> attrs = A(("k", (object)J("\"not-a-bool\"")));
        attrs.GetBool("k").ShouldBeNull();
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("0", false)]
    public void GetBool_JsonElementNumber_ReturnsNonZeroIsTrue(string json, bool expected)
    {
        IDictionary<string, object> attrs = A(("k", (object)J(json)));
        attrs.GetBool("k").ShouldBe(expected);
    }

    // ── GetBool — string literal ───────────────────────────────────────────

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void GetBool_StringParseable_ReturnsParsedValue(string raw, bool expected)
    {
        IDictionary<string, object> attrs = A(("k", (object)raw));
        attrs.GetBool("k").ShouldBe(expected);
    }

    [Fact]
    public void GetBool_StringUnparseable_ReturnsNull()
    {
        IDictionary<string, object> attrs = A(("k", (object)"yes"));
        attrs.GetBool("k").ShouldBeNull();
    }

    // ── GetBool — int / double sentinels ───────────────────────────────────

    [Fact]
    public void GetBool_Int1_ReturnsTrue()
    {
        IDictionary<string, object> attrs = A(("k", (object)1));
        attrs.GetBool("k").ShouldBe(true);
    }

    [Fact]
    public void GetBool_Int0_ReturnsFalse()
    {
        IDictionary<string, object> attrs = A(("k", (object)0));
        attrs.GetBool("k").ShouldBe(false);
    }

    [Fact]
    public void GetBool_Double1_ReturnsTrue()
    {
        IDictionary<string, object> attrs = A(("k", (object)1.0));
        attrs.GetBool("k").ShouldBe(true);
    }

    [Fact]
    public void GetBool_Double0_ReturnsFalse()
    {
        IDictionary<string, object> attrs = A(("k", (object)0.0));
        attrs.GetBool("k").ShouldBe(false);
    }

    // ── GetString ─────────────────────────────────────────────────────────────

    [Fact]
    public void GetString_NullDictionary_ReturnsNull() =>
        ((IDictionary<string, object>?)null).GetString("key").ShouldBeNull();

    [Fact]
    public void GetString_MissingKey_ReturnsNull()
    {
        IDictionary<string, object> attrs = A(("other", (object)"x"));
        attrs.GetString("missing").ShouldBeNull();
    }

    [Fact]
    public void GetString_StringValue_ReturnsSameString()
    {
        IDictionary<string, object> attrs = A(("k", (object)"hello"));
        attrs.GetString("k").ShouldBe("hello");
    }

    [Fact]
    public void GetString_JsonElementString_ReturnsString()
    {
        IDictionary<string, object> attrs = A(("k", (object)J("\"world\"")));
        attrs.GetString("k").ShouldBe("world");
    }

    [Fact]
    public void GetString_JsonElementNull_ReturnsNull()
    {
        IDictionary<string, object> attrs = A(("k", (object)J("null")));
        attrs.GetString("k").ShouldBeNull();
    }

    [Fact]
    public void GetString_JsonElementNumber_ReturnsStringRepresentation()
    {
        IDictionary<string, object> attrs = A(("k", (object)J("42")));
        attrs.GetString("k").ShouldBe("42");
    }

    // ── GetInt ────────────────────────────────────────────────────────────────

    [Fact]
    public void GetInt_NullDictionary_ReturnsNull() =>
        ((IDictionary<string, object>?)null).GetInt("key").ShouldBeNull();

    [Fact]
    public void GetInt_MissingKey_ReturnsNull()
    {
        IDictionary<string, object> attrs = A(("other", (object)"x"));
        attrs.GetInt("missing").ShouldBeNull();
    }

    [Fact]
    public void GetInt_IntValue_ReturnsInt()
    {
        IDictionary<string, object> attrs = A(("k", (object)42));
        attrs.GetInt("k").ShouldBe(42);
    }

    [Fact]
    public void GetInt_LongValue_ReturnsCastInt()
    {
        IDictionary<string, object> attrs = A(("k", (object)100L));
        attrs.GetInt("k").ShouldBe(100);
    }

    [Fact]
    public void GetInt_DoubleValue_ReturnsTruncatedInt()
    {
        IDictionary<string, object> attrs = A(("k", (object)3.9));
        attrs.GetInt("k").ShouldBe(3);
    }

    [Fact]
    public void GetInt_JsonElementNumber_ReturnsInt()
    {
        IDictionary<string, object> attrs = A(("k", (object)J("7")));
        attrs.GetInt("k").ShouldBe(7);
    }

    [Fact]
    public void GetInt_JsonElementStringNumber_ReturnsInt()
    {
        IDictionary<string, object> attrs = A(("k", (object)J("\"7\"")));
        attrs.GetInt("k").ShouldBe(7);
    }

    [Fact]
    public void GetInt_JsonElementNull_ReturnsNull()
    {
        IDictionary<string, object> attrs = A(("k", (object)J("null")));
        attrs.GetInt("k").ShouldBeNull();
    }

    [Fact]
    public void GetInt_StringParseable_ReturnsParsed()
    {
        IDictionary<string, object> attrs = A(("k", (object)"99"));
        attrs.GetInt("k").ShouldBe(99);
    }

    [Fact]
    public void GetInt_StringUnparseable_ReturnsNull()
    {
        IDictionary<string, object> attrs = A(("k", (object)"abc"));
        attrs.GetInt("k").ShouldBeNull();
    }

    // ── GetDouble ─────────────────────────────────────────────────────────────

    [Fact]
    public void GetDouble_NullDictionary_ReturnsNull() =>
        ((IDictionary<string, object>?)null).GetDouble("key").ShouldBeNull();

    [Fact]
    public void GetDouble_DoubleValue_ReturnsDouble()
    {
        IDictionary<string, object> attrs = A(("k", (object)3.14));
        attrs.GetDouble("k").ShouldBe(3.14);
    }

    [Fact]
    public void GetDouble_IntValue_ReturnsDouble()
    {
        IDictionary<string, object> attrs = A(("k", (object)5));
        attrs.GetDouble("k").ShouldBe(5.0);
    }

    [Fact]
    public void GetDouble_LongValue_ReturnsDouble()
    {
        IDictionary<string, object> attrs = A(("k", (object)10L));
        attrs.GetDouble("k").ShouldBe(10.0);
    }

    [Fact]
    public void GetDouble_JsonElementNumber_ReturnsDouble()
    {
        IDictionary<string, object> attrs = A(("k", (object)J("2.5")));
        attrs.GetDouble("k").ShouldBe(2.5);
    }

    [Fact]
    public void GetDouble_JsonElementStringNumber_ReturnsDouble()
    {
        IDictionary<string, object> attrs = A(("k", (object)J("\"2.5\"")));
        attrs.GetDouble("k").ShouldBe(2.5);
    }

    [Fact]
    public void GetDouble_JsonElementNull_ReturnsNull()
    {
        IDictionary<string, object> attrs = A(("k", (object)J("null")));
        attrs.GetDouble("k").ShouldBeNull();
    }

    [Fact]
    public void GetDouble_StringParseable_ReturnsParsed()
    {
        IDictionary<string, object> attrs = A(("k", (object)"1.23"));
        attrs.GetDouble("k").ShouldBe(1.23);
    }

    [Fact]
    public void GetDouble_StringUnparseable_ReturnsNull()
    {
        IDictionary<string, object> attrs = A(("k", (object)"nope"));
        attrs.GetDouble("k").ShouldBeNull();
    }

    // ── GetStringArray ────────────────────────────────────────────────────────

    [Fact]
    public void GetStringArray_NullDictionary_ReturnsNull() =>
        ((IDictionary<string, object>?)null).GetStringArray("key").ShouldBeNull();

    [Fact]
    public void GetStringArray_MissingKey_ReturnsNull()
    {
        IDictionary<string, object> attrs = A(("other", (object)"x"));
        attrs.GetStringArray("missing").ShouldBeNull();
    }

    [Fact]
    public void GetStringArray_JsonElementArray_ReturnsStrings()
    {
        IDictionary<string, object> attrs = A(("k", (object)J("[\"color\",\"brightness\"]")));
        attrs.GetStringArray("k").ShouldBe(["color", "brightness"]);
    }

    [Fact]
    public void GetStringArray_JsonElementArrayWithNumbers_CoercesToString()
    {
        IDictionary<string, object> attrs = A(("k", (object)J("[1,2,3]")));
        attrs.GetStringArray("k").ShouldBe(["1", "2", "3"]);
    }

    [Fact]
    public void GetStringArray_EnumerableOfObject_ReturnsStrings()
    {
        IDictionary<string, object> attrs = A(("k", (object)new List<object> { "heat", "cool" }));
        attrs.GetStringArray("k").ShouldBe(["heat", "cool"]);
    }

    [Fact]
    public void GetStringArray_NonArrayValue_ReturnsNull()
    {
        IDictionary<string, object> attrs = A(("k", (object)"not-an-array"));
        attrs.GetStringArray("k").ShouldBeNull();
    }
}
