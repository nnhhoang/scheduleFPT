using data_manipulation_visualisation;
using solvers;
using Spectre.Console;
using System.Text.RegularExpressions;

namespace scheduler_interface
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string inputExcelFilePath = "";
            string outputExcelFolderPath = "";

            bool inputPathFlag = false;
            bool inputRegexFlag = false;
            bool outputPathFlag = false;
            bool outputRegexFlag = false;

            DataModel dataModel = new DataModel();

            string[] classNames = Array.Empty<string>();
            string[] slotNames = Array.Empty<string>();
            string[] instructorNames = Array.Empty<string>();
            string[] subjectNames = Array.Empty<string>();
            string[] areaNames = Array.Empty<string>();

            //string xlsxPattern = Constant.XLSX_PATTERN;
            //do
            //{
            //    inputExcelFilePath = $@"{AnsiConsole.Prompt(new TextPrompt<string>(" Input file path ( D:\\InputFolder\\InputExcel.xlsx ): "))}";
            //    inputPathFlag = File.Exists(inputExcelFilePath);
            //    if (!inputPathFlag)
            //        AnsiConsole.Markup("[red]Input File Doesn't Existed![/]\n");
            //    inputRegexFlag = Regex.IsMatch(inputExcelFilePath, xlsxPattern);
            //    if (!inputRegexFlag)
            //        AnsiConsole.Markup("[red]This isn't .xlsx file![/]\n");
            //}
            //while (!inputPathFlag || !inputRegexFlag);

            //string pattern = Constant.EXPORT_PATH_PATTEN;
            //do
            //{
            //    outputExcelFolderPath = AnsiConsole.Prompt(new TextPrompt<string>(" Output path ( D:\\OutputFolder\\ ): "));
            //    outputPathFlag = Directory.Exists(outputExcelFolderPath);
            //    if (!outputPathFlag)
            //        AnsiConsole.Markup("[red]Output Path Doesn't Existed![/]\n");
            //    outputRegexFlag = Regex.IsMatch(outputExcelFolderPath, pattern);
            //    if (!outputRegexFlag)
            //        AnsiConsole.Markup("[red]Output Path Format Doesn't Correct![/]\n");
            //}
            //while (!outputPathFlag || !outputRegexFlag);


            inputExcelFilePath = @"C:\Users\reina\OneDrive\Desktop\inputSE.xlsx";
            outputExcelFolderPath = @"C:\Users\reina\OneDrive\Desktop\";

            // Read and Solve
            //var results = new List<List<(int, int)>>();
            bool isRead = ExcelHandler.ReadInputExcel(inputExcelFilePath, dataModel, ref classNames, ref slotNames, ref instructorNames, ref subjectNames, ref areaNames);
            
            Util.cleanCOM();
            if (isRead)
            {
                //FBTCPSAT cpSat = new FBTCPSAT(dataModel, new SolverConfiguration());
                //cpSat.SetupSolver();
                //var results = cpSat.ObjectiveOptimize();

                //ExcelHandler.WriteOutputExcel(outputExcelFolderPath, dataModel, results[0], classNames, slotNames, instructorNames, subjectNames);

                var cplex = new FBTCPSAT(dataModel, new SolverConfiguration());
                cplex.SetupSolver();
                var results = cplex.ObjectiveOptimize();
                ExcelHandler.WriteOutputExcel(outputExcelFolderPath, dataModel, results[0], classNames, slotNames, instructorNames, subjectNames);
            }
            Console.Write("\nPress anything to exit.");
            Console.ReadKey();
        }
    }
}
