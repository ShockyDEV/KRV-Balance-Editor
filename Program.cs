using System;
using System.IO;
using System.Windows.Forms;

namespace BalanceEditor
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                string logPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "crash.log");
                File.WriteAllText(logPath, ex.ToString());
                MessageBox.Show(ex.ToString(), "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
