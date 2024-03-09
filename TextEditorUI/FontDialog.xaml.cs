using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TextEditorUI
{
    /// <summary>
    /// Interaktionslogik für FontDialog.xaml
    /// </summary>
    public partial class FontDialog : Window
    {
        readonly List<string> FontFamiliesNames = [];
        readonly List<int> FontSizes = [8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72];
        public MessageBoxResult MessageBoxResult { get; set; } = MessageBoxResult.Cancel;
        public int CurrentFontSize { get; set; }
        public string CurrentFontFamily { get; set; }

        public FontDialog()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Window));
            InstalledFontCollection Fonts = new();
            foreach (var f in Fonts.Families)
                FontFamiliesNames.Add(f.Name);
            FontsListBox.ItemsSource = FontFamiliesNames;
            FontSizesListBox.ItemsSource = FontSizes;
        }

        public FontDialog(int size, string fontFamily): this()
        {
            FontSizeTextBox.Text = size.ToString();
            FontTextBox.Text = fontFamily;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckValuesAndSetProperties())
                return;

            Close();
        }

        private bool CheckValuesAndSetProperties()
        {
            if (!Regex.IsMatch(FontSizeTextBox.Text, @"^\d{1,3}$") || !int.TryParse(FontSizeTextBox.Text, out int newSize))
            {
                MainWindow.ShowError(Properties.Resources.FONTDIALOG_ERROR_FONTSIZE_PARSE);
                return false;
            }

            if (newSize < 1 || newSize > 100)
            {
                MainWindow.ShowError(Properties.Resources.FONTDIALOG_ERROR_FONTSIZE_VALUE);
                return false;
            }

            if (!FontFamiliesNames.Contains(FontTextBox.Text))
            {
                MainWindow.ShowError(Properties.Resources.FONTDIALOG_ERROR_FONT);
                return false;
            }

            CurrentFontFamily = FontTextBox.Text;
            CurrentFontSize = newSize;
            MessageBoxResult = MessageBoxResult.OK;

            return true;
        }

        private void FontSizesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FontSizesListBox.SelectedItem != null)
                FontSizeTextBox.Text = FontSizesListBox.SelectedItem.ToString();
        }

        private void FontsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(FontsListBox.SelectedItem != null)
                FontTextBox.Text = FontsListBox.SelectedItem.ToString();
        }
    }
}
