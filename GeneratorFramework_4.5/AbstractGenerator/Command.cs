using System;
using System.Collections.Generic;

namespace Abstracta.Generators.Framework.AbstractGenerator
{
    internal class Command
    {
        internal Command(string name, string type, string desc, List<int> requestIds, Dictionary<ParametersType, string> parameters)
        {
            Name = name;
            Type = type;
            Desc = desc;
            RequestIds = requestIds;
            Parameters = parameters;
        }

        internal string Name { get; private set; }
        internal string Type { get; private set; }
        internal string Desc { get; private set; }
        internal List<int> RequestIds { get; private set; }
        internal Dictionary<ParametersType, string> Parameters { get; private set; }

        internal static ParametersType GetParamFromName(string parameterName)
        {
            switch (parameterName.Trim())
            {
                case "Stop Execution":
                    return ParametersType.StopExecution;
                case "Text":
                    return ParametersType.TextToValidate;
                case "Negate Validation":
                    return ParametersType.NegateValidation;
                case "Error Description":
                    return ParametersType.ErrorDescription;
                case "Url":
                    return ParametersType.Url;
                case "Element":
                    return ParametersType.HTMLElement;
                case "Table":
                    return ParametersType.Table;
                case "Link":
                    return ParametersType.Link;
                case "Input":
                    return ParametersType.Input;
                case "Value":
                    return ParametersType.Value;
                case "Parameters":
                    return ParametersType.Parameters;
                case "Target DataPool":
                    return ParametersType.TargetDataPool;
                case "Combo":
                    return ParametersType.Combo;
                case "User":
                    return ParametersType.User;
                case "Password":
                    return ParametersType.Password;
                case "Object Name":
                    return ParametersType.ObjectName;
                case "Aditional Parameters":
                    return ParametersType.AditionalParameters;
                case "Menu":
                    return ParametersType.Menu;
                case "Variable":
                    return ParametersType.Variable;
                default:
                    throw new Exception("ParameterName unknown: " + parameterName);
            }
        }
    }
}