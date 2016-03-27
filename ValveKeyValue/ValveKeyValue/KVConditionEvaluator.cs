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
            Expression expression;
            try
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

                expression = CreateExpression(tokens);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidDataException($"Invalid conditional syntax \"{expressionText}\"", ex);
            }

            var value = (bool)Expression.Lambda(expression).Compile().DynamicInvoke();
            return value;
        }

        bool EvaluateVariable(string variable) => definedVariables.Contains(variable);

        Expression CreateExpression(IList<KVConditionToken> tokens)
        {
            if (tokens.Count == 0)
            {
                throw new InvalidOperationException($"{nameof(CreateExpression)} called with no condition tokens.");
            }

            PreprocessBracketedExpressions(tokens);

            KVConditionToken token;

            // Process AND and OR next. Split the list of expressions into two parts, and recursively process
            // each part before joining in the desired expression.
            for (int i = 0; i < tokens.Count; i++)
            {
                token = tokens[i];
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

            if (tokens.Count == 2 && tokens[0].TokenType == KVConditionTokenType.Negation)
            {
                var positiveExpression = CreateExpression(tokens.Skip(1).ToList());
                return Expression.Not(positiveExpression);
            }
            else if (tokens.Count == 1)
            {
                token = tokens.Single();
                switch (token.TokenType)
                {
                    case KVConditionTokenType.Value:
                        return EvaluteVariableExpression((string)token.Value);

                    case KVConditionTokenType.PreprocessedExpression:
                        return (Expression)token.Value;
                }
            }

            throw new InvalidOperationException("Invalid conditional syntax.");
        }

        void PreprocessBracketedExpressions(IList<KVConditionToken> tokens)
        {
            int startIndex;
            for (startIndex = 0; startIndex < tokens.Count; startIndex++)
            {
                if (tokens[startIndex].TokenType == KVConditionTokenType.BeginSubExpression)
                {
                    break;
                }
            }

            if (startIndex == tokens.Count)
            {
                return;
            }

            int endIndex;
            for (endIndex = tokens.Count - 1; endIndex > startIndex; endIndex--)
            {
                if (tokens[endIndex].TokenType == KVConditionTokenType.EndSubExpression)
                {
                    break;
                }
            }

            if (endIndex == 0)
            {
                return;
            }

            var subRange = tokens.Skip(startIndex + 1).Take(endIndex - startIndex - 1).ToList();
            var evalutedExpression = CreateExpression(subRange);

            for (int i = 0; i < endIndex - startIndex + 1; i++)
            {
                tokens.RemoveAt(startIndex);
            }

            tokens.Insert(startIndex, new KVConditionToken(evalutedExpression));
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

                case '(':
                    return KVConditionToken.LeftParenthesis;

                case ')':
                    return KVConditionToken.RightParenthesis;
            }

            throw new InvalidOperationException("Bad condition syntax.");
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
            BeginSubExpression,
            EndSubExpression,
            PreprocessedExpression // Used internally for bracketed expressions
        }

        class KVConditionToken
        {
            public KVConditionToken(string variable)
                : this(KVConditionTokenType.Value)
            {
                Value = variable;
            }

            public KVConditionToken(Expression expression)
                 : this(KVConditionTokenType.PreprocessedExpression)
            {
                Value = expression;
            }

            KVConditionToken(KVConditionTokenType type)
            {
                TokenType = type;
            }

            public KVConditionTokenType TokenType { get; }

            public object Value { get; }

            public static KVConditionToken Not
                => new KVConditionToken(KVConditionTokenType.Negation);

            public static KVConditionToken Or
                => new KVConditionToken(KVConditionTokenType.OrJoin);

            public static KVConditionToken And
                => new KVConditionToken(KVConditionTokenType.AndJoin);

            public static KVConditionToken LeftParenthesis
                => new KVConditionToken(KVConditionTokenType.BeginSubExpression);

            public static KVConditionToken RightParenthesis
                => new KVConditionToken(KVConditionTokenType.EndSubExpression);
        }
    }
}
