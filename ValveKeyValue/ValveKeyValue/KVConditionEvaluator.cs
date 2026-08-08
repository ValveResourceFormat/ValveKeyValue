namespace ValveKeyValue
{
    // Evaluates C style infix parenthetic logical expressions, e.g. ($WIN32 || $X360) && !$GERMAN.
    // Supports $<identifier>, numeric literals (0 is false, non-zero is true), !, ||, &&, ().
    // Modelled on CExpressionEvaluator in Valve's tier1/exprevaluator.cpp, where && and ||
    // have equal precedence and are left-associative.
    class KVConditionEvaluator
    {
        public KVConditionEvaluator(ICollection<string> definedVariables)
        {
            ArgumentNullException.ThrowIfNull(definedVariables);

            this.definedVariables = definedVariables;
        }

        // Bail out before pathologically nested input (thousands of '(' or '!') can overflow the stack.
        const int MaximumNestingDepth = 128;

        readonly ICollection<string> definedVariables;

        string expressionText = string.Empty;
        int position;

        public bool Evaluate(string expressionText)
        {
            ArgumentNullException.ThrowIfNull(expressionText);

            this.expressionText = expressionText;
            position = 0;

            try
            {
                var result = EvaluateExpression(depth: 0);

                SkipWhitespace();
                if (position < expressionText.Length)
                {
                    throw new InvalidOperationException($"Unexpected '{expressionText[position]}' after end of expression.");
                }

                return result;
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidDataException($"Invalid conditional syntax \"{expressionText}\"", ex);
            }
        }

        // expression := term { ('&&' | '||') term }
        bool EvaluateExpression(int depth)
        {
            var result = EvaluateTerm(depth);

            while (true)
            {
                SkipWhitespace();

                if (TrySkipOperator('&'))
                {
                    result &= EvaluateTerm(depth);
                }
                else if (TrySkipOperator('|'))
                {
                    result |= EvaluateTerm(depth);
                }
                else
                {
                    break;
                }
            }

            return result;
        }

        // term := '!' term | '(' expression ')' | '$' identifier | digits
        bool EvaluateTerm(int depth)
        {
            if (depth > MaximumNestingDepth)
            {
                throw new InvalidOperationException("Expression is nested too deeply.");
            }

            SkipWhitespace();

            if (position >= expressionText.Length)
            {
                throw new InvalidOperationException("Unexpected end of expression.");
            }

            var current = expressionText[position];
            switch (current)
            {
                case '!':
                    position++;
                    return !EvaluateTerm(depth + 1);

                case '(':
                    {
                        position++;
                        var result = EvaluateExpression(depth + 1);

                        SkipWhitespace();
                        if (position >= expressionText.Length || expressionText[position] != ')')
                        {
                            throw new InvalidOperationException("Unterminated bracketed expression.");
                        }

                        position++;
                        return result;
                    }

                case '$':
                    position++;
                    return EvaluateVariable(ReadVariableName());

                case >= '0' and <= '9':
                    return ReadNumericLiteral();
            }

            throw new InvalidOperationException($"Unexpected '{current}'.");
        }

        // Case-insensitive to match Valve, whose symbol resolution uses V_stricmp.
        bool EvaluateVariable(string variable)
        {
            foreach (var definedVariable in definedVariables)
            {
                if (string.Equals(definedVariable, variable, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        bool TrySkipOperator(char operatorCharacter)
        {
            if (position + 1 < expressionText.Length
                && expressionText[position] == operatorCharacter
                && expressionText[position + 1] == operatorCharacter)
            {
                position += 2;
                return true;
            }

            return false;
        }

        bool ReadNumericLiteral()
        {
            var isNonZero = false;
            while (position < expressionText.Length && char.IsAsciiDigit(expressionText[position]))
            {
                isNonZero |= expressionText[position] != '0';
                position++;
            }

            return isNonZero;
        }

        string ReadVariableName()
        {
            var start = position;
            while (position < expressionText.Length && IsVariableCharacter(expressionText[position]))
            {
                position++;
            }

            return expressionText[start..position];
        }

        static bool IsVariableCharacter(char c) => char.IsLetterOrDigit(c) || c == '_';

        void SkipWhitespace()
        {
            while (position < expressionText.Length && char.IsWhiteSpace(expressionText[position]))
            {
                position++;
            }
        }
    }
}
