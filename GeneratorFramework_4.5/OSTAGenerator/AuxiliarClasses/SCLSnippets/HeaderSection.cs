using System;
using System.Collections.Generic;

namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.SCLSnippets
{
    internal class HeaderSection : ISCLSections
    {
// NP - 21/01/2014
// Cambio la lista que almacen las variables por un diccionario para optimizar las búsquedas, que se realizaban en cada función
        private readonly IDictionary<String, VariableDeclaration> _variables = new Dictionary<String, VariableDeclaration>();
        private readonly IList<ConstantDeclarationSection> _constants = new List<ConstantDeclarationSection>();

        public HeaderSection(){ 
            //esto se puede poner en el global
            _variables.Add("USER_AGENT", new VariableDeclaration(new Variable("USER_AGENT", "CHARACTER*512", VariablesScopes.Local)));
            _variables.Add("MESSAGE", new VariableDeclaration(new Variable("MESSAGE", "CHARACTER*512", VariablesScopes.Local)));
            _variables.Add("USE_PAGE_TIMERS", new VariableDeclaration(new Variable("USE_PAGE_TIMERS", "Integer", VariablesScopes.Local)));
        }

        public void AddVariable(Variable var)
        {
            //si no existe la agrego
            if (_variables.ContainsKey(var.Name))
            {
                // NP 02/09/2013
                // se agrega el if del scope para arreglar el bug de que si el DataPool se llama igual que el TC no te agrega la definición de la variable del archivo
                // <strike> igual habría que cambiar la estructura que guarda las variables por un diccionario </strike> DONE
                if (_variables[var.Name].Variable.Scope == var.Scope)
                {
                    return;
                }
                
                if (_variables.ContainsKey("DF_" + var.Name))
                {
                    return;
                }

                var.Name = "DF_" + var.Name;
            }

            _variables.Add(var.Name, new VariableDeclaration(var));
        }

        public Variable GetVariable(string name)
        {
            return _variables.ContainsKey(name) ? _variables[name].Variable : null;
        }

        public bool ExistsVariable(string name)
        {
            return _variables.ContainsKey(name);
        }

        #region ISCLSections Members

        public string WriteCode()
        {
            var result =
                                        "Definitions \n" +
                                        "\t !Standard defines\n" +
                                        "\t Include \t\t \"RESPONSE_CODES.INC\"\n" +
                                        "\t Include \t\t \"GLOBAL_VARIABLES.INC\"\n" +
                                        "\t Include \t\t \"FunctionsVariables.INC\"\n\n" 
                                                   ;

            foreach (var cons in _constants)
            {
                result += cons.WriteCode();
            }

            //agrupar todos los timers
            var timers = string.Empty;
            var characters = string.Empty;
            var integers = string.Empty;
            var others = string.Empty;
            foreach (var pair in _variables)
            {
                var var = pair.Value;
                var aux = var.WriteCode();
                if (var.Variable.Type.ToLower().StartsWith("character"))
                {
                    characters += aux;
                }
                else if (var.Variable.Type.ToLower().StartsWith("timer"))
                {
                    timers += aux;
                }
                else if (var.Variable.Type.ToLower().StartsWith("integer"))
                {
                    integers += aux;
                }
                else
                {
                    others += aux;
                }
            }

            result += timers;
            result += characters;
            result += integers;
            result += others;
            return result;
        }
        #endregion

        internal void AddConstant(ConstantDeclarationSection constantDeclarationSection)
        {
            _constants.Add(constantDeclarationSection);
        }
    }
}
