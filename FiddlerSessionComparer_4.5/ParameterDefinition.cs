using System;
using Newtonsoft.Json.Linq;

namespace Abstracta.FiddlerSessionComparer
{
    public enum ParameterContext
    {
        Default,
        AloneValue,
        FirstComaSeparatedValue,
        ComaSeparatedValue, 
        LastComaSeparatedValue,
        KeyEqualValue,

        XMLAttribute,
        XMLValue,
        
        JSonNumberValue,
        JSonStringValue,
        JSonDateValue,
    }

    public class ParameterDefinition
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public ParameterContext Context { get; set; }

        public ParameterDefinition(string key, string value, ParameterContext pContext)
        {
            Key = key;
            Value = value;
            Context = pContext;
        }

        protected bool Equals(ParameterDefinition other)
        {
            return string.Equals(Key, other.Key) && string.Equals(Value, other.Value) && Context == other.Context;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Key != null ? Key.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Value != null ? Value.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (int) Context;

                return hashCode;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            return obj.GetType() == GetType() && Equals((ParameterDefinition) obj);
        }

        public override string ToString()
        {
            return Context + ": " + Key + ":" + Value;
        }

        public static ParameterContext GetContextFromJPropertyType(JTokenType type)
        {
            switch (type)
            {
                case JTokenType.Integer:
                case JTokenType.Float:
                case JTokenType.Bytes:
                case JTokenType.Guid:
                case JTokenType.TimeSpan:
                case JTokenType.Boolean:
                    return ParameterContext.JSonNumberValue;

                case JTokenType.String:
                case JTokenType.Null:
                    return ParameterContext.JSonStringValue;

                case JTokenType.Date:
                    return ParameterContext.JSonDateValue;

                default:
                    return ParameterContext.Default;
            }
        }

        public static ParameterContext GetContextFromStringValue(object value)
        {
            if (value is string)
            {
                return ParameterContext.JSonStringValue;
            }

            if (value is int || value is long || value is float || value is double)
            {
                return ParameterContext.JSonNumberValue;
            }

            if (value is DateTime || value is TimeSpan)
            {
                return ParameterContext.JSonDateValue;
            }

            return ParameterContext.JSonStringValue;
        }

        public static ParameterContext GetContextFromStringValue(string stringValue)
        {
            var result = ParameterContext.JSonStringValue;

            int intValue;
            var parsed = int.TryParse(stringValue, out intValue);
            if (parsed)
            {
                result = ParameterContext.JSonNumberValue;
            }
            else
            {
                double doubleValue;
                parsed = double.TryParse(stringValue, out doubleValue);
                if (parsed)
                {
                    result = ParameterContext.JSonNumberValue;
                }
                else
                {
                    DateTime dateTimeValue;
                    parsed = DateTime.TryParse(stringValue, out dateTimeValue);
                    if (parsed)
                    {
                        result = ParameterContext.JSonDateValue;
                    }
                }
            }

            return result;
        }
    }
}