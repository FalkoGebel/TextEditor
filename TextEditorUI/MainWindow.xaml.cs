using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace TextEditorUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string CurrentText;
        private string CurrentFileName;
        private string CurrentFile;
        private string CurrentFontFamily = "Consolas";
        private int CurrentFontSize = 12;
        
        public ViewModel ViewModel { get; set; } = new ViewModel();
        
        public MainWindow()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Window));

            Binding myBinding = new()
            {
                Source = ViewModel,
                Path = new PropertyPath("Title")
            };
            SetBinding(TitleProperty, myBinding);

            DataContext = ViewModel;

            CreateNewFile();
            MenuViewStatusBar.IsChecked = true;

            UpdateEditorFontSetup();

            UpdateStatusBarPosition();
        }

        private int GetEditorLineNumber()
        {
            int output = 1;

            TextPointer caretLineStart = EditorRichTextBox.CaretPosition.GetLineStartPosition(0);
            TextPointer p = EditorRichTextBox.Document.ContentStart.GetLineStartPosition(0);

            while (true)
            {
                if (caretLineStart.CompareTo(p) < 0)
                    break;
                p = p.GetLineStartPosition(1, out int result);
                if (result == 0)
                    break;
                output++;
            }

            return output;
        }

        private int GetEditorColumnNumber()
        {
            return Math.Max(EditorRichTextBox.CaretPosition.GetLineStartPosition(0).GetOffsetToPosition(EditorRichTextBox.CaretPosition) - 1, 0) + 1;
        }
        
        private void UpdateStatusBarPosition()
        {
            if (StatusBar.Visibility != Visibility.Visible)
                return;

            int col = GetEditorColumnNumber();
            int line = GetEditorLineNumber();
            StatusBarPositionLabel.Content = $"{Properties.Resources.STATUS_BAR_POSITION_LINE} {line}{Properties.Resources.STATUS_BAR_POSITION_SEPARATOR} {Properties.Resources.STATUS_BAR_POSITION_COLUMN} {col}";
        }

        private void MenuFileNew_Click(object sender, RoutedEventArgs e)
        {
            CreateNewFile();
        }

        public static void ShowError(string msg)
        {
            _ = MessageBox.Show(msg, Properties.Resources.ERROR_CAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        }


        private static bool ShowConfirm(string msg, string title)
        {
            MessageBoxResult result = MessageBox.Show(msg, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        private void CreateNewFile()
        {
            if (CurrentFile != null)
            {
                MessageBoxResult result = SaveCurrentTextToFile();

                if (result == MessageBoxResult.Cancel)
                    return;
            }

            CurrentText = ((char)13).ToString() + ((char)10).ToString();
            CurrentFile = "";

            // Update text box
            UpdateCurrentTextContent();
            
            // Update window title
            UpdateTitle();

            EditorRichTextBox.Focus();
        }

        private void UpdateCurrentTextContent()
        {
            MemoryStream stream = new(Encoding.Default.GetBytes(CurrentText));
            TextRange textRange = new(EditorRichTextBox.Document.ContentStart, EditorRichTextBox.Document.ContentEnd);
            textRange.Load(stream, DataFormats.Text);
        }
        
        private string GetCurrentTextContent()
        {
            TextRange textRange = new(EditorRichTextBox.Document.ContentStart, EditorRichTextBox.Document.ContentEnd);
            return textRange.Text;
        }

        private void UpdateTitle()
        {
            string title = "";

            CurrentFileName = System.IO.Path.GetFileName(CurrentFile);

            if (CurrentFileName == "")
                title += Properties.Resources.TITLE_UNKNOWN;
            else
                title += CurrentFileName;

            if (GetCurrentTextContent() != CurrentText)
                title += "*";

            ViewModel.Title = title;
        }

        private void UpdateEditorRichTextBoxWidth()
        {
            if (!EditorRichTextBox.IsLoaded)
                return;
            
            string text = new TextRange(EditorRichTextBox.Document.ContentStart, EditorRichTextBox.Document.ContentEnd).Text;
            FormattedText ft = new(text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(EditorRichTextBox.FontFamily, EditorRichTextBox.FontStyle, EditorRichTextBox.FontWeight, EditorRichTextBox.FontStretch), EditorRichTextBox.FontSize, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            EditorRichTextBox.Document.PageWidth = ft.Width + 12;            
            EditorRichTextBox.HorizontalScrollBarVisibility = EditorRichTextBox.ActualWidth < EditorRichTextBox.Document.PageWidth ? ScrollBarVisibility.Visible : ScrollBarVisibility.Hidden;
        }
        
        private void EditorRichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateEditorRichTextBoxWidth();
            UpdateTitle();
        }

        private void MenuFileExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private MessageBoxResult SaveCurrentTextToFile()
        {
            MessageBoxResult result = MessageBoxResult.Yes;

            if (GetCurrentTextContent() != CurrentText)
            {
                string fileName = CurrentFileName == "" ? Properties.Resources.TITLE_UNKNOWN : CurrentFileName;
                result = ShowQuestionYesNoCancel(Properties.Resources.ASK_QUESTION_SAVE_CHANGE.Replace("[FILENAME]", fileName));
                if (result == MessageBoxResult.Yes)
                    ChooseSaveFileType();
            }

            return result;
        }

        private static MessageBoxResult ShowQuestionYesNoCancel(string msg)
        {
            return MessageBox.Show(msg, Properties.Resources.ASK_QUESTION_CAPTION, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        }

        private void MenuViewStatusBar_Checked(object sender, RoutedEventArgs e)
        {
            UpdateStatusBarVisibility();
        }

        private void MenuViewStatusBar_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateStatusBarVisibility();
        }

        private void UpdateStatusBarVisibility()
        {
            StatusBar.Visibility = MenuViewStatusBar.IsChecked ? Visibility.Visible : Visibility.Collapsed;
            UpdateStatusBarPosition();
        }

        private void MenuFileSaveAs_Click(object sender, RoutedEventArgs e)
        {
            FileSaveAs();
        }

        private void FileSaveAs()
        {
            SaveFileDialog dlg = new()
            {
                FileName = Properties.Resources.FILE_SAVE_AS_FILENAME,
                DefaultExt = Properties.Resources.FILE_SAVE_AS_EXTENSION,
                Filter = Properties.Resources.FILE_SAVE_AS_FILTER
            };

            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                if (dlg.FileName != CurrentFile)
                {
                    if (File.Exists(dlg.FileName))
                    {
                        if (!ShowConfirm(Properties.Resources.CONFIRM_SAVE_AS_MSG.Replace("[FILENAME]", dlg.FileName),
                            Properties.Resources.CONFIRM_SAVE_AS_CAPTION))
                        {
                            return;
                        }
                    }
                    CurrentFile = dlg.FileName;
                }
                FileSave();
            }
        }

        private void FileOpen()
        {
            OpenFileDialog dlg = new()
            {
                FileName = Properties.Resources.FILE_OPEN_FILENAME,
                Filter = Properties.Resources.FILE_OPEN_FILTER
            };

            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                try
                {
                    CurrentFile = dlg.FileName;
                    CurrentText = File.ReadAllText(CurrentFile);
                    UpdateCurrentTextContent();
                    UpdateTitle();
                } catch (Exception e)
                {
                    ShowError(e.Message);
                }
            }
        }

        private void FileSave()
        {
            CurrentText = GetCurrentTextContent();
            File.WriteAllText(CurrentFile, CurrentText);
            UpdateTitle();
        }

        private void MenuFileSave_Click(object sender, RoutedEventArgs e)
        {
            ChooseSaveFileType();
        }

        private void ChooseSaveFileType()
        {
            if (CurrentFile == null)
                FileSaveAs();
            else
                FileSave();
        }

        private void MenuFileOpen_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = SaveCurrentTextToFile();

            if (result == MessageBoxResult.Cancel)
                return;

            FileOpen();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = !CloseWindow();
        }

        private bool CloseWindow()
        {
            MessageBoxResult result = SaveCurrentTextToFile();

            if (result == MessageBoxResult.Cancel)
                return false;

            return true;
        }

        private void MenuFormatFontType_Click(object sender, RoutedEventArgs e)
        {
            ShowFontDialog();
        }

        private void ShowFontDialog()
        {
            FontDialog fd = new(CurrentFontSize, CurrentFontFamily);
            fd.ShowDialog();

            if (fd.MessageBoxResult == MessageBoxResult.OK)
            {
                CurrentFontSize = fd.CurrentFontSize;
                CurrentFontFamily = fd.CurrentFontFamily;
                UpdateEditorFontSetup();
            }
        }

        private void UpdateEditorFontSetup()
        {
            EditorRichTextBox.FontSize = CurrentFontSize;
            EditorRichTextBox.FontFamily = new FontFamily(CurrentFontFamily);
        }

        private void EditorRichTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateStatusBarPosition();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateEditorRichTextBoxWidth();
        }
    }

    public class ViewModel : INotifyPropertyChanged
    {
        private string title;
        
        public string Title
        {
            get => title;
            set
            {
                title = "";
                if (value != "")
                    title = value + " - ";
                title += Assembly.GetEntryAssembly().GetName().Name;
                OnTitleChanged(nameof(Title));
            }
        }

        public ViewModel()
        {
            Title = "";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnTitleChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
