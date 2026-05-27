using hagymix.lib;
using hagymix.utils;
using Microsoft.Win32;
using System.IO;
using System.Text.Json;
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
        string? loadedMapPath;
        bool fogOfWarEnabled = false;
        bool[,]? visitedCells;
        bool isHungarian = true;

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

        readonly Dictionary<string, string> hu = new()
        {
            ["menu"] = "Menü",
            ["loadMap"] = "Pálya betöltése",
            ["editor"] = "Szerkesztő",
            ["fog"] = "Fedett térkép (csak bejárt részek látszanak)",
            ["save"] = "Mentés",
            ["reload"] = "Visszatöltés",
            ["back"] = "Menü",
            ["lang"] = "HU",
            ["title"] = "Labirintus",
            ["gameInfo"] = "Kincsek: {0}/{1} | Irányok: {2}",
            ["noMapToSave"] = "Nincs aktív játék mentéshez.",
            ["savedOk"] = "Mentés kész: {0}",
            ["loadSaveQuestion"] = "Találtam korábbi mentést. Visszatöltsem?",
            ["exitConfirm"] = "Biztosan ki szeretnél lépni a labirintusból?",
            ["exitEarlyConfirm"] = "Még nincs meg minden kincs. Biztosan kilépsz?",
            ["gameWon"] = "Siker! Minden kincset megtaláltál és kijutottál.",
            ["gameEndedEarly"] = "Kiléptél, de nem találtál meg minden kincset.",
            ["saveMissing"] = "A mentési fájl nem található.",
            ["loadedSaveOk"] = "Mentés visszatöltve.",
            ["editorLoad"] = "Pálya betöltése",
            ["editorSave"] = "Pálya mentése",
            ["nextTile"] = "Következő elem",
            ["prevTile"] = "Előző elem",
            ["selectedTile"] = "Kiválasztott elem: {0}",
            ["editorHelp"] = "Bal kattintás: rajzolás | Jobb kattintás: törlés",
            ["editorStats"] = "Kincsek: {0} | Kijáratok: {1}{2}",
            ["invalidChars"] = " | Érvénytelen karakter",
        };

        readonly Dictionary<string, string> en = new()
        {
            ["menu"] = "Menu",
            ["loadMap"] = "Load map",
            ["editor"] = "Editor",
            ["fog"] = "Fog-of-war mode (show only visited areas)",
            ["save"] = "Save",
            ["reload"] = "Load save",
            ["back"] = "Menu",
            ["lang"] = "EN",
            ["title"] = "Labyrinth",
            ["gameInfo"] = "Treasures: {0}/{1} | Directions: {2}",
            ["noMapToSave"] = "No active game to save.",
            ["savedOk"] = "Saved: {0}",
            ["loadSaveQuestion"] = "A previous save was found. Load it now?",
            ["exitConfirm"] = "Are you sure you want to leave the labyrinth?",
            ["exitEarlyConfirm"] = "Not all treasures are collected yet. Exit anyway?",
            ["gameWon"] = "Success! You collected all treasures and escaped.",
            ["gameEndedEarly"] = "You escaped before collecting all treasures.",
            ["saveMissing"] = "Save file not found.",
            ["loadedSaveOk"] = "Save loaded.",
            ["editorLoad"] = "Load map",
            ["editorSave"] = "Save map",
            ["nextTile"] = "Next tile",
            ["prevTile"] = "Previous tile",
            ["selectedTile"] = "Selected tile: {0}",
            ["editorHelp"] = "Left click: paint | Right click: erase",
            ["editorStats"] = "Treasures: {0} | Exits: {1}{2}",
            ["invalidChars"] = " | Invalid character",
        };

        readonly struct CellCoord
        {
            public int Row { get; }
            public int Col { get; }
            public CellCoord(int row, int col) { Row = row; Col = col; }
        }

        class SaveData
        {
            public bool FogMode { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public bool IsStarted { get; set; }
            public bool IsOnMap { get; set; }
            public List<string> CollectedTreasures { get; set; } = new();
            public List<string> Visited { get; set; } = new();
        }

        public MainWindow()
        {
            InitializeComponent();
            ApplyLanguage();
        }

        private string T(string key) => (isHungarian ? hu : en).TryGetValue(key, out var value) ? value : key;

        private void ApplyLanguage()
        {
            Title = T("title");
            MenuTitle.Content = T("menu");
            LoadMapButton.Content = T("loadMap");
            EditorButton.Content = T("editor");
            FogModeCheckbox.Content = T("fog");
            SaveGameButton.Content = T("save");
            LoadSaveButton.Content = T("reload");
            BackToMenuButton.Content = T("back");
            LangToggleBTN.Content = T("lang");
            UpdateSelectedTileLabel();
            UpdateEditorStats();
            UpdateGameInfo();
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
            GameInfoText.Text = "";
        }

        private void UpdateSelectedTileLabel()
        {
            if (editorTileLabel == null) return;
            char selected = editorTilePalette[editorSelectedTileIndex];
            editorTileLabel.Text = string.Format(T("selectedTile"), TileToDisplayString(selected));
        }

        private void UpdateEditorStats()
        {
            if (editorStatsLabel == null) return;
            if (editorMap == null) { editorStatsLabel.Text = ""; return; }

            int treasures = HagymixSpecialPackage.GetRoomNumber(editorMap);
            int exits = HagymixSpecialPackage.GetSuitableEntrance(editorMap);
            bool invalid = HagymixSpecialPackage.IsInvalidElement(editorMap);
            editorStatsLabel.Text = string.Format(T("editorStats"), treasures, exits, invalid ? T("invalidChars") : "");
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
            toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }); // Prev tile
            toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }); // Next tile
            toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Labels

            var loadBtn = new Button { Content = T("editorLoad"), Margin = new Thickness(2) };
            loadBtn.Click += EditorLoadBtn_Click;
            Grid.SetColumn(loadBtn, 0);

            var saveBtn = new Button { Content = T("editorSave"), Margin = new Thickness(2) };
            saveBtn.Click += EditorSaveBtn_Click;
            Grid.SetColumn(saveBtn, 1);

            var backBtn = new Button { Content = T("back"), Margin = new Thickness(2) };
            backBtn.Click += (s, e) => BackToMenu();
            Grid.SetColumn(backBtn, 2);

            var tilePrevBtn = new Button { Content = T("prevTile"), Margin = new Thickness(2) };
            tilePrevBtn.Click += (s, e) =>
            {
                editorSelectedTileIndex = (editorSelectedTileIndex - 1 + editorTilePalette.Length) % editorTilePalette.Length;
                UpdateSelectedTileLabel();
            };
            Grid.SetColumn(tilePrevBtn, 3);

            var tileNextBtn = new Button { Content = T("nextTile"), Margin = new Thickness(2) };
            tileNextBtn.Click += (s, e) =>
            {
                editorSelectedTileIndex = (editorSelectedTileIndex + 1) % editorTilePalette.Length;
                UpdateSelectedTileLabel();
            };
            Grid.SetColumn(tileNextBtn, 4);

            var rightStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            editorTileLabel = new TextBlock
            {
                Text = string.Format(T("selectedTile"), TileToDisplayString(editorTilePalette[editorSelectedTileIndex])),
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

            rightStack.Children.Add(new TextBlock
            {
                Text = T("editorHelp"),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.DarkSlateGray
            });

            Grid.SetColumn(rightStack, 5);

            toolbar.Children.Add(loadBtn);
            toolbar.Children.Add(saveBtn);
            toolbar.Children.Add(backBtn);
            toolbar.Children.Add(tilePrevBtn);
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
            MazeGrid.Children.Clear();
            MazeGrid.RowDefinitions.Clear();
            MazeGrid.ColumnDefinitions.Clear();
            CreateColumnDefinitions();
            CreateRowDefinitions();

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

        private string GetDirectionsText()
        {
            if (player == null || maze == null || !player.IsOnMap || maze[player.Y, player.X] == null) return "-";
            var room = maze[player.Y, player.X]!;
            var dirs = new List<string>();
            if (room.ways[0]) dirs.Add(isHungarian ? "W/fel" : "W/up");
            if (room.ways[1]) dirs.Add(isHungarian ? "D/jobb" : "D/right");
            if (room.ways[2]) dirs.Add(isHungarian ? "S/le" : "S/down");
            if (room.ways[3]) dirs.Add(isHungarian ? "A/bal" : "A/left");
            return dirs.Count == 0 ? "-" : string.Join(", ", dirs);
        }

        private void UpdateGameInfo()
        {
            if (!isPlaying || player == null)
            {
                GameInfoText.Text = "";
                return;
            }

            GameInfoText.Text = string.Format(T("gameInfo"), player.TreasureCount, player.TotalTreasureCount, GetDirectionsText());
        }

        private void MarkVisitedCurrent()
        {
            if (!fogOfWarEnabled || visitedCells == null || player == null || !player.IsOnMap) return;
            if (player.Y >= 0 && player.X >= 0 && player.Y < visitedCells.GetLength(0) && player.X < visitedCells.GetLength(1))
            {
                visitedCells[player.Y, player.X] = true;
            }
        }

        private void RefreshMazeVisualState()
        {
            if (maze == null || player == null) return;
            int rows = maze.GetLength(0);
            int cols = maze.GetLength(1);

            for (int i = 0; i < MazeGrid.Children.Count; i++)
            {
                var vb2 = MazeGrid.Children[i] as Viewbox;
                if (vb2 == null) continue;
                var tb2 = vb2.Child as TextBlock;
                if (tb2 == null) continue;

                int y = i / cols;
                int x = i % cols;
                if (y < 0 || y >= rows || x < 0 || x >= cols) continue;

                bool visible = !fogOfWarEnabled || visitedCells == null || visitedCells[y, x];
                var room = maze[y, x];

                if (!visible)
                {
                    tb2.Text = " ";
                    tb2.Foreground = Brushes.Black;
                    tb2.Background = Brushes.WhiteSmoke;
                    continue;
                }

                tb2.Text = room?.roomChar ?? " ";
                bool isCollected = (room != null && room.isTreasure == Treasure.Collected);
                tb2.Foreground = isCollected ? Brushes.Yellow : Brushes.Black;
                tb2.Background = Brushes.White;
            }

            if (player.IsOnMap)
            {
                int playerIndex = player.Y * cols + player.X;
                if (playerIndex >= 0 && playerIndex < MazeGrid.Children.Count)
                {
                    var vb = MazeGrid.Children[playerIndex] as Viewbox;
                    var tb = vb?.Child as TextBlock;
                    if (tb != null) tb.Background = Brushes.LightGreen;
                }
            }

            UpdateGameInfo();
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

            if (maze.GetLength(1) == 0 || maze.GetLength(0) == 0) return;

            Direction? direction = e.Key switch
            {
                Key.Up or Key.W => Direction.Up,
                Key.Right or Key.D => Direction.Right,
                Key.Down or Key.S => Direction.Down,
                Key.Left or Key.A => Direction.Left,
                _ => null
            };

            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                if (!player.IsStarted)
                {
                    player.SetEntrance();
                    MarkVisitedCurrent();
                }
                RefreshMazeVisualState();
                return;
            }

            if (e.Key == Key.F5)
            {
                SaveGameState();
                return;
            }

            if (e.Key == Key.F9)
            {
                LoadGameStateFromCurrentMap();
                return;
            }

            if (direction == null) return;
            if (!player.IsStarted)
            {
                if (direction == Direction.Right) player.ChangeEntrance();
                RefreshMazeVisualState();
                return;
            }

            if (player.CanExit(direction.Value))
            {
                bool allDone = player.AllTreasuresCollected;
                string msg = allDone ? T("exitConfirm") : T("exitEarlyConfirm");
                if (MessageBox.Show(msg, T("title"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    RefreshMazeVisualState();
                    return;
                }
            }

            MoveResult moveResult = player.Move(direction.Value);
            if (moveResult == MoveResult.Exited)
            {
                isPlaying = false;
                MessageBox.Show(player.AllTreasuresCollected ? T("gameWon") : T("gameEndedEarly"));
                BackToMenu();
                return;
            }

            MarkVisitedCurrent();
            RefreshMazeVisualState();
        }

        private void LoadMapClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "text file|*.txt";
            ofd.Title = T("loadMap");
            ofd.Multiselect = false;
            if (ofd.ShowDialog() == false) return;

            loadedMapPath = ofd.FileName;
            fogOfWarEnabled = FogModeCheckbox.IsChecked == true;

            using Stream stream = ofd.OpenFile();
            maze = StringHelper.Char2DToRoomMap(StringHelper.FileToChar2D(stream))!;
            mazeDimensions = [Room.GetMazeLength(maze), Room.GetMazeWidth(maze)];
            CreateMapVisualization();
            player = new Player(maze);
            visitedCells = new bool[mazeDimensions[0], mazeDimensions[1]];
            MarkVisitedCurrent();
            isPlaying = true;
            MenuGrid.Visibility = Visibility.Hidden;
            MazeGrid.Visibility = Visibility.Visible;
            Editor.Visibility = Visibility.Hidden;
            RefreshMazeVisualState();

            if (File.Exists(GetSavePath()))
            {
                if (MessageBox.Show(T("loadSaveQuestion"), T("title"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    LoadGameStateFromCurrentMap();
                }
            }
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
            ApplyLanguage();
        }

        private void ToggleLangClick(object sender, RoutedEventArgs e)
        {
            isHungarian = !isHungarian;
            ApplyLanguage();
        }

        private string GetSavePath()
        {
            if (string.IsNullOrWhiteSpace(loadedMapPath)) return "";
            return System.IO.Path.ChangeExtension(loadedMapPath, ".sav");
        }

        private void SaveGameState()
        {
            if (!isPlaying || player == null || maze == null || string.IsNullOrWhiteSpace(loadedMapPath))
            {
                MessageBox.Show(T("noMapToSave"));
                return;
            }

            var save = new SaveData
            {
                FogMode = fogOfWarEnabled,
                X = player.X,
                Y = player.Y,
                IsStarted = player.IsStarted,
                IsOnMap = player.IsOnMap
            };

            for (int i = 0; i < maze.GetLength(0); i++)
            {
                for (int j = 0; j < maze.GetLength(1); j++)
                {
                    if (maze[i, j] != null && maze[i, j]!.isTreasure == Treasure.Collected)
                    {
                        save.CollectedTreasures.Add($"{i}:{j}");
                    }
                    if (visitedCells != null && visitedCells[i, j])
                    {
                        save.Visited.Add($"{i}:{j}");
                    }
                }
            }

            string saveJson = JsonSerializer.Serialize(save);
            string savePath = GetSavePath();
            File.WriteAllText(savePath, saveJson, Encoding.UTF8);
            MessageBox.Show(string.Format(T("savedOk"), savePath));
        }

        private void LoadGameStateFromCurrentMap()
        {
            if (!isPlaying || player == null || maze == null || string.IsNullOrWhiteSpace(loadedMapPath)) return;

            string savePath = GetSavePath();
            if (!File.Exists(savePath))
            {
                MessageBox.Show(T("saveMissing"));
                return;
            }

            SaveData? save = JsonSerializer.Deserialize<SaveData>(File.ReadAllText(savePath, Encoding.UTF8));
            if (save == null) return;

            fogOfWarEnabled = save.FogMode;
            FogModeCheckbox.IsChecked = fogOfWarEnabled;

            visitedCells = new bool[maze.GetLength(0), maze.GetLength(1)];
            foreach (string pos in save.Visited)
            {
                string[] parts = pos.Split(':');
                if (parts.Length != 2) continue;
                if (int.TryParse(parts[0], out int row) && int.TryParse(parts[1], out int col))
                {
                    if (row >= 0 && col >= 0 && row < visitedCells.GetLength(0) && col < visitedCells.GetLength(1))
                    {
                        visitedCells[row, col] = true;
                    }
                }
            }

            var collected = new List<(int row, int col)>();
            foreach (string pos in save.CollectedTreasures)
            {
                string[] parts = pos.Split(':');
                if (parts.Length != 2) continue;
                if (int.TryParse(parts[0], out int row) && int.TryParse(parts[1], out int col))
                {
                    collected.Add((row, col));
                }
            }

            player.RestoreState(save.X, save.Y, save.IsStarted, save.IsOnMap, collected);
            MarkVisitedCurrent();
            RefreshMazeVisualState();
            MessageBox.Show(T("loadedSaveOk"));
        }

        private void SaveGameClick(object sender, RoutedEventArgs e)
        {
            SaveGameState();
        }

        private void LoadGameStateClick(object sender, RoutedEventArgs e)
        {
            LoadGameStateFromCurrentMap();
        }

        private void BackToMenuClick(object sender, RoutedEventArgs e)
        {
            BackToMenu();
        }
    }
}