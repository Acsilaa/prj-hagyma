using hagymix.lib;
using hagymix.utils;
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

        static Room[,] maze = StringHelper.Char2DToRoomMap(StringHelper.FileToChar2D("resources/minta.txt"));
        static int[] playerPos = new int[2] { 0, 0 };
        static int[] mazeDimensions = new int[2] { Room.GetMazeLength(maze), Room.GetMazeWidth(maze) };
        static Player player;
        static bool isPlaying = false;
        public MainWindow()
        {
            InitializeComponent();
            
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

        }

        private void ToggleLangClick(object sender, RoutedEventArgs e)
        {

        }
    }
}