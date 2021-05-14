using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace MigoLib
{
    public static class PositionalSerializer
    {    
        public static PositionalSerializer<T> CreateFor<T>(char delimiter)  where T: new()
            => new PositionalSerializer<T>(delimiter);
    }

    public class PositionalSerializer<T> where T: new()
    {
        private readonly List<PositionParser<T>> _fieldParsers;

        private T _data;
        private char _delimiter;
        
        public bool IsError { get; private set; }

        public PositionalSerializer(char delimiter)
        {
            _delimiter = delimiter;
            _fieldParsers = new List<PositionParser<T>>();
            _data = new T();
        }

        public PositionParser<T> GetParser(int fieldIndex)
        {
            if (fieldIndex >= _fieldParsers.Count)
            {
                throw new IndexOutOfRangeException();
            }

            return _fieldParsers[fieldIndex];
        }
        
        public PositionalSerializer<T> Field<TProperty>(
            Expression<Func<T, TProperty>> expression)
        {
            var parserAction = CompileParserSpan(expression);

            var parser = new PositionParser<T>(parserAction, _delimiter);

            _fieldParsers.Add(parser);
            return this;
        }

        private ReadOnlySpanAction<char, T> CompileParserSpan<TProperty>(
            Expression<Func<T, TProperty>> accessor)
        {
            ParameterExpression model = Expression.Parameter(typeof(T), "model");
            ParameterExpression lexem = Expression.Parameter(typeof(ReadOnlySpan<char>), "lexem");
            ParameterExpression prop = Expression.Parameter(typeof(TProperty), "prop");
            
            var memberExpression = (MemberExpression) accessor.Body;
            var property = (PropertyInfo) memberExpression.Member;
            var setMethod = property.GetSetMethod();

            Expression block;
            if (typeof(TProperty) == typeof(string))
            {
                var toString = typeof(ReadOnlySpan<char>)
                    .GetMethod("ToString");
                
                var lexemAsString = Expression.Call(lexem, toString);

                block = Expression.Call(model, setMethod, lexemAsString);
            }
            else
            {
                block = Expression.Block(
                    new [] { prop },
                    Expression.Call(typeof(TProperty), "TryParse", null, lexem, prop),
                    Expression.Call(model, setMethod, prop));
            }
            
            var parameters = new[] {lexem, model};
            
            var blockCall = Expression.Lambda<ReadOnlySpanAction<char, T>>(
                block, 
                parameters).Compile();

            return blockCall;
        }
        
        private Action<T, string> CompileParser<TProperty>(
            Expression<Func<T, TProperty>> accessor)
        {
            ParameterExpression model = Expression.Parameter(typeof(T), "model");
            ParameterExpression lexem = Expression.Parameter(typeof(string), "lexem");
            ParameterExpression prop = Expression.Parameter(typeof(TProperty), "prop");
            
            var memberExpression = (MemberExpression) accessor.Body;
            var property = (PropertyInfo) memberExpression.Member;
            var setMethod = property.GetSetMethod();
            
            var block = Expression.Block(
                new [] {prop},
                Expression.Call(typeof(TProperty), "TryParse", null, lexem, prop),
                Expression.Call(model, setMethod, prop));

            var parameters = new[] {model, lexem};
            
            var blockCall = Expression.Lambda<Action<T, string>>(
                block, 
                parameters).Compile();

            return blockCall;
        }

        public PositionalSerializer<T> FixedString(string @fixed)
        {
            var parserAction = VerifyFixedString(@fixed);
            var parser = new PositionParser<T>(parserAction, _delimiter);
            _fieldParsers.Add(parser);
            return this;
        }

        private ReadOnlySpanAction<char, T> VerifyFixedString(string @fixed) 
            => (lexem, model) 
                => IsError = !lexem.Equals(@fixed, StringComparison.Ordinal);

        public PositionalSerializer<T> Skip(int count = 1)
        {
            void SkipItem(ReadOnlySpan<char> span, T arg) { }

            for (int i = 0; i < count; i++)
            {
                var parser = new PositionParser<T>(SkipItem, _delimiter);
                _fieldParsers.Add(parser);
            }
            
            return this;
        }

        public T Parse(ReadOnlySpan<char> input)
        {
            int from = 0;
            int to = 0;
            int fieldIndex = 0;
            int length = input.Length;
            
            var currentParser = GetParser(fieldIndex);

            for (int i = 0; i < length; i++)
            {
                if (input[i] != currentParser.Delimiter)
                {
                    continue;
                }

                to = i;
                if (!TryParseField(currentParser, input, from, to, fieldIndex))
                {
                    return _data;
                }

                from = to + 1;
                fieldIndex++;
                currentParser = GetParser(fieldIndex);
            }

            if (to == length - 1) 
                return _data;
            
            to = length;

            if (from != to)
            {
                var parser = GetParser(fieldIndex);
                TryParseField(parser, input, from, to, fieldIndex);
            }
            
            return _data;
        }

        private bool TryParseField(PositionParser<T> parser, ReadOnlySpan<char> input, int from, int to, int fieldIndex)
        {
            var slice = input.Slice(from, to - from);

            parser.ParserAction(slice, _data);

            return !(IsError || fieldIndex == _fieldParsers.Count - 1);
        }

        public PositionalSerializer<T> NextDelimiter(char delimiter)
        {
            _delimiter = delimiter;
            return this;
        }

        public PositionalSerializer<T> Switch<TProperty>(
            (string marker, Expression<Func<T, TProperty>> accessor, TProperty value) switch1,
            (string marker, Expression<Func<T, TProperty>> accessor, TProperty value) switch2)
        {
            ParameterExpression model = Expression.Parameter(typeof(T), "model");
            ParameterExpression lexem = Expression.Parameter(typeof(ReadOnlySpan<char>), "lexem");
           
            var parameters = new[] {lexem, model};

            var block = Expression.Block(
                CompileSwitch(model, lexem, switch1),
                CompileSwitch(model, lexem, switch2)
            );
            
            var blockCall = Expression
                .Lambda<ReadOnlySpanAction<char, T>>(block,parameters);

            var blockCallCompiled = blockCall.Compile();

            var parser = new PositionParser<T>(blockCallCompiled, _delimiter);
            _fieldParsers.Add(parser);
            return this;
        }

        private ConditionalExpression CompileSwitch<TProperty>(
            ParameterExpression model,
            ParameterExpression lexem,
            (string marker, Expression<Func<T, TProperty>> accessor, TProperty value) switchCase)
        {
            var accessorBody = (MemberExpression) switchCase.accessor.Body;
            var propertyToSet = (PropertyInfo) accessorBody.Member;
            var setMethod = propertyToSet.GetSetMethod();

            var marker = Expression.Constant(switchCase.marker);
            var valueToSet = Expression.Constant(switchCase.value);
            var comparisonMethod = Expression.Constant(StringComparison.Ordinal);
            
            var asSpan = typeof(MemoryExtensions)
                .GetMethod(
                    "AsSpan",
                    new [] {typeof(string)});

            var spanEquals = typeof(MemoryExtensions)
                .GetMethod(
                    "Equals",
                    new []
                    {
                        typeof(ReadOnlySpan<char>), 
                        typeof(ReadOnlySpan<char>),
                        typeof(StringComparison)
                    });

            var markerAsSpan = Expression.Call(asSpan, marker);

            var equalityTest = Expression.Call(spanEquals, 
                lexem, 
                markerAsSpan,
                comparisonMethod);
            var block = Expression.Call(model, setMethod, valueToSet);

            var conditionalExpression = Expression.IfThen(equalityTest, block);

            return conditionalExpression;
        }
    }
}