// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RIFF.Interfaces.Formats.CSV
{
    internal enum TokenType
    {
        Comma,
        Quote,
        Value,
        LineBreak
    }

    public class CSVParser : IEnumerable<String>
    {
        private char _separator;
        private StreamTokenizer _tokenizer;

        public CSVParser(Stream data, char separator = ',')
        {
            _separator = separator;
            _tokenizer = new StreamTokenizer(new StreamReader(data), _separator);
        }

        public CSVParser(string data, char separator = ',')
        {
            _separator = separator;
            _tokenizer = new StreamTokenizer(new StringReader(data), _separator);
        }

        public IEnumerator<string> GetEnumerator()
        {
            Boolean inQuote = false;
            var result = new StringBuilder();

            foreach (Token token in _tokenizer)
            {
                switch (token.Type)
                {
                    case TokenType.LineBreak:
                        if (inQuote)
                        {
                            result.Append(token.Value);
                        }
                        else
                        {
                            if (result.Length > 0)
                            {
                                yield return result.ToString();
                                result.Length = 0;
                            }
                            yield return Environment.NewLine;
                        }
                        break;

                    case TokenType.Comma:
                        if (inQuote)
                        {
                            result.Append(token.Value);
                        }
                        else
                        {
                            yield return result.ToString();
                            result.Length = 0;
                        }
                        break;

                    case TokenType.Quote:
                        // Toggle quote state
                        inQuote = !inQuote;
                        break;

                    case TokenType.Value:
                        result.Append(token.Value);
                        break;

                    default:
                        throw new InvalidOperationException("Unknown token type: " + token.Type);
                }
            }

            if (result.Length > 0)
            {
                yield return result.ToString();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal class StreamTokenizer : IEnumerable<Token>
    {
        private TextReader _reader;
        private char _separator;

        public StreamTokenizer(TextReader reader, char separator = ',')
        {
            _reader = reader;
            _separator = separator;
        }

        public IEnumerator<Token> GetEnumerator()
        {
            var line = _reader.ReadToEnd();
            var value = new StringBuilder();

            foreach (Char c in line)
            {
                if (c == _separator)
                {
                    if (value.Length > 0)
                    {
                        yield return new Token(TokenType.Value, value.ToString());
                        value.Length = 0;
                    }
                    yield return new Token(TokenType.Comma, c.ToString());
                }
                else
                {
                    switch (c)
                    {
                        case '"':
                            yield return new Token(TokenType.Quote, c.ToString());
                            break;
                        case '\r':
                        case '\n':
                            if (value.Length > 0)
                            {
                                yield return new Token(TokenType.Value, value.ToString());
                                value.Length = 0;
                            }
                            yield return new Token(TokenType.LineBreak, c.ToString());
                            break;

                        default:
                            value.Append(c);
                            break;
                    }
                }
            }
            if (value.Length > 0) { yield return new Token(TokenType.Value, value.ToString()); }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal class Token
    {
        public TokenType Type { get; private set; }

        public String Value { get; private set; }

        public Token(TokenType type, string value)
        {
            Value = value;
            Type = type;
        }
    }
}
