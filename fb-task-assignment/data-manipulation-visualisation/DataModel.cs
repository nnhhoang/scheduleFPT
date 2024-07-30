namespace data_manipulation_visualisation
{
    public class DataModel
    {
        public double MaxSearchingTimeOption { get; set; } = 120.0;
        public int Strategy { get; set; } = 2;
        public int[] ObjectiveOptions { get; set; } = new int[8] { 1, 1, 0, 0, 0, 0, 0, 0 };
        public int[] ObjectiveWeight { get; set; } = new int[8] { 50, 25, 1, 1, 1, 1, 1, 1 };
        public bool DebugLoggerOption { get; set; } = false;
        public int NumSubjects { get; set; } = 0;
        public int NumTasks { get; set; } = 0;
        public int NumSlots { get; set; } = 0;
        public int NumDays { get; set; } = 0;
        public int NumTimes { get; set; } = 0;
        public int NumSegments { get; set; } = 0;
        public int NumInstructors { get; set; } = 0;
        public int NumBackupInstructors { get; set; } = 0;
        public int NumAreas { get; set; } = 0;
        public int[] AllSubjects { get; set; } = Array.Empty<int>();
        public int[] AllTasks { get; set; } = Array.Empty<int>();
        public int[] AllSlots { get; set; } = Array.Empty<int>();
        public int[] AllDays { get; set; } = Array.Empty<int>();
        public int[] AllTimes { get; set; } = Array.Empty<int>();
        public int[] AllSegments { get; set; } = Array.Empty<int>();
        public int[] AllInstructors { get; set; } = Array.Empty<int>();
        public int[] AllInstructorsWithBackup { get; set; } = Array.Empty<int>();
        public int[,] SlotConflict { get; set; } = new int[0, 0];
        public int[,] SlotDay { get; set; } = new int[0, 0];
        public int[,] SlotTime { get; set; } = new int[0, 0];
        public int[,,] SlotSegment { get; set; } = new int[0, 0, 0];
        public int[] PatternCost { get; set; } = Array.Empty<int>();
        public int[,] InstructorSubject { get; set; } = new int[0, 0];
        public int[,] InstructorSubjectPreference { get; set; } = new int[0, 0];
        public int[,] InstructorSlot { get; set; } = new int[0, 0];
        public int[,] InstructorSlotPreference { get; set; } = new int[0, 0];
        public List<(int, int, int)> InstructorPreassign { get; set; } = new List<(int, int, int)>();
        public int[] InstructorQuota { get; set; } = Array.Empty<int>();
        public int[] InstructorMinQuota { get; set; } = Array.Empty<int>();
        public int[] TaskSubjectMapping { get; set; } = Array.Empty<int>();
        public int[] TaskSlotMapping { get; set; } = Array.Empty<int>();
        public int[] TaskAreaMapping { get; set; } = Array.Empty<int>();
        public int[,] AreaDistance { get; set; } = new int[0, 0];
        public int[,] AreaSlotCoefficient { get; set; } = new int[0, 0];
        public void SetRange()
        {
            AllSubjects = Enumerable.Range(0, NumSubjects).ToArray();
            AllTasks = Enumerable.Range(0, NumTasks).ToArray();
            AllSlots = Enumerable.Range(0, NumSlots).ToArray();
            AllDays = Enumerable.Range(0, NumDays).ToArray();
            AllTimes = Enumerable.Range(0, NumTimes).ToArray();
            AllSegments = Enumerable.Range(0, NumSegments).ToArray();
            AllInstructors = Enumerable.Range(0, NumInstructors).ToArray();
            if (NumBackupInstructors > 0)
            {
                AllInstructorsWithBackup = Enumerable.Range(0, NumInstructors + 1).ToArray();
                if (InstructorQuota.Length <= NumInstructors)
                {
                    InstructorQuota = InstructorQuota.Concat(new int[] { NumBackupInstructors }).ToArray();
                    InstructorMinQuota = InstructorMinQuota.Concat(new int[] { 0 }).ToArray();
                }
                else
                {
                    InstructorQuota[NumInstructors] = NumBackupInstructors;
                    InstructorMinQuota[NumInstructors] = 0;
                }
            }
            else
            {
                AllInstructorsWithBackup = Enumerable.Range(0, NumInstructors).ToArray();
            }
        }
    }
}
