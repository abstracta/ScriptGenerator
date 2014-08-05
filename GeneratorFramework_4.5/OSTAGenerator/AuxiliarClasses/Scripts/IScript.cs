namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.Scripts
{
    internal interface IScript
    {
        void Save(string folder);

        string Name { get; set; }
    }
}
