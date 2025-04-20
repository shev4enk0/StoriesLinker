using System;
using System.Text;
using System.Windows.Forms;

namespace StoriesLinker
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Регистрируем поддержку дополнительных кодовых страниц
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            
            // Остальной код...
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}