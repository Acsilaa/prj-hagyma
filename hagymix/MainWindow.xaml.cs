using hagymix.lib;
using hagymix.utils;
using Microsoft.Win32;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace hagymix
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {

        static Room[,] maze;
        static int[] playerPos = new int[2] { 0, 0 };
        static int[] mazeDimensions;
        static Player player;
        static bool isPlaying = false;

        // -------- Editor state --------
        bool isEditing = false;
        char[,]? editorMap;
        readonly char[] editorTilePalette = new char[]
        {
            '.', '█', '╬', '║', '═', '╝', '╚', '╦', '╗', '╩', '╠', '╣', '╔'
        };
        int editorSelectedTileIndex = 0;
        Grid? editorCellsGrid;
        TextBlock[,]? editorCellTexts;
        TextBlock? editorTileLabel;
        TextBlock? editorStatsLabel;

        readonly struct CellCoord
        {
            public int Row { get; }
            public int Col { get; }
            public CellCoord(int row, int col) { Row = row; Col = col; }
        }

        public MainWindow()
        {
            InitializeComponent();
            
        }

        private static char NormalizeTileForStorage(char c)
        {
            // The assignment uses '.' as empty, so treat spaces as '.'.
            return c == ' ' ? '.' : c;
        }

        private static string TileToDisplayString(char c)
        {
            c = NormalizeTileForStorage(c);
            return c.ToString();
        }

        private static Brush GetTileForeground(char c)
        {
            c = NormalizeTileForStorage(c);
            return c == '█' ? Brushes.Yellow : Brushes.Black;
        }

        private void ResetGrid(Grid grid)
        {
            grid.Children.Clear();
            grid.RowDefinitions.Clear();
            grid.ColumnDefinitions.Clear();
        }

        private void BackToMenu()
        {
            isPlaying = false;
            isEditing = false;

            editorMap = null;
            editorCellsGrid = null;
            editorCellTexts = null;

            Editor.Visibility = Visibility.Hidden;
            MazeGrid.Visibility = Visibility.Hidden;
            MenuGrid.Visibility = Visibility.Visible;
        }

        private void UpdateSelectedTileLabel()
        {
            if (editorTileLabel == null) return;
            char selected = editorTilePalette[editorSelectedTileIndex];
            editorTileLabel.Text = $"Selected tile: {TileToDisplayString(selected)}";
        }

        private void UpdateEditorStats()
        {
            if (editorStatsLabel == null) return;
            if (editorMap == null) { editorStatsLabel.Text = ""; return; }

            int treasures = HagymixSpecialPackage.GetRoomNumber(editorMap);
            int exits = HagymixSpecialPackage.GetSuitableEntrance(editorMap);
            bool invalid = HagymixSpecialPackage.IsInvalidElement(editorMap);

            editorStatsLabel.Text = $"Treasures: {treasures} | Exits: {exits}" + (invalid ? " | Invalid chars" : "");
        }

        private void SetEditorChar(int row, int col, char newChar)
        {
            if (editorMap == null || editorCellTexts == null) return;

            newChar = NormalizeTileForStorage(newChar);

            if (row < 0 || col < 0 || row >= editorMap.GetLength(0) || col >= editorMap.GetLength(1)) return;
            if (editorMap[row, col] == newChar) return;

            editorMap[row, col] = newChar;

            var tb = editorCellTexts[row, col];
            tb.Text = TileToDisplayString(newChar);
            tb.Foreground = GetTileForeground(newChar);

            // Lightweight feedback on each paint.
            UpdateEditorStats();
        }

        private void EditorCell_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!isEditing) return;
            if (sender is FrameworkElement fe && fe.Tag is CellCoord coord)
            {
                char selected = editorTilePalette[editorSelectedTileIndex];
                SetEditorChar(coord.Row, coord.Col, selected);
            }
        }

        private void EditorCell_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!isEditing) return;
            if (sender is FrameworkElement fe && fe.Tag is CellCoord coord)
            {
                // Right click = erase (empty).
                SetEditorChar(coord.Row, coord.Col, '.');
            }
        }

        private void NormalizeEditorMap()
        {
            if (editorMap == null) return;
            for (int i = 0; i < editorMap.GetLength(0); i++)
            {
                for (int j = 0; j < editorMap.GetLength(1); j++)
                {
                    editorMap[i, j] = NormalizeTileForStorage(editorMap[i, j]);
                }
            }
        }

        private void BuildEditorUI()
        {
            if (editorMap == null) return;
            int rows = editorMap.GetLength(0);
            int cols = editorMap.GetLength(1);

            ResetGrid(Editor);

            // Row 0: toolbar, Row 1: editable cells.
            Editor.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
            Editor.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var toolbar = new Grid { Margin = new Thickness(4) };
            toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }); // Load
            toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }); // Save
            toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }); // Back
            toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }); // Next tile
            toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Labels

            var loadBtn = new Button { Content = "Load map", Margin = new Thickness(2) };
            loadBtn.Click += EditorLoadBtn_Click;
            Grid.SetColumn(loadBtn, 0);

            var saveBtn = new Button { Content = "Save map", Margin = new Thickness(2) };
            saveBtn.Click += EditorSaveBtn_Click;
            Grid.SetColumn(saveBtn, 1);

            var backBtn = new Button { Content = "Back", Margin = new Thickness(2) };
            backBtn.Click += (s, e) => BackToMenu();
            Grid.SetColumn(backBtn, 2);

            var tileNextBtn = new Button { Content = "Next tile", Margin = new Thickness(2) };
            tileNextBtn.Click += (s, e) =>
            {
                editorSelectedTileIndex = (editorSelectedTileIndex + 1) % editorTilePalette.Length;
                UpdateSelectedTileLabel();
            };
            Grid.SetColumn(tileNextBtn, 3);

            var rightStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            editorTileLabel = new TextBlock
            {
                Text = $"Selected tile: {TileToDisplayString(editorTilePalette[editorSelectedTileIndex])}",
                VerticalAlignment = VerticalAlignment.Center
            };
            rightStack.Children.Add(editorTileLabel);

            editorStatsLabel = new TextBlock
            {
                Text = "",
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.Gray
            };
            rightStack.Children.Add(editorStatsLabel);

            Grid.SetColumn(rightStack, 4);

            toolbar.Children.Add(loadBtn);
            toolbar.Children.Add(saveBtn);
            toolbar.Children.Add(backBtn);
            toolbar.Children.Add(tileNextBtn);
            toolbar.Children.Add(rightStack);

            Grid.SetRow(toolbar, 0);
            Editor.Children.Add(toolbar);

            editorCellsGrid = new Grid();
            for (int i = 0; i < cols; i++)
                editorCellsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < rows; i++)
                editorCellsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Font size heuristic to keep cells readable.
            int maxDim = Math.Max(rows, cols);
            double fontSize = Math.Clamp(60 - (maxDim * 1.2), 10, 60);

            editorCellTexts = new TextBlock[rows, cols];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    char current = editorMap[r, c];
                    var tb = new TextBlock
                    {
                        Text = TileToDisplayString(current),
                        TextAlignment = TextAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = fontSize,
                        Foreground = GetTileForeground(current),
                        FontFamily = new FontFamily("Consolas"),
                        Margin = new Thickness(0),
                        Padding = new Thickness(0)
                    };

                    var viewbox = new Viewbox
                    {
                        Stretch = Stretch.Uniform,
                        StretchDirection = StretchDirection.Both,
                        Margin = new Thickness(0),
                        Child = tb,
                        Tag = new CellCoord(r, c)
                    };
                    viewbox.MouseLeftButtonDown += EditorCell_MouseLeftButtonDown;
                    viewbox.MouseRightButtonDown += EditorCell_MouseRightButtonDown;

                    editorCellTexts[r, c] = tb;
                    Grid.SetRow(viewbox, r);
                    Grid.SetColumn(viewbox, c);
                    editorCellsGrid.Children.Add(viewbox);
                }
            }

            Grid.SetRow(editorCellsGrid, 1);
            Editor.Children.Add(editorCellsGrid);

            UpdateSelectedTileLabel();
            UpdateEditorStats();
        }

        private void EditorLoadBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!isEditing) return;
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "text file|*.txt",
                Title = "Load map",
                Multiselect = false
            };
            if (ofd.ShowDialog() == false) return;

            using Stream stream = ofd.OpenFile();
            editorMap = StringHelper.FileToChar2D(stream);
            NormalizeEditorMap();

            editorSelectedTileIndex = 0;
            BuildEditorUI();
            UpdateEditorStats();
        }

        private void EditorSaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!isEditing || editorMap == null) return;

            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "text file|*.txt",
                Title = "Save map",
                FileName = "map.txt"
            };
            if (sfd.ShowDialog() == false) return;

            if (HagymixSpecialPackage.IsInvalidElement(editorMap))
            {
                MessageBox.Show("Nem menthető: a térkép tartalmaz meg nem engedett karaktereket.");
                return;
            }

            int treasures = HagymixSpecialPackage.GetRoomNumber(editorMap);
            if (treasures <= 0)
            {
                MessageBox.Show("Nem menthető: a térkép nem tartalmaz kincses termet ('█').");
                return;
            }

            int exits = HagymixSpecialPackage.GetSuitableEntrance(editorMap);
            if (exits <= 0)
            {
                MessageBox.Show("Nem menthető: a térkép nem tartalmaz kijáratot a széleken.");
                return;
            }

            var unavailable = HagymixSpecialPackage.GetUnavailableElements(editorMap);
            if (unavailable.Count > 0)
            {
                MessageBox.Show($"Nem menthető: {unavailable.Count} hibás/sziget járatrész van (pl. {unavailable[0]}).");
                return;
            }

            var sb = new StringBuilder();
            int rows = editorMap.GetLength(0);
            int cols = editorMap.GetLength(1);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                    sb.Append(editorMap[i, j]);
                if (i < rows - 1) sb.AppendLine();
            }

            File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
            MessageBox.Show("Mentés kész.");
        }

        void CreateColumnDefinitions()
        {
            for (int i = 0; i < mazeDimensions[1]; i++)
            {
                MazeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
        }

        void CreateRowDefinitions()
        {
            for (int i = 0; i < mazeDimensions[0]; i++)
            {
                MazeGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }
        }
        void CreateMapVisualization ()
        {
            for (int i = 0; i < mazeDimensions[0]; i++)
            {
                for (int j = 0; j < mazeDimensions[1]; j++)
                {
                    Viewbox cell = new Viewbox { Stretch = Stretch.Uniform, StretchDirection = StretchDirection.Both, Margin = new Thickness(0) };
                    // Defensive: some entries in maze[,] may be null. Use a placeholder when missing.
                    var room = maze[i, j];
                    string text = room != null ? room.roomChar.ToString() : " ";

                    // Compute the foreground using the local 'room' variable to avoid re-indexing maze when it's null.
                    var foreground = (room?.isTreasure == Treasure.Collected) ? Brushes.Yellow : Brushes.Black;

                    var tb = new TextBlock
                    {
                        Text = text,
                        TextAlignment = TextAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 100,
                        Foreground = foreground,
                        Padding = new Thickness(0),
                        Margin = new Thickness(0),
                        FontFamily = new FontFamily("Consolas")
                    };
                    cell.Child = tb;
                    Grid.SetRow(cell, i);
                    Grid.SetColumn(cell, j);
                    MazeGrid.Children.Add(cell);

                }
            }
        }
        private void Main_KeyDown(object sender, KeyEventArgs e)
        {
            if (isEditing)
            {
                if (e.Key == Key.Escape)
                {
                    BackToMenu();
                    e.Handled = true;
                    return;
                }

                // Quick tile cycling.
                if (e.Key == Key.Right || e.Key == Key.D)
                {
                    editorSelectedTileIndex = (editorSelectedTileIndex + 1) % editorTilePalette.Length;
                    UpdateSelectedTileLabel();
                    e.Handled = true;
                    return;
                }
                if (e.Key == Key.Left || e.Key == Key.A)
                {
                    editorSelectedTileIndex = (editorSelectedTileIndex - 1 + editorTilePalette.Length) % editorTilePalette.Length;
                    UpdateSelectedTileLabel();
                    e.Handled = true;
                    return;
                }
            }

            if (!isPlaying) return;

            // Guard player and maze before any use
            if (player == null) return;
            if (maze == null) return;

            int rows = maze.GetLength(0);
            int cols = maze.GetLength(1);
            if (cols == 0 || rows == 0) return;

            switch (e.Key)
            {
                case Key.Up:
                    if (player.IsStarted) player.Move(Direction.Up);
                    break;
                case Key.Right:
                    if (player.IsStarted) player.Move(Direction.Right);
                    else player.ChangeEntrance();
                    break;
                case Key.Down:
                    if (player.IsStarted) player.Move(Direction.Down);
                    break;
                case Key.Left:
                    if (player.IsStarted) player.Move(Direction.Left);
                    break;
                case Key.Enter:
                case Key.Space:
                    if (!player.IsStarted) player.SetEntrance();
                    break;
            }

            // Update all cells safely
            for (int i = 0; i < MazeGrid.Children.Count; i++)
            {
                var vb2 = MazeGrid.Children[i] as Viewbox;
                if (vb2 == null) continue;
                var tb2 = vb2.Child as TextBlock;
                if (tb2 == null) continue;

                tb2.Background = Brushes.White;

                int y = i / cols;
                int x = i % cols;
                if (y >= 0 && y < rows && x >= 0 && x < cols)
                {
                    var cell = maze[y, x];
                    bool isCollected = (cell != null && cell.isTreasure == Treasure.Collected);
                    tb2.Foreground = isCollected ? Brushes.Yellow : Brushes.Black;
                }
                else
                {
                    tb2.Foreground = Brushes.Black;
                }
            }

            // Highlight player cell
            if (player.IsOnMap)
            {
                int playerIndex = player.Y * cols + player.X;
                if (playerIndex >= 0 && playerIndex < MazeGrid.Children.Count)
                {
                    var vb = MazeGrid.Children[playerIndex] as Viewbox;
                    var tb = vb?.Child as TextBlock;
                    if (tb != null) tb.Background = Brushes.Green;
                }
            }
        }

        private void LoadMapClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "text file|*.txt";
            ofd.Title = "már megint itt ülök";
            ofd.Multiselect = false;
            if (ofd.ShowDialog() == false) return;

            Stream stream = ofd.OpenFile();
            
            maze = StringHelper.Char2DToRoomMap(StringHelper.FileToChar2D(stream))!;
            mazeDimensions = [Room.GetMazeLength(maze), Room.GetMazeWidth(maze)];
            CreateColumnDefinitions();
            CreateRowDefinitions();
            CreateMapVisualization();
            player = new Player(maze);
            isPlaying = true;
            MenuGrid.Visibility = Visibility.Hidden;
            MazeGrid.Visibility = Visibility.Visible;
        }

        private void EditorClick(object sender, RoutedEventArgs e)
        {
            isPlaying = false;
            isEditing = true;

            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "text file|*.txt",
                Title = "Load map for editor",
                Multiselect = false
            };

            if (ofd.ShowDialog() == false)
            {
                BackToMenu();
                return;
            }

            using Stream stream = ofd.OpenFile();
            editorMap = StringHelper.FileToChar2D(stream);
            NormalizeEditorMap();
            editorSelectedTileIndex = 0;

            MenuGrid.Visibility = Visibility.Hidden;
            MazeGrid.Visibility = Visibility.Hidden;
            Editor.Visibility = Visibility.Visible;

            BuildEditorUI();
        }

        private void ToggleLangClick(object sender, RoutedEventArgs e)
        {

        }
    }
}