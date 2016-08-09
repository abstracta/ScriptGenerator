using Abstracta.Generators.Framework;
using Fiddler;
using ManyConsole;
using NDesk.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptGeneratorCLI_4._5
{
    public class Command : ConsoleCommand
    {
        private bool _showHelp = true;
        private string _host = null;
        private string _port = "80";
        private string _appName = null;
        private string _outputPath = null;
        private string _gxtestXmlPath = null;
        private bool _isGxApp = false;
        private bool _isBMScript = false;
        private List<string> _fiddlerSessionPaths = new List<string>();

        public Command()
        {
            this.IsCommand("ScriptGeneratorCLI_4._5", "Create JMeter/OpenSTA scripts based on Fiddler sessions.");

            HasRequiredOption("h|host=", "(required) host name where your web app is hosted. Example: https://myapp.com/home, host = myapp.com",
                           v => _host = v);
            HasOption("p|port=",
                                "port number where your web app is listening. Example: https://myapp.com/home, port = 443 \n" +
                                    "This must be an integer. By default the value is 80.",
                              v => _port = v);
            HasOption("a|app=", "relative application name. Example: https://myapp.com/home, app = home", 
                        v =>  _appName = v );

            HasRequiredOption("o|output=", "(required) output path", v => _outputPath = v);
            HasOption("gxt|gxtestFile=", "gxtest xml file path", v => _gxtestXmlPath = v);
            HasOption("gx|isGxApplication", "flag to indicate if the application was generated with GeneXus", v => _isGxApp = v != null);
            HasOption("bm|isBMScript", "flag to indicate if the script will generat for BlazeMeter", v => _isBMScript = v != null);

            HasRequiredOption("f|fiddlerPath=", "(required) fiddler session path", v => _fiddlerSessionPaths.Add(v));
            HasOption("help", "show this message and exit", v => _showHelp = v != null);

            //HasAdditionalArguments(2, "<Argument1> <Argument2>");
        }

        public override int Run(string[] remainingArguments)
        {
            if (!_outputPath.EndsWith("\\") || !_outputPath.EndsWith("/"))
            {
                _outputPath += "\\";
            }

            List<Session[]> sessions = new List<Session[]>();
            foreach (string fiddlerPath in _fiddlerSessionPaths)
            {
                string filePath = PreProcessPath(fiddlerPath);
                sessions.Add(LoadFiddlerSession(filePath));
            }

            System.Xml.XmlDocument gxTest = null;
            if (!string.IsNullOrEmpty(_gxtestXmlPath) && File.Exists(PreProcessPath(_gxtestXmlPath)))
            {
                gxTest = new System.Xml.XmlDocument();
                gxTest.Load(_gxtestXmlPath);
            }

            bool replaceInBodies = false; // Actually, I am not sure how it works this parameter, so I let it by default in false :)
            ScriptGenerator generator = new ScriptGenerator(_outputPath, _outputPath, gxTest, sessions, 
                                                                    _host, _appName, _isGxApp, _isBMScript, replaceInBodies);
            generator.GenerateScripts(GeneratorType.JMeter);
            generator.GenerateScripts(GeneratorType.Testing);

            return 0;
        }

        private Fiddler.Session[] LoadFiddlerSession(string filePath)
        {
            Fiddler.Session[] session = Abstracta.Generators.Framework.ScriptGenerator.GetSessionsFromFile(filePath);
            if (session == null)
            {
                Console.WriteLine("File not found or unknown format for: " + filePath);
            }
            return session;       
        }

        /// <summary>
        /// If it is not an absolute windows path, we append the path to the current directory path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string PreProcessPath(string path)
        {
            string filePath = string.Empty;
            if (!path.Contains(":/")) 
            { 
                filePath = Path.Combine(Environment.CurrentDirectory, path);
            }
            return filePath;
        }
    }
}
