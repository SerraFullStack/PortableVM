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
            
            /*a.addCode(new List<string>
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
            */
           
           a.addCode(new List<string>
            {
                "object.create \"object\" teste",
                "ui.messagebox teste",
                "object.setProperty teste prop \"cool\"",
                "eval \"ui.messagebox \" teste \".prop\"",
                "object.getProperty teste prop",
                "set a 'legal'",
                "set b 'carrinho'",
                "string.compose 'mas bah! Achei esse' b ' muito ' a",
                "ui.messagebox _return",
                "",
                "",
                "set count  0",
                "goto 'run'",
                "",
                "_nc_ thread",
                "   math.sum count 1",
                "   set count _return",
                "   if count >= 100000 'endThread'",
                "   goto 'thread'",
                "   _nc_ endThread",
                "       ParentThreadCall 'teste'",
                "       ui.messagebox 'Thread has ended'",
                "       finish",
                "return",
                "",
                "_nc_ teste",
                "   ui.messagebox 'The thread call a parent thread metod",
                "return",
                "",
                "",
                "",
                "",
                "",
                "",
                "_nc_ funcaoTeste(v1, v2)",
                "   set temp 'legalzao'",
                "   set temp2 'muito'",
                "   string.compose 'este teste é ',temp2,' ','show e ',temp ' (' v1 ', ' v2 ')'",
                "return _return",
                "",
                "",
                "",
                "",
                "_nc_ run",
                "   StarThread 'thread'",
                "",
                "   Sleep(1000)",
                "   ui.messagebox count",
                "",
                "",
                "",
                "attribuitionTest = string.compose 'este ' 'é', ' um ' ' teste de atribuição'",
                "ui.messagebox attribuitionTest",
                "",
                "",
                "",
                "testeString2 = call funcaoTeste 'parametro1', 'outro parametro'",
                "ui.messagebox testeString2",
                "",
                "",
                "",
                "finish"
            });
           
           a.Run(true);
        }
        
    }
}
