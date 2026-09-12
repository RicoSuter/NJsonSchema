using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NJsonSchema.Infrastructure;

/// <summary>Canonical identity of a finite JSON number, without floating point conversion.</summary>
internal sealed class JsonNumber : IEquatable<JsonNumber>
{
    private readonly bool _negative;
    private readonly string _digits;
    private readonly BigInteger _exponent;

    private JsonNumber(bool negative, string digits, BigInteger exponent)
    {
        _negative = negative;
        _digits = digits;
        _exponent = exponent;
    }

    public bool IsInteger => _exponent.Sign >= 0;

    /// <summary>Reads exact numeric text from a parsed JSON value.</summary>
    public static JsonNumber Parse(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Number)
        {
            throw new ArgumentException("The JSON value must be a number.", nameof(element));
        }

        return ParseValidText(element.GetRawText());
    }

    /// <summary>Serializes CLR-backed numeric values using JSON's finite-number rules.</summary>
    public static bool TryCreate(JsonValue value, out JsonNumber? number)
    {
        if (value.GetValueKind() != JsonValueKind.Number)
        {
            number = null;
            return false;
        }

        number = value.TryGetValue<JsonElement>(out var element)
            ? Parse(element)
            : ParseValidText(value.ToJsonString());
        return true;
    }

    private static JsonNumber ParseValidText(string text)
    {
        var negative = text[0] == '-';
        var start = negative ? 1 : 0;
        var exponentStart = text.IndexOfAny(['e', 'E']);
        var coefficientEnd = exponentStart < 0 ? text.Length : exponentStart;
        var exponent = exponentStart < 0
            ? BigInteger.Zero
#if NET8_0_OR_GREATER
            : BigInteger.Parse(text.AsSpan(exponentStart + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
#else
            : BigInteger.Parse(text.Substring(exponentStart + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
#endif
        var decimalPoint = text.IndexOf('.');
        var digits = text.Substring(start, coefficientEnd - start);
        if (decimalPoint >= 0)
        {
            exponent -= coefficientEnd - decimalPoint - 1;
            digits = digits.Remove(decimalPoint - start, 1);
        }

        var first = 0;
        while (first < digits.Length && digits[first] == '0')
        {
            first++;
        }

        if (first == digits.Length)
        {
            return new JsonNumber(false, "0", BigInteger.Zero);
        }

        var last = digits.Length - 1;
        while (digits[last] == '0')
        {
            last--;
            exponent++;
        }

        return new JsonNumber(negative, digits.Substring(first, last - first + 1), exponent);
    }

    /// <summary>Emits a plain integer only when it fits the caller's output-length budget.</summary>
    public bool TryGetIntegerLiteral(int maximumLength, out string? literal)
    {
        var length = _exponent + _digits.Length + (_negative ? 1 : 0);
        if (!IsInteger || maximumLength < 1 || length > maximumLength)
        {
            literal = null;
            return false;
        }

        literal = (_negative ? "-" : "") + _digits + new string('0', (int)_exponent);
        return true;
    }

    public bool Equals(JsonNumber? other) => other != null &&
        _negative == other._negative && _digits == other._digits && _exponent == other._exponent;

    public override bool Equals(object? obj) => obj is JsonNumber other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return ((_negative.GetHashCode() * 397) ^ StringComparer.Ordinal.GetHashCode(_digits)) * 397 ^ _exponent.GetHashCode();
        }
    }
}
