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

        PortableVM.VM a;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
            a = new VM();
            a.onUnknownInstruction += delegate(VM senderVM, string instruction, 
                                               List<PortableVM.DynamicValue> rawVars, 
                                               List<PortableVM.DynamicValue> solvedArgs, 
                                               ref int nextIp, 
                                               out bool allowContinue)
            {
                if (instruction == "ui.messagebox")
                {
                    allowContinue = true;
                    MessageBox.Show(solvedArgs[0].AsString);
                    return null;
                }
                
                allowContinue = false;
                return null;
                
            };
            
            a.addCode(new List<string>
            {
                "set i 100000",
                "set countTotal 0",
                "_nc_ start",
                "if i <= 0 \"end\"",
                "     math.sub i 1",
                "     set i _return",
                //increment the count total
                "     math.sum countTotal 1",
                "     set countTotal _return",
                "     goto \"start\"",
                "_nc_ end",   
                "Ui.MessageBox countTotal",
                "finish"
            });
            
            DateTime start = DateTime.Now;
            a.Run(true);
            
            var result = DateTime.Now.Subtract(start);
            double totalMilisseconds = result.TotalMilliseconds;
            double speed = a.totalRunnedInstructions / result.TotalSeconds;
            MessageBox.Show("Total milisseconds = " + totalMilisseconds.ToString() + 
                            ".\nThe 'i' variable is "+a.GetVar("i", new DynamicValue("Error")).AsString +
                            ".\nThe 'countTotal' variable is "+a.GetVar("countTotal", new DynamicValue("Error")).AsString +
                            "\n The total of runned instruction are: "+a.totalRunnedInstructions +         
                            "\nThe VM speed is "+speed+" instructions by second");
        }
        
    }
}
