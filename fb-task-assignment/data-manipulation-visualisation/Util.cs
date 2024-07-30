using Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using Range = Microsoft.Office.Interop.Excel.Range;

namespace data_manipulation_visualisation
{
    public static class Util
    {
        public static int calObjWalkingDistance(List<int> tasks, DataModel dataModel)
        {
            int distance = 0;
            int n = tasks.Count();
            for (int i = 0; i < n - 1; i++)
                for (int j = i + 1; j < n; j++)
                {
                    int t1 = tasks[i];
                    int t2 = tasks[j];
                    distance += dataModel.AreaSlotCoefficient[dataModel.TaskSlotMapping[t1], dataModel.TaskSlotMapping[t2]] * dataModel.AreaDistance[dataModel.TaskAreaMapping[t1], dataModel.TaskAreaMapping[t2]];
                }
            return distance;
        }
        public static int calObjWaitingTime(List<int> tasks, DataModel dataModel)
        {
            int result = 0;
            int[,] flag = new int[dataModel.NumDays, dataModel.NumSegments];
            foreach (int task in tasks)
            {
                int slot = dataModel.TaskSlotMapping[task];
                foreach (int d in dataModel.AllDays)
                    foreach (int s in dataModel.AllSegments)
                        if (dataModel.SlotSegment[slot, d, s] == 1)
                            flag[d, s] = 1;
            }
            foreach (int d in dataModel.AllDays)
            {
                int pattern = 0;
                foreach (int s in dataModel.AllSegments)
                    pattern += flag[d, s] * (1 << (dataModel.NumSegments - s - 1));
                result += dataModel.PatternCost[pattern];
            }
            return result;
        }

        public static int[] excelToMapping(Worksheet oSheet, int numRows, int col, string[] namesArray)
        {
            int[] mapping = new int[numRows];
            Range oRng;
            for (int i = 2; i <= numRows + 1; i++)
            {
                oRng = oSheet.Cells[i, col];
                if (oRng.Value2 == null)
                {
                    throw new Exception($"Missing data at {oSheet.Name} {i} - {col} !");
                }
                mapping[i - 2] = Array.IndexOf(namesArray, oRng.Value2);
                if (mapping[i - 2] == -1)
                {
                    throw new Exception($"Cannot find value {oRng.Value2} at {oSheet.Name} {i} - {col} in name array to map!");
                }
            }
            return mapping;
        }
        public static int[,] excelToArray(Worksheet oSheet, int startRow, int startCol, int numRows, int numCols, int lb, int ub)
        {
            Range oRng;
            oRng = oSheet.Cells[startRow, startCol].Resize[numRows, numCols];
            object[,] values = (object[,])oRng.Value;
            int[,] data = new int[numRows, numCols];
            for (int i = 1; i <= numRows; i++)
            {
                for (int j = 1; j <= numCols; j++)
                {
                    if (values[i, j] == null)
                    {
                        throw new Exception($"Missing value at sheet {oSheet.Name} {startRow + i - 1} - {startCol + j - 1} !");
                    }
                    try
                    {
                        data[i - 1, j - 1] = (int)(double)values[i, j];
                        if (!(lb <= data[i - 1, j - 1] && data[i - 1, j - 1] <= ub))
                            throw new Exception("Value must be in range!");
                    }
                    catch (Exception)
                    {
                        throw new Exception($"Invalid value at sheet {oSheet.Name} {startRow + i - 1} - {startCol + j - 1}");
                    }

                }
            }
            return data;
        }
        public static string[] excelToNameArray(Worksheet oSheet, int count, bool isColumn, int posrow, int poscol)
        {
            string[] data = new string[count];
            Range oRng;
            if (isColumn)
            {
                oRng = oSheet.Cells[posrow, poscol].Resize[count, 1];
                object[,] values = (object[,])oRng.Value;
                for (int i = 1; i <= count; i++)
                {
                    if (values[i, 1] == null)
                        throw new Exception($"Missing name at sheet {oSheet.Name} {i + 1} - {1} !");
                    data[i - 1] = (string)values[i, 1];
                }
            }
            else
            {
                oRng = oSheet.Cells[posrow, poscol].Resize[1, count];
                object[,] values = (object[,])oRng.Value;
                for (int i = 1; i <= count; i++)
                {
                    if (values[1, i] == null)
                        throw new Exception($"Missing name at sheet {oSheet.Name} {1} - {i + 1} !");
                    data[i - 1] = (string)values[1, i];
                }
            }
            return data;
        }
        public static void alignMiddle(Range range)
        {
            range.VerticalAlignment = XlVAlign.xlVAlignCenter;
            range.HorizontalAlignment = XlHAlign.xlHAlignCenter;
        }
        public static void fullBorder(Range range)
        {
            range.BorderAround(XlLineStyle.xlContinuous, XlBorderWeight.xlThin);
        }
        public static void drawChart(Worksheet oWS, ChartObjects charts, string name, string bartype, string x, string y, int[] valuearray, int offset, bool rounded)
        {
            int[] distinctvalues = valuearray.Distinct().OrderBy(x => x).ToArray();
            int[] distinctcount = valuearray.FindAllIndexof(distinctvalues).ToArray();
            for (int i = 0; i < distinctvalues.Length; i++)
            {
                if (rounded)
                    oWS.Cells[offset * 20 + 1, i + 2] = distinctvalues[i] / 100.0;
                else
                    oWS.Cells[offset * 20 + 1, i + 2] = distinctvalues[i];
                oWS.Cells[offset * 20 + 1, i + 2].Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.DarkOrange);
                oWS.Cells[offset * 20 + 2, i + 2] = distinctcount[i];
            }
            oWS.Cells[offset * 20 + 2, 1] = bartype;
            oWS.Cells[offset * 20 + 2, 1].Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.SteelBlue);
            // OFFSET X OFF SET Y SIZE X,SIZE Y
            ChartObject chartObject = charts.Add(0, offset * 300 + 30, 600, 270);
            var chart = chartObject.Chart;
            // Set chart range.
            var range = oWS.Cells[offset * 20 + 1, 1].Resize[2, distinctvalues.Length + 1];
            chart.SetSourceData(range);
            // Set chart properties.
            chart.ChartType = XlChartType.xlColumnStacked;
            chart.ChartWizard(Source: range,
                Title: name,
                CategoryTitle: x,
                ValueTitle: y);
        }

        public static int[,] toBinaryArray(int[,] data)
        {
            int numRows = data.GetLength(0);
            int numColumns = data.GetLength(1);
            int[,] result = new int[numRows, numColumns];
            for (int i = 0; i < numRows; i++)
                for (int j = 0; j < numColumns; j++)
                    if (data[i, j] > 0)
                    {
                        result[i, j] = 1;
                    }
                    else
                    {
                        result[i, j] = 0;
                    }
            return result;
        }
        public static int[] flattenArray(int[,] data)
        {
            int numRows = data.GetLength(0);
            int numColumns = data.GetLength(1);
            int[] flattened = new int[numRows * numColumns];
            int k = 0;
            for (int i = 0; i < numRows; i++)
            {
                for (int j = 0; j < numColumns; j++)
                {
                    flattened[k++] = data[i, j];
                }
            }
            return flattened;
        }
        public static void Log2DArray(int[,] array)
        {
            for (int i = 0; i < array.GetLength(0); i++)
            {
                for (int j = 0; j < array.GetLength(1); j++)
                {
                    Console.Write($"{array[i, j]} ");
                }
                Console.WriteLine();
            }
        }
        public static void LogResult(List<List<(int, int)>> results, int size)
        {
            if (results != null)
            {
                List<(int, int)> tmp = results[0];
                Console.Write("[");
                for (int i = 0; i < size; i++)
                {
                    Console.Write(tmp[i].Item2);
                    if (i != size - 1)
                    {
                        Console.Write(",");
                    }
                }
                Console.Write("]");
            }
        }
        public static void cleanCOM()
        {
            do
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            while (Marshal.AreComObjectsAvailableForCleanup());
        }
        public static int[] FindAllIndexof<T>(this IEnumerable<T> values, T[] val)
        {
            List<int> index = new List<int>();
            for (int j = 0; j < val.Length; j++)
                index.Add(values.Count(x => object.Equals(x, val[j])));
            return index.ToArray();
        }
    }
}
