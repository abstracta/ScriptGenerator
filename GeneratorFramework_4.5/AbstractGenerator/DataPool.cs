using System.Collections.Generic;

namespace Abstracta.Generators.Framework.AbstractGenerator
{
    internal class DataPool
    {
        private readonly List<string> _columns;

        public DataPool(string name, string fileName, List<string> columns)
        {
            FileName = fileName;
            Name = name;
            _columns = columns;
        }

        public string Name { get; private set; }
        
        public string FileName { get; private set; }

        public List<string> Columns()
        {
            return _columns;
        }

        public string ColumnsJoined(string separator)
        {
            return string.Join(separator, _columns.ToArray());
        }
    }
}