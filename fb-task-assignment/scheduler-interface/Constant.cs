using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scheduler_interface
{
    public static class Constant
    {
        public static string EXPORT_PATH_PATTEN = 
            @"^([A-Za-z]:|\\{2}([-\w]+|((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|"
            + @"2[0-4][0-9]|[01]?[0-9][0-9]?))\\(([^""*/:?|<>\\,;[\]+=.\x00-\x20]|\.[.\x20]*["
            + @"^""*/:?|<>\\,;[\]+=.\x00-\x20])([^""*/:?|<>\\,;[\]+=\x00-\x1F]*[^""*/:?|<>\\,"
            + @";[\]+=\x00-\x20])?))\\([^""*/:?|<>\\.\x00-\x20]([^""*/:?|<>\\\x00-\x1F]*[^""*"
            + @"/:?|<>\\.\x00-\x20])?\\)*$";

        public static string XLSX_PATTERN = @"^.*\.xlsx$";
    }
}
