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

        static Room?[,] maze = StringHelper.Char2DToRoomMap(StringHelper.FileToChar2D("resources/minta.txt"));
        static int[] playerPos = new int[2] { 0, 0 };
        static int[] mazeDimensions = new int[2] { Room.GetMazeLength(maze), Room.GetMazeWidth(maze) };
        static Player player;
        public MainWindow()
        {
            InitializeComponent();
            CreateColumnDefinitions();
            CreateRowDefinitions();
            CreateMapVisualization();
            player = new Player(maze);
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
                    if (maze[i, j] != null)
                    {
                        Viewbox cell = new Viewbox { Stretch = Stretch.Uniform, StretchDirection = StretchDirection.Both, Margin = new Thickness(0) };
                        var tb = new TextBlock {
                            Text = maze[i, j]?.roomChar,
                            TextAlignment = TextAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontSize = 100,
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
        }
        private void Main_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    if (player.IsStarted)
                    {
                        player.Move(Direction.Up);
                    }
                    break;
                case Key.Right:
                    if (player.IsStarted)
                    {
                        player.Move(Direction.Right);
                    }
                    else
                    {
                        player.ChangeEntrance();
                    }
                    break;
                case Key.Down:
                    if (player.IsStarted)
                    {
                        player.Move(Direction.Down);
                    }
                    break;
                case Key.Left:
                    if (player.IsStarted)
                    {
                        player.Move(Direction.Left);
                    }
                    break;
                case Key.Enter:
                case Key.Space:
                    if (!player.IsStarted) { player.SetEntrance(); }
                    break;
            }
            for(int i =0;i< MazeGrid.Children.Count; i++)
            {
                Viewbox vb2 = (Viewbox)MazeGrid.Children[i];
                TextBlock tb2 = (TextBlock)vb2.Child;
                tb2.Background = Brushes.White;
            }
            
            
            Viewbox vb = (Viewbox)MazeGrid.Children[player.Y * maze.GetLength(1) + player.X];
            TextBlock tb = (TextBlock)vb.Child;
            tb.Background = Brushes.Green;

        }

    }
}