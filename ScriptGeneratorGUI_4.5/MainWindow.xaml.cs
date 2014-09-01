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

            const string path = @"D:\Abstracta\Desarrollo\GitHubAbstractaTools\ScriptGenerator\trunk\Examples\Unifrutti\";

            Host.Text = "200.111.176.10:8080";
            AppName.Text = "performance";
            FiddlerFileName1.Text = path + "BodyGrande1.saz";
            FiddlerFileName2.Text = path + "BodyGrande2.saz";

            // FiddlerFileName1.Text = @"D:\Abstracta\GDrive\Abstracta - PROYECTOS\RUNNING\GXC\2014 - 08 - Perf - Unifrutti\JMeterScripts\4- RecepcionGeneralOtraFruta\RecepcionFrutaGranel1.saz";
            // FiddlerFileName2.Text = @"D:\Abstracta\GDrive\Abstracta - PROYECTOS\RUNNING\GXC\2014 - 08 - Perf - Unifrutti\JMeterScripts\4- RecepcionGeneralOtraFruta\RecepcionFrutaGranel2.saz";

            ResultFolderName.Text = @"D:\Abstracta\GDrive\Abstracta - PROYECTOS\RUNNING\GXC\2014 - 08 - Perf - Unifrutti\JMeterScripts\4- RecepcionGeneralOtraFruta";
        }

        private void GenerateScript(object sender, RoutedEventArgs e)
        {
            var host = Host.Text;
            var appName = AppName.Text;

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

                var sessions = (string.IsNullOrEmpty(f2))? new Session[1][] : new Session[2][];

                if (!File.Exists(f1))
                {
                    MessageBox.Show("File doesn't exists: " + f1, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                sessions[0] = Generators.Framework.ScriptGenerator.GetSessionsFromFile(f1);
                if (sessions[0] == null)
                {
                    MessageBox.Show("File not found or unknown format for Sessions1: " + f1, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!string.IsNullOrEmpty(f2))
                {
                    if (!File.Exists(f2))
                    {
                        MessageBox.Show("File doesn't exists: " + f2, "Error", MessageBoxButton.OK,
                                        MessageBoxImage.Error);
                        return;
                    }

                    sessions[1] = Generators.Framework.ScriptGenerator.GetSessionsFromFile(f2);
                    if (sessions[1] == null)
                    {
                        MessageBox.Show("File not found or unknown format for Sessions2: " + f2, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                var generator = new Generators.Framework.ScriptGenerator(path, path, null, sessions, host, appName);
                generator.GenerateScripts(GeneratorType.JMeter);
                generator.GenerateScripts(GeneratorType.Testing);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            MessageBox.Show("Finished", "Notification", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
