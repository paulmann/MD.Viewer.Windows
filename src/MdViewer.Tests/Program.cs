using System.Text;
using MdViewer.Tests;

namespace MdViewer.Tests;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        if (args.Length > 0 && args.Contains("--ui-smoke"))
        {
            return UiSmoke.Run(args);
        }

        if (args.Length > 0 && args.Contains("--report"))
        {
            return Report.Run(args);
        }

        if (args.Length > 0 && args.Contains("--snapshot"))
        {
            return Snapshot.Run(args);
        }

        bool verbose = args.Contains("--verbose") || args.Contains("-v");
        Harness.Verbose = verbose;

        Console.WriteLine("MD.Viewer.Windows — test runner");

        ParserTests.Run();
        RenderTests.Run();
        ThemeTests.Run();
        DiagramTests.Run();
        MarkdownExtraTests.Run();
        GlossaryTests.Run();
        PrintTests.Run();

        return Harness.Summarize();
    }
}





