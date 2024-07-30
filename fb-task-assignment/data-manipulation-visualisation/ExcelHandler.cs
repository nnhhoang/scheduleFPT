using Microsoft.Office.Interop.Excel;
using Spectre.Console;

namespace data_manipulation_visualisation
{
    public class ExcelHandler
    {
        public static bool ReadInputExcel(
            string inputPath,
            DataModel dataModel,
            ref string[] classNames,
            ref string[] slotNames,
            ref string[] instructorNames,
            ref string[] subjectNames,
            ref string[] areaNames
        )
        {
            Application? oXL = null;
            Workbook? oWB = null;
            try
            {
                AnsiConsole.MarkupLine("\n{0} [underline green]{1}[/]\n", "Import Data From", $"{inputPath}".EscapeMarkup());
                oXL = new Application();
                oWB = oXL.Workbooks.Open(inputPath);
                if (oWB.Sheets.Count < 13)
                {
                    throw new Exception($"{oWB.Sheets.Count}/13 sheets found!");
                }
                Worksheet oWS_inputInfo = oWB.Sheets[1];
                Worksheet oWS_tasks = oWB.Sheets[2];
                Worksheet oWS_slotConflict = oWB.Sheets[3];
                Worksheet oWS_slotDay = oWB.Sheets[4];
                Worksheet oWS_slotTime = oWB.Sheets[5];
                Worksheet oWS_slotSegment = oWB.Sheets[6];
                Worksheet oWS_patternCost = oWB.Sheets[7];
                Worksheet oWS_instructorSubject = oWB.Sheets[8];
                Worksheet oWS_instructorSlot = oWB.Sheets[9];
                Worksheet oWS_instructorQuota = oWB.Sheets[10];
                Worksheet oWS_instructorPreassign = oWB.Sheets[11];
                Worksheet oWS_areaDistance = oWB.Sheets[12];
                Worksheet oWS_areaSlotCoefficient = oWB.Sheets[13];

                int numSlotSegmentRules = 0;

                try
                {
                    dataModel.NumTasks = (int)oWS_inputInfo.Cells[1, 2].Value2;
                    dataModel.NumInstructors = (int)oWS_inputInfo.Cells[2, 2].Value2;
                    dataModel.NumSlots = (int)oWS_inputInfo.Cells[3, 2].Value2;
                    dataModel.NumDays = (int)oWS_inputInfo.Cells[4, 2].Value2;
                    dataModel.NumTimes = (int)oWS_inputInfo.Cells[5, 2].Value2;
                    dataModel.NumSegments = (int)oWS_inputInfo.Cells[6, 2].Value2;
                    numSlotSegmentRules = (int)oWS_inputInfo.Cells[7, 2].Value2;
                    dataModel.NumSubjects = (int)oWS_inputInfo.Cells[8, 2].Value2;
                    dataModel.NumAreas = (int)oWS_inputInfo.Cells[9, 2].Value2;
                    dataModel.NumBackupInstructors = (int)oWS_inputInfo.Cells[10, 2].Value2;
                }
                catch
                {
                    throw new Exception($"Missing data in info sheet!");
                }
                // NAME
                classNames = Util.excelToNameArray(oWS_tasks, dataModel.NumTasks, true, 2, 1);
                slotNames = Util.excelToNameArray(oWS_slotConflict, dataModel.NumSlots, true, 2, 1);
                instructorNames = Util.excelToNameArray(oWS_instructorSubject, dataModel.NumInstructors, true, 2, 1);
                subjectNames = Util.excelToNameArray(oWS_instructorSubject, dataModel.NumSubjects, false, 1, 2);
                areaNames = Util.excelToNameArray(oWS_areaDistance, dataModel.NumAreas, true, 2, 1);
                // SLOT
                dataModel.SlotConflict = Util.excelToArray(oWS_slotConflict, 2, 2, dataModel.NumSlots, dataModel.NumSlots, 0, 1);
                dataModel.SlotDay = Util.excelToArray(oWS_slotDay, 2, 2, dataModel.NumSlots, dataModel.NumDays, 0, 1);
                dataModel.SlotTime = Util.excelToArray(oWS_slotTime, 2, 2, dataModel.NumSlots, dataModel.NumTimes, 0, 1);
                dataModel.SlotSegment = new int[dataModel.NumSlots, dataModel.NumDays, dataModel.NumSegments];
                for (int i = 0; i < numSlotSegmentRules; i++)
                {
                    if (oWS_slotSegment.Cells[i + 2, 1].Value2 == null)
                    {
                        throw new Exception($"Missing value at {oWS_slotSegment} {i + 2} - 1");
                    }
                    int slot = Array.IndexOf(slotNames, (string)oWS_slotSegment.Cells[i + 2, 1].Value2);
                    if (slot == -1)
                    {
                        throw new Exception($"Cannot find value {(string)oWS_slotSegment.Cells[i + 2, 1].Value2} at {oWS_slotSegment} {i + 2} - 1 in name array to map!");
                    }
                    int day = 0;
                    if (oWS_slotSegment.Cells[i + 2, 2].Value2 == null)
                    {
                        throw new Exception($"Missing value at {oWS_slotSegment} {i + 2} - 2");
                    }
                    try
                    {
                        day = (int)(double)oWS_slotSegment.Cells[i + 2, 2].Value2 - 1; ;
                        if (!(0 <= day && day < dataModel.NumDays))
                            throw new Exception("Value must be in range!");
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Invalid value at sheet {oWS_slotSegment} {i + 2} - 2");
                    }

                    int segment = 0;
                    if (oWS_slotSegment.Cells[i + 2, 3].Value2 == null)
                    {
                        throw new Exception($"Missing value at {oWS_slotSegment} {i + 2} - 3");
                    }
                    try
                    {
                        segment = (int)(double)oWS_slotSegment.Cells[i + 2, 3].Value2 - 1;
                        if (!(0 <= segment && segment < dataModel.NumSegments))
                            throw new Exception("Value must be in range!");
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Invalid value at sheet {oWS_slotSegment} {i + 2} - 3");
                    }

                    dataModel.SlotSegment[slot, day, segment] = 1;
                }
                dataModel.PatternCost = Util.flattenArray(Util.excelToArray(oWS_patternCost, 2, 2, (1 << dataModel.NumSegments), 1, 0, Int32.MaxValue));
                // INSTRUCTOR
                dataModel.InstructorSubjectPreference = Util.excelToArray(oWS_instructorSubject, 2, 2, dataModel.NumInstructors, dataModel.NumSubjects, 0, 5);
                dataModel.InstructorSubject = Util.toBinaryArray(dataModel.InstructorSubjectPreference);
                dataModel.InstructorSlotPreference = Util.excelToArray(oWS_instructorSlot, 2, 2, dataModel.NumInstructors, dataModel.NumSlots, 0, 5);
                dataModel.InstructorSlot = Util.toBinaryArray(dataModel.InstructorSlotPreference);
                dataModel.InstructorQuota = Util.flattenArray(Util.excelToArray(oWS_instructorQuota, 2, 3, dataModel.NumInstructors, 1, 0, Int32.MaxValue));
                dataModel.InstructorMinQuota = Util.flattenArray(Util.excelToArray(oWS_instructorQuota, 2, 2, dataModel.NumInstructors, 1, 0, Int32.MaxValue));
                dataModel.InstructorPreassign = new List<(int, int, int)>();
                for (int i = 0; i < dataModel.NumInstructors; i++)
                {
                    for (int j = 0; j < dataModel.NumSlots; j++)
                    {
                        var content = oWS_instructorPreassign.Cells[i + 2, j + 2].Value2;
                        if (content != null)
                        {
                            dataModel.InstructorPreassign.Add((i, (int)content - 1, 1));
                        }
                    }
                }
                // AREA
                dataModel.AreaDistance = Util.excelToArray(oWS_areaDistance, 2, 2, dataModel.NumAreas, dataModel.NumAreas, 0, Int32.MaxValue);
                dataModel.AreaSlotCoefficient = Util.excelToArray(oWS_areaSlotCoefficient, 2, 2, dataModel.NumSlots, dataModel.NumSlots, 0, Int32.MaxValue);
                // TASK
                dataModel.TaskSubjectMapping = Util.excelToMapping(oWS_tasks, dataModel.NumTasks, 2, subjectNames);
                dataModel.TaskSlotMapping = Util.excelToMapping(oWS_tasks, dataModel.NumTasks, 4, slotNames);
                dataModel.TaskAreaMapping = new int[dataModel.NumTasks];
                for (int i = 0; i < dataModel.NumTasks; i++)
                {
                    string cellValue = oWS_tasks.Cells[i + 2, 7].Value2;
                    if (cellValue == null)
                    {
                        throw new Exception($"Missing task - {i + 1} location ");
                    }
                    bool flag = false;
                    for (int j = 0; j < dataModel.NumAreas; j++)
                    {
                        if (cellValue.Contains(areaNames[j]))
                        {
                            dataModel.TaskAreaMapping[i] = j;
                            flag = true;
                            break;
                        }
                    }
                    if (!flag)
                    {
                        throw new Exception($"Cannot recognize location {cellValue} at task - {i + 1}");
                    }
                }
                oWB.Close();
                oXL.Quit();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                if (oWB != null)
                {
                    oWB.Close();
                }
                if (oXL != null)
                {
                    oXL.Quit();
                }
                return false;
            }
        }

        public static void WriteOutputExcel(
            string outputPath,
            DataModel dataModel,
            List<(int, int)>? result,
            string[] classNames,
            string[] slotNames,
            string[] instructorNames,
            string[] subjectNames
        )
        {
            if (result != null)
            {
                Application? oXL = null;
                Workbook? oWB = null;
                try
                {
                    string[] statisticColumn = new string[] { "Quota ", "Teaching Day", "Teaching Time", "Waiting Time", "Subject Diversity", "Quota Available", "Walking Distance", "Subject Preference", "Slot Preference", "Day Efficiency", "Time Efficiency", "Subject Preference Average", "Slot Preference Average" };
                    DateTime currentTime = DateTime.Now;
                    string currentTimeString = currentTime.ToString("yyyy-MM-ddTHH-mm-ss");
                    AnsiConsole.MarkupLine("\n{0} [underline green]{1}[/]\n", "Export Result Into", $"{outputPath}result_{currentTimeString}.xlsx".EscapeMarkup());

                    oXL = new Application();
                    oWB = oXL.Workbooks.Add();

                    int[] dataQuota = new int[dataModel.NumInstructors];
                    int[] dataDayEfficiency = new int[dataModel.NumInstructors];
                    int[] dataTimeEfficiency = new int[dataModel.NumInstructors];
                    int[] dataWaitingTime = new int[dataModel.NumInstructors];
                    int[] dataSubjectDiversity = new int[dataModel.NumInstructors];
                    int[] dataQuotaAvailable = new int[dataModel.NumInstructors];
                    int[] dataWalkingDistance = new int[dataModel.NumInstructors];
                    int[] dataSubjectPreference = new int[dataModel.NumInstructors];
                    int[] dataSlotPreference = new int[dataModel.NumInstructors];

                    bool[] flag = new bool[dataModel.NumTasks];
                    #region Statistic
                    Worksheet oWS = oWB.ActiveSheet;
                    oWS.Name = "Statistic";
                    for (int i = 0; i < dataModel.NumInstructors; i++)
                    {
                        oWS.Cells[i + 2, 1] = instructorNames[i];
                        oWS.Cells[i + 2, 1].Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.DarkOrange);
                        Util.alignMiddle(oWS.Cells[i + 2, 1]);
                    }
                    for (int i = 0; i < statisticColumn.Length; i++)
                    {
                        oWS.Cells[1, i + 2] = statisticColumn[i];
                        oWS.Cells[1, i + 2].Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.SteelBlue);
                        Util.alignMiddle(oWS.Cells[1, i + 2]);
                    }
                    for (int i = 0; i <= dataModel.NumInstructors; i++)
                    {
                        for (int j = 0; j <= statisticColumn.Length; j++)
                        {
                            Util.fullBorder(oWS.Cells[i + 1, j + 1]);
                        }
                    }
                    var sorted = result.OrderBy(t => t.Item2);
                    int currentId = -1;
                    int objQuota = 0;
                    int[] objDay = new int[dataModel.NumDays];
                    int[,] objTime = new int[dataModel.NumDays, dataModel.NumTimes];
                    int objWaiting = 0;
                    int[] objSubjectDiversity = new int[dataModel.NumSubjects];
                    int objQuotaAvailable = 0;
                    int objWalkingDistance = 0;
                    int objSubjectPreference = 0;
                    int objSlotPreference = 0;
                    List<int> tasks = new List<int>();
                    foreach (var item in sorted)
                    {
                        if (currentId != item.Item2)
                        {
                            if (currentId != -1)
                            {
                                oWS.Cells[currentId + 2, 2] = objQuota;
                                oWS.Cells[currentId + 2, 3] = objDay.Sum();
                                oWS.Cells[currentId + 2, 4] = Util.flattenArray(objTime).Sum();
                                oWS.Cells[currentId + 2, 5] = Util.calObjWaitingTime(tasks, dataModel);
                                oWS.Cells[currentId + 2, 6] = objSubjectDiversity.Sum();
                                oWS.Cells[currentId + 2, 7] = dataModel.InstructorQuota[currentId] - objQuota;
                                oWS.Cells[currentId + 2, 8] = Util.calObjWalkingDistance(tasks, dataModel);
                                oWS.Cells[currentId + 2, 9] = objSubjectPreference;
                                oWS.Cells[currentId + 2, 10] = objSlotPreference;

                                dataQuota[currentId] = objQuota;
                                dataWaitingTime[currentId] = Util.calObjWaitingTime(tasks, dataModel);
                                dataSubjectDiversity[currentId] = objSubjectDiversity.Sum();
                                dataQuotaAvailable[currentId] = dataModel.InstructorQuota[currentId] - objQuota;
                                dataWalkingDistance[currentId] = Util.calObjWalkingDistance(tasks, dataModel);
                                if (objQuota != 0)
                                {
                                    dataDayEfficiency[currentId] = (int)(100.0 * objQuota / (objDay.Sum() * 2.0));
                                    dataTimeEfficiency[currentId] = (int)(100.0 * objQuota / Util.flattenArray(objTime).Sum());
                                    dataSubjectPreference[currentId] = (int)(100.0 * objSubjectPreference / objQuota);
                                    dataSlotPreference[currentId] = (int)(100.0 * objSlotPreference / objQuota);

                                    oWS.Cells[currentId + 2, 11] = dataDayEfficiency[currentId] / 100.0;
                                    oWS.Cells[currentId + 2, 12] = dataTimeEfficiency[currentId] / 100.0;
                                    oWS.Cells[currentId + 2, 13] = dataSubjectPreference[currentId] / 100.0;
                                    oWS.Cells[currentId + 2, 14] = dataSlotPreference[currentId] / 100.0;
                                }
                                else
                                {
                                    dataDayEfficiency[currentId] = 100;
                                    dataTimeEfficiency[currentId] = 100;
                                    dataSubjectPreference[currentId] = 500;
                                    dataSlotPreference[currentId] = 500;

                                    oWS.Cells[currentId + 2, 11] = 1;
                                    oWS.Cells[currentId + 2, 12] = 1;
                                    oWS.Cells[currentId + 2, 13] = 5;
                                    oWS.Cells[currentId + 2, 14] = 5;
                                }


                            }
                            //reset
                            objQuota = 0;
                            Array.Clear(objDay, 0, objDay.Length);
                            Array.Clear(objTime, 0, objTime.Length);
                            objWaiting = 0;
                            Array.Clear(objSubjectDiversity, 0, objSubjectDiversity.Length);
                            objQuotaAvailable = 0;
                            objWalkingDistance = 0;
                            objSubjectPreference = 0;
                            objSlotPreference = 0;
                            tasks.Clear();
                            currentId = item.Item2;
                        }
                        if (currentId != -1)
                        {
                            tasks.Add(item.Item1);
                            int thisTaskSlot = dataModel.TaskSlotMapping[item.Item1];
                            int thisTaskSubject = dataModel.TaskSubjectMapping[item.Item1];
                            objQuota += 1;
                            for (int d = 0; d < dataModel.NumDays; d++)
                            {
                                if (dataModel.SlotDay[thisTaskSlot, d] == 1)
                                {
                                    objDay[d] = 1;
                                    for (int t = 0; t < dataModel.NumTimes; t++)
                                    {
                                        if (dataModel.SlotTime[thisTaskSlot, t] == 1)
                                            objTime[d, t] = 1;
                                    }
                                }
                            }
                            objSubjectDiversity[thisTaskSubject] = 1;
                            objSubjectPreference += dataModel.InstructorSubjectPreference[item.Item2, thisTaskSubject];
                            objSlotPreference += dataModel.InstructorSlotPreference[item.Item2, thisTaskSlot];
                        }
                    }
                    if (currentId != -1)
                    {
                        oWS.Cells[currentId + 2, 2] = objQuota;
                        oWS.Cells[currentId + 2, 3] = objDay.Sum();
                        oWS.Cells[currentId + 2, 4] = Util.flattenArray(objTime).Sum();
                        oWS.Cells[currentId + 2, 5] = Util.calObjWaitingTime(tasks, dataModel);
                        oWS.Cells[currentId + 2, 6] = objSubjectDiversity.Sum();
                        oWS.Cells[currentId + 2, 7] = dataModel.InstructorQuota[currentId] - objQuota;
                        oWS.Cells[currentId + 2, 8] = Util.calObjWalkingDistance(tasks, dataModel);
                        oWS.Cells[currentId + 2, 9] = objSubjectPreference;
                        oWS.Cells[currentId + 2, 10] = objSlotPreference;


                        dataQuota[currentId] = objQuota;
                        dataWaitingTime[currentId] = Util.calObjWaitingTime(tasks, dataModel);
                        dataSubjectDiversity[currentId] = objSubjectDiversity.Sum();
                        dataQuotaAvailable[currentId] = dataModel.InstructorQuota[currentId] - objQuota;
                        dataWalkingDistance[currentId] = Util.calObjWalkingDistance(tasks, dataModel);
                        if (objQuota != 0)
                        {
                            dataDayEfficiency[currentId] = (int)(100.0 * objQuota / (objDay.Sum() * 2.0));
                            dataTimeEfficiency[currentId] = (int)(100.0 * objQuota / Util.flattenArray(objTime).Sum());
                            dataSubjectPreference[currentId] = (int)(100.0 * objSubjectPreference / objQuota);
                            dataSlotPreference[currentId] = (int)(100.0 * objSlotPreference / objQuota);

                            oWS.Cells[currentId + 2, 11] = dataDayEfficiency[currentId] / 100.0;
                            oWS.Cells[currentId + 2, 12] = dataTimeEfficiency[currentId] / 100.0;
                            oWS.Cells[currentId + 2, 13] = dataSubjectPreference[currentId] / 100.0;
                            oWS.Cells[currentId + 2, 14] = dataSlotPreference[currentId] / 100.0;
                        }
                        else
                        {
                            dataDayEfficiency[currentId] = 100;
                            dataTimeEfficiency[currentId] = 100;
                            dataSubjectPreference[currentId] = 500;
                            dataSlotPreference[currentId] = 500;

                            oWS.Cells[currentId + 2, 11] = 1;
                            oWS.Cells[currentId + 2, 12] = 1;
                            oWS.Cells[currentId + 2, 13] = 5;
                            oWS.Cells[currentId + 2, 14] = 5;
                        }

                    }
                    foreach (int i in dataModel.AllInstructors)
                    {
                        if (oWS.Cells[i + 2, 2].Value == null)
                        {
                            oWS.Cells[i + 2, 2] = 0;
                            oWS.Cells[i + 2, 3] = 0;
                            oWS.Cells[i + 2, 4] = 0;
                            oWS.Cells[i + 2, 5] = 0;
                            oWS.Cells[i + 2, 6] = 0;
                            oWS.Cells[i + 2, 7] = dataModel.InstructorQuota[i];
                            oWS.Cells[i + 2, 8] = 0;
                            oWS.Cells[i + 2, 9] = 0;
                            oWS.Cells[i + 2, 10] = 0;
                            oWS.Cells[i + 2, 11] = 1;
                            oWS.Cells[i + 2, 12] = 1;
                            oWS.Cells[i + 2, 13] = 5;
                            oWS.Cells[i + 2, 14] = 5;

                            dataQuota[i] = 0;
                            dataWaitingTime[i] = 0;
                            dataSubjectDiversity[i] = 0;
                            dataQuotaAvailable[i] = dataModel.InstructorQuota[i];
                            dataWalkingDistance[i] = 0;
                            dataDayEfficiency[i] = 100;
                            dataTimeEfficiency[i] = 100;
                            dataSubjectPreference[i] = 500;
                            dataSlotPreference[i] = 500;

                        }
                    }
                    oWS.Columns.AutoFit();
                    #endregion
                    #region Chart
                    oWS = oWB.Sheets.Add();
                    oWS.Name = "Statistic Chart";
                    ChartObjects charts = oWS.ChartObjects();
                    int[] distinctvalues = dataQuota.Distinct().OrderBy(x => x).ToArray();
                    int[] distinctcount = dataQuota.FindAllIndexof(distinctvalues).ToArray();
                    Util.drawChart(oWS, charts, "Working Quota", "Statistic", "Quota", "Count", dataQuota, 0, false);
                    Util.drawChart(oWS, charts, "Day Efficiency", "Statistic", "Score", "Count", dataDayEfficiency, 1, true);
                    Util.drawChart(oWS, charts, "Time Efficiency", "Statistic", "Score", "Count", dataTimeEfficiency, 2, true);
                    Util.drawChart(oWS, charts, "Waiting Time", "Statistic", "Time", "Count", dataWaitingTime, 3, false);
                    Util.drawChart(oWS, charts, "Subject Diversity", "Statistic", "Subject", "Count", dataSubjectDiversity, 4, false);
                    Util.drawChart(oWS, charts, "Quota Available", "Statistic", "Quota", "Count", dataQuotaAvailable, 5, false);
                    Util.drawChart(oWS, charts, "Walking Distance", "Statistic", "Distance", "Count", dataWalkingDistance, 6, false);
                    Util.drawChart(oWS, charts, "Subject Preference", "Statistic", "Score", "Count", dataSubjectPreference, 7, true);
                    Util.drawChart(oWS, charts, "Slot Preference", "Statistic", "Score", "Count", dataSlotPreference, 8, true);
                    #endregion
                    #region Result
                    oWS = oWB.Sheets.Add();
                    oWS.Name = "Result";

                    for (int i = 0; i < dataModel.NumInstructors; i++)
                    {
                        oWS.Cells[i + 2, 1] = instructorNames[i];
                        oWS.Cells[i + 2, 1].Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.DarkOrange);
                        Util.alignMiddle(oWS.Cells[i + 2, 1]);
                    }
                    oWS.Cells[dataModel.NumInstructors + 2, 1] = "UNASSIGNED";
                    oWS.Cells[dataModel.NumInstructors + 2, 1].Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.OrangeRed);
                    Util.alignMiddle(oWS.Cells[dataModel.NumInstructors + 2, 1]);
                    for (int i = 0; i < dataModel.NumSlots; i++)
                    {
                        oWS.Cells[1, i + 2] = slotNames[i];
                        oWS.Cells[1, i + 2].Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.SteelBlue);
                        Util.alignMiddle(oWS.Cells[1, i + 2]);
                    }
                    for (int i = 0; i <= dataModel.NumInstructors + 1; i++)
                        for (int j = 0; j <= dataModel.NumSlots; j++)
                        {
                            Util.fullBorder(oWS.Cells[i + 1, j + 1]);
                        }
                    foreach ((int, int) pair in result)
                    {
                        if (pair.Item2 >= 0)
                        {
                            flag[pair.Item1] = true;
                            oWS.Cells[pair.Item2 + 2, dataModel.TaskSlotMapping[pair.Item1] + 2] = $"{pair.Item1 + 1}.{classNames[pair.Item1]}.{subjectNames[dataModel.TaskSubjectMapping[pair.Item1]]}";
                            oWS.Cells[pair.Item2 + 2, dataModel.TaskSlotMapping[pair.Item1] + 2].Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.AntiqueWhite);
                        }
                        else
                        {
                            flag[pair.Item1] = false;
                            oWS.Cells[dataModel.NumInstructors + 2, dataModel.TaskSlotMapping[pair.Item1] + 2] = oWS.Cells[dataModel.NumInstructors + 2, dataModel.TaskSlotMapping[pair.Item1] + 2].Value + $"{pair.Item1 + 1}.{classNames[pair.Item1]}.{subjectNames[dataModel.TaskSubjectMapping[pair.Item1]]}\n";
                            oWS.Cells[dataModel.NumInstructors + 2, dataModel.TaskSlotMapping[pair.Item1] + 2].Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Red);
                        }
                    }
                    #endregion
                    #region Subject
                    //SUBJECT
                    int startSubjectTable = dataModel.NumInstructors + 5;
                    int row = 1;
                    List<int>[,] subjects = new List<int>[dataModel.NumSubjects, dataModel.NumSlots];
                    foreach (int i in dataModel.AllSubjects)
                    {
                        foreach (int j in dataModel.AllSlots)
                        {
                            subjects[i, j] = new List<int>();
                        }
                    }
                    int[] subjectSlotCount = new int[dataModel.NumSubjects];
                    foreach (int n in dataModel.AllTasks)
                    {
                        subjects[dataModel.TaskSubjectMapping[n], dataModel.TaskSlotMapping[n]].Add(n);
                        subjectSlotCount[dataModel.TaskSubjectMapping[n]] = Math.Max(subjectSlotCount[dataModel.TaskSubjectMapping[n]], subjects[dataModel.TaskSubjectMapping[n], dataModel.TaskSlotMapping[n]].Count());
                    }

                    for (int i = 0; i < dataModel.NumSlots; i++)
                    {
                        oWS.Cells[startSubjectTable + 1, i + 2] = slotNames[i];
                        oWS.Cells[startSubjectTable + 1, i + 2].Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.SteelBlue);
                        Util.alignMiddle(oWS.Cells[startSubjectTable + 1, i + 2]);
                    }
                    row++;
                    for (int i = 0; i < dataModel.NumSubjects; i++)
                    {
                        for (int j = 0; j < subjectSlotCount[i]; j++)
                        {
                            oWS.Cells[startSubjectTable + row, 1] = subjectNames[i];
                            if (i % 2 == 0)
                            {
                                oWS.Cells[startSubjectTable + row, 1].Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.DarkOrange);
                            }
                            else
                            {
                                oWS.Cells[startSubjectTable + row, 1].Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.LightSalmon);
                            }
                            for (int z = 0; z < dataModel.NumSlots; z++)
                            {
                                if (subjects[i, z].Count() > j)
                                {
                                    int subjectId = subjects[i, z][j];
                                    oWS.Cells[startSubjectTable + row, z + 2] = $"{subjectId + 1}.{classNames[subjectId]}.{subjectNames[dataModel.TaskSubjectMapping[subjectId]]}";
                                    if (flag[subjectId])
                                        oWS.Cells[startSubjectTable + row, z + 2].Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.AntiqueWhite);
                                    else
                                        oWS.Cells[startSubjectTable + row, z + 2].Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Red);
                                }
                            }
                            row++;
                        }
                    }
                    for (int i = startSubjectTable + 1; i < startSubjectTable + row; i++)
                    {
                        for (int j = 1; j <= dataModel.NumSlots + 1; j++)
                        {
                            Util.fullBorder(oWS.Cells[i, j]);
                        }
                    }
                    oWS.Columns.AutoFit();
                    #endregion
                    oWB.SaveAs($@"{outputPath}result_{currentTimeString}.xlsx");
                    oWB.Close();
                    oXL.Quit();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    if (oWB != null)
                    {
                        oWB.Close();
                    }
                    if (oXL != null)
                    {
                        oXL.DisplayAlerts = true;
                        oXL.Quit();
                    }
                }
            }
            else
            {
                Console.Write("\nNo solution to export!\n\n");
            }
        }
    }
}
