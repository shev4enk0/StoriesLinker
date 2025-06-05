namespace StoriesLinker;

static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        // Устанавливаем контекст лицензии EPPlus (некоммерческое использование)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        
        // Устанавливаем кодировку консоли для корректного отображения кириллицы
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Form1());
    }
}
