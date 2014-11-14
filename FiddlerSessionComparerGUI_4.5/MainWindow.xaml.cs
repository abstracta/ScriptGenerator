using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Abstracta.FiddlerSessionComparer;
using Fiddler;

namespace Abstracta.FiddlerSessionComparerGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private string _ruta1;
        private string _ruta2;
        private string _extenssions;
        private Session[] _sessions1;
        private Session[] _sessions2;
        private Session _session1;
        private Session _session2;

        private readonly FiddlerSessionComparer.FiddlerSessionComparer _fsc;

        public MainWindow()
        {
            InitializeComponent();
            Lista1.SelectionMode = SelectionMode.Single;
            Lista2.SelectionMode = SelectionMode.Single;

            _fsc = new FiddlerSessionComparer.FiddlerSessionComparer(true, true);

            // for testing porposes
            /*
            _fsc.Load(_ruta1, _ruta2, null, out _sessions1, out _sessions2);
            Lista1.ItemsSource = _sessions1.ToList().Select(s => s.fullUrl);
            Lista2.ItemsSource = _sessions2.ToList().Select(s => s.fullUrl);

            Lista1.SelectedIndex = Lista2.SelectedIndex = 7;
            // */
        }

        private void Comparar_Click(object sender, RoutedEventArgs e)
        {
            var results = _fsc.CompareSimple(_session1.id, _session2.id, ComparerResultType.ShowAll);

            var table = new DataTable();
            table.Columns.Add("Parametro");
            table.Columns.Add("Valor saz 1");
            table.Columns.Add("Valor saz 2");
            table.Columns.Add("Iguales?");

            foreach (var result in results)
            {
                var row = table.NewRow();
                row.ItemArray = new object[] {result.Key, result.Value1, result.Value2, result.AreEqual};
                
                table.Rows.Add(row);
            }

            Grilla.ItemsSource = table.DefaultView;
            Grilla.AutoGenerateColumns = true;
        }

        private void Ruta1_TextChanged(object sender, TextChangedEventArgs e)
        {
            _ruta1 = ((TextBox) sender).Text;
        }

        private void Ruta2_TextChanged(object sender, TextChangedEventArgs e)
        {
            _ruta2 = ((TextBox)sender).Text;
        }

        private void Cargar_Click(object sender, RoutedEventArgs e)
        {
            var extenssionsList = _extenssions != "" ? _extenssions.Split(',') : null;

            try
            {
                _sessions1 = FiddlerSessionComparer.FiddlerSessionComparer.GetSessionsFromFile(_ruta1);
                _sessions2 = FiddlerSessionComparer.FiddlerSessionComparer.GetSessionsFromFile(_ruta2);

                _fsc.Load(_sessions1, _sessions2, extenssionsList);

                Lista1.ItemsSource = _sessions1.ToList().Select(s => s.fullUrl);
                Lista2.ItemsSource = _sessions2.ToList().Select(s => s.fullUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Extensiones_TextChanged(object sender, TextChangedEventArgs e)
        {
            _extenssions = ((TextBox)sender).Text;
        }

        private void Lista1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _session1 = _sessions1[Lista1.SelectedIndex];
        }

        private void Lista2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _session2 = _sessions2[Lista2.SelectedIndex];
        }
    }
}
