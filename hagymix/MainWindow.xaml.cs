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
        static Room?[,] maze = StringHelper.Char2DToRoomMap(StringHelper.FileToChar2D("maze.txt"));
        static int[] playerPos = new int[2] { 0, 0 };
        public MainWindow()
        {
            InitializeComponent();
            
        }
    }
}