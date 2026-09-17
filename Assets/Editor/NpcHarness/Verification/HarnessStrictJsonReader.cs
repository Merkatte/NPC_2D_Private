using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

internal sealed class HarnessStrictJsonReader
{
    private readonly string _json;
    private int _index;

    private HarnessStrictJsonReader(string json)
    {
        _json = json;
    }

    public static object Parse(string json)
    {
        HarnessStrictJsonReader reader = new HarnessStrictJsonReader(json ?? string.Empty);
        object value = reader.ReadValue();
        reader.SkipWhitespace();
        if (!reader.IsAtEnd)
        {
            reader.ThrowInvalid("Unexpected trailing content");
        }

        return value;
    }

    private bool IsAtEnd => _index >= _json.Length;

    private object ReadValue()
    {
        SkipWhitespace();
        if (IsAtEnd)
        {
            ThrowInvalid("Expected a JSON value");
        }

        switch (_json[_index])
        {
            case '{': return ReadObject();
            case '[': return ReadArray();
            case '"': return ReadString();
            case 't': ReadLiteral("true"); return true;
            case 'f': ReadLiteral("false"); return false;
            case 'n': ReadLiteral("null"); return null;
            default: return ReadNumber();
        }
    }

    private IDictionary<string, object> ReadObject()
    {
        Dictionary<string, object> result = new Dictionary<string, object>(StringComparer.Ordinal);
        _index++;
        SkipWhitespace();
        if (TryRead('}'))
        {
            return result;
        }

        while (true)
        {
            SkipWhitespace();
            if (IsAtEnd || _json[_index] != '"')
            {
                ThrowInvalid("Expected an object field name");
            }

            string fieldName = ReadString();
            if (result.ContainsKey(fieldName))
            {
                ThrowInvalid($"Duplicate object field {fieldName}");
            }

            SkipWhitespace();
            Require(':');
            result.Add(fieldName, ReadValue());
            SkipWhitespace();
            if (TryRead('}'))
            {
                return result;
            }

            Require(',');
        }
    }

    private IList<object> ReadArray()
    {
        List<object> result = new List<object>();
        _index++;
        SkipWhitespace();
        if (TryRead(']'))
        {
            return result;
        }

        while (true)
        {
            result.Add(ReadValue());
            SkipWhitespace();
            if (TryRead(']'))
            {
                return result;
            }

            Require(',');
        }
    }

    private string ReadString()
    {
        Require('"');
        StringBuilder result = new StringBuilder();
        while (!IsAtEnd)
        {
            char current = _json[_index++];
            if (current == '"')
            {
                return result.ToString();
            }

            if (current < 0x20)
            {
                ThrowInvalid("A JSON string contains an unescaped control character");
            }

            if (current != '\\')
            {
                result.Append(current);
                continue;
            }

            if (IsAtEnd)
            {
                ThrowInvalid("A JSON string ends with an incomplete escape");
            }

            char escaped = _json[_index++];
            switch (escaped)
            {
                case '"': result.Append('"'); break;
                case '\\': result.Append('\\'); break;
                case '/': result.Append('/'); break;
                case 'b': result.Append('\b'); break;
                case 'f': result.Append('\f'); break;
                case 'n': result.Append('\n'); break;
                case 'r': result.Append('\r'); break;
                case 't': result.Append('\t'); break;
                case 'u': result.Append(ReadUnicodeEscape()); break;
                default: ThrowInvalid($"Unsupported JSON escape \\{escaped}"); break;
            }
        }

        ThrowInvalid("Unterminated JSON string");
        return string.Empty;
    }

    private char ReadUnicodeEscape()
    {
        if (_index + 4 > _json.Length)
        {
            ThrowInvalid("Incomplete JSON unicode escape");
        }

        string digits = _json.Substring(_index, 4);
        if (!ushort.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ushort value))
        {
            ThrowInvalid("Invalid JSON unicode escape");
        }

        _index += 4;
        return (char)value;
    }

    private double ReadNumber()
    {
        int start = _index;
        TryRead('-');
        if (TryRead('0'))
        {
            if (!IsAtEnd && char.IsDigit(_json[_index]))
            {
                ThrowInvalid("A JSON number cannot contain a leading zero");
            }
        }
        else
        {
            ReadDigits("Expected a JSON number");
        }

        if (TryRead('.'))
        {
            ReadDigits("Expected digits after a JSON decimal point");
        }

        if (TryRead('e') || TryRead('E'))
        {
            if (!TryRead('+'))
            {
                TryRead('-');
            }
            ReadDigits("Expected digits in a JSON exponent");
        }

        string token = _json.Substring(start, _index - start);
        if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ||
            double.IsNaN(value) ||
            double.IsInfinity(value))
        {
            ThrowInvalid($"Invalid JSON number {token}");
        }

        return value;
    }

    private void ReadDigits(string errorMessage)
    {
        int start = _index;
        while (!IsAtEnd && char.IsDigit(_json[_index]))
        {
            _index++;
        }

        if (_index == start)
        {
            ThrowInvalid(errorMessage);
        }
    }

    private void ReadLiteral(string literal)
    {
        if (_index + literal.Length > _json.Length ||
            !string.Equals(_json.Substring(_index, literal.Length), literal, StringComparison.Ordinal))
        {
            ThrowInvalid($"Expected JSON literal {literal}");
        }

        _index += literal.Length;
    }

    private void SkipWhitespace()
    {
        while (!IsAtEnd)
        {
            char current = _json[_index];
            if (current != ' ' && current != '\t' && current != '\r' && current != '\n')
            {
                return;
            }

            _index++;
        }
    }

    private bool TryRead(char expected)
    {
        if (IsAtEnd || _json[_index] != expected)
        {
            return false;
        }

        _index++;
        return true;
    }

    private void Require(char expected)
    {
        if (!TryRead(expected))
        {
            ThrowInvalid($"Expected '{expected}'");
        }
    }

    private void ThrowInvalid(string message)
    {
        throw new InvalidOperationException($"Invalid JSON at offset {_index}: {message}.");
    }
}
