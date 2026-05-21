using hagymix.lib;
using hagymix.utils;
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
        public MainWindow()
        {
            InitializeComponent();
            CreateColumnDefinitions();
            CreateRowDefinitions();
            CreateMapVisualization();
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
                        Viewbox cell = new Viewbox();
                        cell.Child = new Label {
                            Content = maze[i, j]?.roomChar,
                            HorizontalContentAlignment = HorizontalAlignment.Center,
                            VerticalContentAlignment = VerticalAlignment.Center,
                            FontSize = 300
                        };
                        Grid.SetRow(cell, i);
                        Grid.SetColumn(cell, j);
                        MazeGrid.Children.Add(cell);
                    }
                }
            }
        }
    }
}