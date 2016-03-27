using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ValveKeyValue
{
    class KVConditionEvaluator
    {
        public KVConditionEvaluator(ICollection<string> definedVariables)
        {
            Require.NotNull(definedVariables, nameof(definedVariables));

            this.definedVariables = definedVariables;
        }

        ICollection<string> definedVariables;

        public bool Evalute(string expressionText)
        {
            var tokens = new List<KVConditionToken>();
            using (var reader = new StringReader(expressionText))
            {
                KVConditionToken token;
                while ((token = ReadToken(reader)) != null)
                {
                    tokens.Add(token);
                }
            }

            var expression = CreateExpression(tokens);
            var value = Expression.Lambda(expression).Compile().DynamicInvoke() as bool?;
            return value.Value;
        }

        bool EvaluateVariable(string variable) => definedVariables.Contains(variable);

        Expression CreateExpression(IList<KVConditionToken> tokens)
        {
            if (tokens.Count == 0)
            {
                throw new ArgumentException($"{nameof(CreateExpression)} called with no condition tokens.", nameof(tokens));
            }

            // Process AND and OR first. Split the list of expressions into two parts, and recursively process
            // each part before joining in the desired expression.
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token.TokenType != KVConditionTokenType.OrJoin && token.TokenType != KVConditionTokenType.AndJoin)
                {
                    continue;
                }

                var left = tokens.Take(i).ToList();
                var right = tokens.Skip(i + 1).ToList();

                var leftExpression = CreateExpression(left);
                var rightExpression = CreateExpression(right);

                switch (token.TokenType)
                {
                    case KVConditionTokenType.OrJoin:
                        return Expression.OrElse(leftExpression, rightExpression);

                    case KVConditionTokenType.AndJoin:
                        return Expression.AndAlso(leftExpression, rightExpression);
                }
            }

            if (tokens[0].TokenType == KVConditionTokenType.Negation)
            {
                var positiveExpression = CreateExpression(tokens.Skip(1).ToList());
                return Expression.Not(positiveExpression);
            }

            if (tokens[0].TokenType == KVConditionTokenType.Value)
            {
                return EvaluteVariableExpression(tokens[0].Value);
            }

            throw new InvalidOperationException("What the hell am I doing?");
        }

        Expression EvaluteVariableExpression(string variable)
        {
            var instance = Expression.Constant(this);
            var method = typeof(KVConditionEvaluator)
                .GetMethod(nameof(EvaluateVariable), BindingFlags.NonPublic | BindingFlags.Instance);
            return Expression.Call(instance, method, new[] { Expression.Constant(variable) });
        }

        static KVConditionToken ReadToken(TextReader reader)
        {
            SkipWhitespace(reader);

            var current = reader.Read();
            if (current == -1)
            {
                return null;
            }

            switch (current)
            {
                case -1:
                    return null; // End of string

                case '$':
                    return ReadVariableToken(reader);

                case '!':
                    return KVConditionToken.Not;

                case '|':
                {
                    var next = reader.Peek();
                    if (next != -1 && (char)next == '|')
                    {
                            reader.Read();
                        return KVConditionToken.Or;
                    }

                    break;
                    }

                case '&':
                {
                    var next = reader.Peek();
                    if (next != -1 && (char)next == '&')
                    {
                        reader.Read();
                        return KVConditionToken.And;
                    }

                    break;
                }
            }

            throw new InvalidDataException("Bad condition syntax.");
        }

        static void SkipWhitespace(TextReader reader)
        {
            var next = reader.Peek();
            while (next != -1 && char.IsWhiteSpace((char)next))
            {
                reader.Read();
                next = reader.Peek();
            }
        }

        static KVConditionToken ReadVariableToken(TextReader reader)
        {
            var builder = new StringBuilder();
            while (IsReadableVariableCharacter(reader.Peek()))
            {
                builder.Append((char)reader.Read());
            }

            return new KVConditionToken(builder.ToString());
        }

        static bool IsReadableVariableCharacter(int value)
        {
            if (value == -1)
            {
                return false;
            }

            return char.IsLetterOrDigit((char)value);
        }

        enum KVConditionTokenType
        {
            Value,
            Negation,
            OrJoin,
            AndJoin,
        }

        class KVConditionToken
        {
            public KVConditionToken(string variable)
                : this(KVConditionTokenType.Value)
            {
                Value = variable;
            }

            KVConditionToken(KVConditionTokenType type)
            {
                TokenType = type;
            }

            public KVConditionTokenType TokenType { get; }

            public string Value { get; }

            public static KVConditionToken Not
                => new KVConditionToken(KVConditionTokenType.Negation);

            public static KVConditionToken Or
                => new KVConditionToken(KVConditionTokenType.OrJoin);

            public static KVConditionToken And
                => new KVConditionToken(KVConditionTokenType.AndJoin);
        }
    }
}
