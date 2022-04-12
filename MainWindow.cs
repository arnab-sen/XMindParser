using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Threading;
using System.Windows.Media;
using System.Windows.Controls.Primitives;

/// <summary>
/// This is a parsing tool for converting an XMind file into a series of WireTo lines as per the ALA format.
/// Author: Arnab Sen
/// </summary>
namespace XMindParser
{
    public class MainWindow : Window
    {
        private string defaultPath = "";
        private string VERSION_NUMBER = "1.6";

        ComboBox diagramFilePathDropDown = new ComboBox() { Width = 600, IsEditable = true, ItemsSource = new List<string>() };
        ComboBox appFilePathDropDown = new ComboBox() { Width = 600, IsEditable = true, ItemsSource = new List<string>() };

        private string DiagramPath
        {
            get
            {
                var path = "";
                diagramFilePathDropDown.Dispatcher.Invoke(() => path = diagramFilePathDropDown.Text);
                return path;
            }

            set
            {
                diagramFilePathDropDown.Dispatcher.Invoke(() => diagramFilePathDropDown.Text = value);
            }
        }

        private string AppCodePath
        {
            get
            {
                var path = "";
                appFilePathDropDown.Dispatcher.Invoke(() => path = appFilePathDropDown.Text);
                return path;
            }

            set
            {
                appFilePathDropDown.Dispatcher.Invoke(() => appFilePathDropDown.Text = value);
            }
        }

        private string parsedString = "";
        private TextBox parsedStringDisplay = new TextBox()
        {
            MinHeight = 300,
            Margin = new Thickness(0, 0, 20, 20),
            VerticalAlignment = VerticalAlignment.Top,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
            AcceptsReturn = true
        };

        private Label errorMessageLabel = new Label() { Foreground = Brushes.Red, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 20) };

        private bool overwrite = false;

        private FileSystemWatcher fileWatcher;

        private FileSystemWatcher SetWatcher(string filePath)
        {
            var path = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileName(filePath);

            var watcher = new FileSystemWatcher();
            watcher.Path = path;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Filter = fileName;

            watcher.Changed += (sender, args) => Parse();
            
            return watcher;
        }

        public MainWindow()
        {
            Title = "XMindParser";
            Width = 1280;
            Height = 720;

            #region main menu
            var mainMenu = new Menu() { Background = Brushes.Transparent, Margin = new Thickness(-20, 0, 0, 0) };

            var fileMenu = new MenuItem() { Header = "File" };

            var openProjectMenu = new MenuItem() { Header = "Open project folder" };
            openProjectMenu.Click += (sender, args) => OpenProjectFolder();
            fileMenu.Items.Add(openProjectMenu);

            var helpMenu = new MenuItem() { Header = "Help" };

            var aboutMenu = new MenuItem() { Header = "About" };
            aboutMenu.Click += (sender, args) => MessageBox.Show($"Version: {VERSION_NUMBER}{Environment.NewLine}Author: Arnab Sen", "About");

            mainMenu.Items.Add(fileMenu); 
            mainMenu.Items.Add(helpMenu);
            
            helpMenu.Items.Add(aboutMenu);
            #endregion

            #region diagram file
            var diagramPathHoriz = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };
            diagramFilePathDropDown.Text = "";

            diagramFilePathDropDown.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler((sender, args) => MatchDiagramToCodeFile()));

            diagramFilePathDropDown.SelectionChanged += (sender, args) => MatchDiagramToCodeFile();

            diagramPathHoriz.Children.Add(diagramFilePathDropDown);
            var diagramFileOpenFolderButton = new Button()
            {
                Content = "Open XMind diagram",
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(10, 0, 0, 0),
                Width = 200
            };

            diagramFileOpenFolderButton.Click += (sender, args) =>
            {
                var openFileDialog = new OpenFileDialog() { FileName = defaultPath };
                if (openFileDialog.ShowDialog() == true)
                {
                    DiagramPath = openFileDialog.FileName;
                    diagramFilePathDropDown.Text = DiagramPath;

                    // Setup the fileWatcher to watch only the new diagram file
                    fileWatcher?.Dispose(); // Stop watching the old file
                    fileWatcher = SetWatcher(DiagramPath);
                    fileWatcher.EnableRaisingEvents = true;

                    // Set the default app code file to be the first one that matches the selected diagram file
                    MatchDiagramToCodeFile();
                }
            };
            diagramPathHoriz.Children.Add(diagramFileOpenFolderButton);

            #endregion

            #region app code file
            var appFileHoriz = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };
            appFilePathDropDown.Text = "";
            appFileHoriz.Children.Add(appFilePathDropDown);
            var appFileOpenFolderButton = new Button()
            {
                Content = "Open code file",
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(10, 0, 0, 0),
                Width = 200
            };
            appFileOpenFolderButton.Click += (sender, args) =>
            {
                var openFileDialog = new OpenFileDialog() { };
                if (openFileDialog.ShowDialog() == true)
                {
                    AppCodePath = openFileDialog.FileName;
                    appFilePathDropDown.Text = AppCodePath;
                }
            };

            appFileHoriz.Children.Add(appFileOpenFolderButton);

            #endregion



            var buttonHoriz = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            Button parseButton = new Button();
            parseButton.Content = "Parse";
            parseButton.Height = 23;
            parseButton.Width = 100;
            parseButton.Click += (sender, args) => Parse();
            buttonHoriz.Children.Add(parseButton);
            CheckBox overwriteFileCheckBox = new CheckBox() { Margin = new Thickness(5), Content = "Overwrite Application.cs" };
            overwriteFileCheckBox.Click += (object sender, RoutedEventArgs e) =>
            {
                overwrite = (bool)overwriteFileCheckBox.IsChecked;
            };
            buttonHoriz.Children.Add(overwriteFileCheckBox);
            
            // var colourButtonHoriz = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(20), HorizontalAlignment = HorizontalAlignment.Center };
            // Button colourButton = new Button() { Content = "Add colour to all instance nodes", Height = 23, Width = 200 };
            // var colourPicker = new ColorPicker() { Height = 23, Width = 100, Margin = new Thickness(10, 0, 10, 0) };
            // colourButton.Click += (object sender, RoutedEventArgs e) =>
            // {
            //     selectedInstanceNodeColour = $"#{colourPicker.SelectedColor.ToString().Substring(3)}";
            //     newFilePath = new ZenParser().ApplyLambdaToSheetNodes(diagramFilePathDropDown.Text, "Main",
            //         node => node.Fill =
            //             Regex.IsMatch(node.Content, @"^[<>*]") || Regex.IsMatch(node.Content, @"^Application")
            //                 ? null
            //                 :  selectedInstanceNodeColour);
            //
            // };
            // buttonHoriz.Children.Add(colourPicker);
            // buttonHoriz.Children.Add(colourButton);

            // Button abbreviateButton = new Button() { Content = "Create diagram with only instances", Height = 23, Width = 200, Margin = new Thickness(10, 0, 0, 0) };
            // abbreviateButton.Click += (object sender, RoutedEventArgs e) =>
            // {
            //     new ZenParser().GenerateDiagramFromCode(fileName: diagramFilePathDropDown.Text, fillColour: selectedInstanceNodeColour);
            // };
            // buttonHoriz.Children.Add(abbreviateButton);

            #region arrange UI in the grid
            Grid mainGrid = new Grid() { Margin = new Thickness(20, 0, 0, 0 ) };
            Content = mainGrid;

            var filePathsUI = new StackPanel() { Margin = new Thickness(0, 10, 0, 0) };
            filePathsUI.Children.Add(diagramPathHoriz);
            filePathsUI.Children.Add(appFileHoriz);

            mainGrid.RowDefinitions.Add(new RowDefinition()
            {
                Height = new GridLength(20, GridUnitType.Pixel)
            });
            mainGrid.Children.Add(mainMenu);
            Grid.SetRow(mainMenu, 0);
            
            mainGrid.RowDefinitions.Add(new RowDefinition()
            {
                Height = new GridLength(70, GridUnitType.Pixel)
            });
            mainGrid.Children.Add(filePathsUI);
            Grid.SetRow(filePathsUI, 1);

            mainGrid.RowDefinitions.Add(new RowDefinition()
            {
                Height = new GridLength(1, GridUnitType.Star)
            });
            mainGrid.Children.Add(parsedStringDisplay);
            Grid.SetRow(parsedStringDisplay, 2);

            mainGrid.RowDefinitions.Add(new RowDefinition()
            {
                Height = new GridLength(20, GridUnitType.Auto)
            });
            mainGrid.Children.Add(buttonHoriz);
            Grid.SetRow(buttonHoriz, 3);

            mainGrid.RowDefinitions.Add(new RowDefinition()
            {
                Height = new GridLength(50, GridUnitType.Auto)
            });
            mainGrid.Children.Add(errorMessageLabel);
            Grid.SetRow(errorMessageLabel, 4);
            #endregion
            
        }

        void Parse()
        {
            errorMessageLabel.Dispatcher.Invoke(() => errorMessageLabel.Content =  $"[{DateTime.Now}]:" + Environment.NewLine);

            try
            {
                XMindParser parser = new XMindParser() { Overwrite = overwrite };
                int tryCount = 0;
                int maxTries = 10;

                // Parsing can fail if the diagram file is still in use - on failure, wait momentarily and try again
                while (tryCount < maxTries)
                {
                    try
                    {
                        parsedString = parser.GenerateCodeFromDiagram(DiagramPath, AppCodePath);
                        break;
                    }
                    catch (IOException e)
                    {
                        tryCount++;
                        Thread.Sleep(200);
                    }
                }

                parsedStringDisplay.Dispatcher.Invoke(() => parsedStringDisplay.Text = parsedString);

                errorMessageLabel.Dispatcher.Invoke(() =>
                {
                    errorMessageLabel.Foreground = Brushes.Green;
                    errorMessageLabel.Content += "Parsed diagram successfully!";
                });
            }
            catch (Exception e)
            {
                errorMessageLabel.Dispatcher.Invoke(() =>
                {
                    errorMessageLabel.Foreground = Brushes.Red;
                    errorMessageLabel.Content += "Something went wrong. Please ensure that everything is configured and that your diagram is syntactically correct.\n" +
                        $"Exception: \"{e.GetType()}: {e.Message}\"";
                });
            }
        }

        private void DirectorySearch(string rootFolder, List<string> foundPaths, string filter = "*.*")
        {
            try
            {
                DirectoryInfo root = new DirectoryInfo(rootFolder);
                foundPaths.AddRange(root.GetFiles(filter).Select(s => s.FullName));

                var directories = root.GetDirectories();
                foreach (var directory in directories)
                {
                    DirectorySearch(directory.FullName, foundPaths, filter);
                }
            }
            catch (Exception e)
            {

            }
        }

        private void OpenProjectFolder()
        {
            var openFolderDialog = new System.Windows.Forms.FolderBrowserDialog();

            if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Find XMind diagrams and update the diagram file dropdown with their paths
                if (diagramFilePathDropDown.ItemsSource is List<string>)
                {
                    diagramFilePathDropDown.Dispatcher.Invoke(() =>
                    {
                        (diagramFilePathDropDown.ItemsSource as List<string>).Clear();
                        DirectorySearch(openFolderDialog.SelectedPath, diagramFilePathDropDown.ItemsSource as List<string>, filter: "*.xmind");
                    });
                }

                // Find app code files and update the app code file dropdown with their paths
                if (appFilePathDropDown.ItemsSource is List<string>)
                {
                    appFilePathDropDown.Dispatcher.Invoke(() =>
                    {
                        (appFilePathDropDown.ItemsSource as List<string>).Clear();
                        DirectorySearch(openFolderDialog.SelectedPath, appFilePathDropDown.ItemsSource as List<string>, filter: "*.cs"); 
                    });
                }

                diagramFilePathDropDown.SelectedIndex = 0;
            }
        }

        private void MatchDiagramToCodeFile()
        {
            diagramFilePathDropDown.Dispatcher.Invoke(() =>
            {
                var diagramName = Path.GetFileNameWithoutExtension(diagramFilePathDropDown.SelectedItem as string);
                AppCodePath = (appFilePathDropDown.ItemsSource as IEnumerable<string>)?.FirstOrDefault(s => diagramName == Path.GetFileNameWithoutExtension(s));
            });
        }

        [STAThread]
        public static void Main()
        {
            Application app = new Application();
            app.Run(new MainWindow());
        }
    }
}