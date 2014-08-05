using System;
using System.Collections.Generic;
using System.IO;

namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses
{
    // tiene un nombre y un contenido
    internal class DataFile
    {
        //para separar un dato de otro en las líneas de un archivo de datos
        //esto es luego útil para la función parse_string de OpenSTA
        internal const string Separator = ",";

        /* PUBLIC GETTERS AND SETTERS */
        internal string Name { get; private set; }

        internal Dictionary<string, DataFileColumn> Columns { get; set; }
        
        /* CONSTRUCTORS */
        internal DataFile(string name)
        {
            Name = name;
            Columns = new Dictionary<string, DataFileColumn>();
        }

        internal DataFile(string name, ICollection<KeyValuePair<string, Variable>> columns, IEnumerable<string> values)
        {
            Name = name;
            Columns = new Dictionary<string, DataFileColumn>();
            
            var columnaValores = new List<string>[columns.Count];
            for (var i = 0; i < columns.Count; i++)
            {
                columnaValores[i] = new List<string>();
            }
            
            foreach (var fila in values)
            {
                var valoresSeparados = fila.Split(Separator.ToCharArray());
                if (valoresSeparados.Length != columns.Count)
                {
                    throw new Exception("Error en datos (distintas columnas que valores en el archivo");
                }

                for (var i = 0; i < columns.Count; i++)
                {
                    columnaValores[i].Add(valoresSeparados[i]);
                }
            }
            
            var n = 0;
            foreach (var col in columns)
	        {
                AddColumn(col.Key, col.Value, columnaValores[n]);
                n++;
	        }
        }

        internal void AddColumn(string fileColmumn, Variable varible, string value)
        {
            if (!Columns.ContainsKey(fileColmumn))
            {
                Columns.Add(fileColmumn, new DataFileColumn(fileColmumn, varible.Name, value));
            }
        }

        internal void AddColumn(string fileColmumn, Variable varible, List<string> values)
        {
            if (!Columns.ContainsKey(fileColmumn))
            {
                Columns.Add(fileColmumn, new DataFileColumn(fileColmumn, varible.Name, values));
            }
        }

        internal void Write(string folder)
        {
            if (!folder.EndsWith("\\") || !folder.EndsWith("/"))
            {
                folder = folder + "\\";
            }

            var filePath = folder + Name + ".fvr";

            if (File.Exists(filePath))
            {
                return;
            }

            var line0 = string.Empty;

            IEnumerator<DataFileColumn> enumerator = Columns.Values.GetEnumerator();
            enumerator.MoveNext();

            var cantValues = enumerator.Current.Values.Count;

            for (var i = 0; i < cantValues; i++)
            {
                foreach (var col in Columns.Values)
                {
                    line0 += col.Values[i] + Separator;
                }

                line0 = line0.Substring(0, line0.Length - Separator.Length) + Environment.NewLine;
            }

            using (var file = new StreamWriter(filePath))
            {
                file.Write(line0);    
            }
        }
    }
}
