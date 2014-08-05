using System;
using System.IO;
using System.Windows;
using Abstracta.Generators.Framework;
using Fiddler;

namespace Abstracta.ScriptGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void GenerateScript(object sender, RoutedEventArgs e)
        {
            try
            {
                var path = ResultFolderName.Text;
                if (!Directory.Exists(path))
                {
                    throw new Exception("Folder doesn't exists: " + path);
                }

                if (!path.EndsWith("\\") || !path.EndsWith("/"))
                {
                    path += "\\";
                }

                var f1 = FiddlerFileName1.Text;
                var f2 = FiddlerFileName2.Text;

                if (!File.Exists(f1))
                {
                    MessageBox.Show("File doesn't exists: " + f1, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!File.Exists(f2))
                {
                    MessageBox.Show("File doesn't exists: " + f2, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var host = Host.Text;
                var appName = AppName.Text;

                var fiddlerSessions1 = Generators.Framework.ScriptGenerator.GetSessionsFromFile(f1);
                if (fiddlerSessions1 == null)
                {
                    MessageBox.Show("File not found or unknown format for Sessions1: " + f1, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var fiddlerSessions2 = Generators.Framework.ScriptGenerator.GetSessionsFromFile(f2);
                if (fiddlerSessions2 == null)
                {
                    MessageBox.Show("File not found or unknown format for Sessions2: " + f2, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var sessions = new Session[2][];
                sessions[0] = fiddlerSessions1;
                sessions[1] = fiddlerSessions2;

                var generator = new Generators.Framework.ScriptGenerator(path, path, null, sessions, host, appName);
                generator.GenerateScripts(GeneratorType.JMeter);
                generator.GenerateScripts(GeneratorType.Testing);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
