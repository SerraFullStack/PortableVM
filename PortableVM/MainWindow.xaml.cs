using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PortableVM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
            PortableVM.VM a = new VM();
            
            a.addCode(new List<string>
            {
                "set i 100000",
                "_nc_ start",
                "if i <= 0 \"end\"",
                "     math.sub i 1",
                "     set i _return",  
                "     goto \"start\"",
                "_nc_ end",
                "finish"
            });
            
            DateTime start = DateTime.Now;
            a.Run(true);
            
            var result = DateTime.Now.Subtract(start);
            double totalMilisseconds = result.TotalMilliseconds;
            double speed = a.totalRunnedInstructions / result.TotalSeconds;
            MessageBox.Show("Total milisseconds = " + totalMilisseconds.ToString() + ".\nThe 'i' variable is "+a.GetVar("i", new DynamicValue("Error")).AsString +
                            "\n The total of runned instruction are: "+a.totalRunnedInstructions +         
                            "\nThe VM speed is "+speed+" instructions by second");
        }
        
    }
}
