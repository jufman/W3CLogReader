using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W3CLogReader.Core.Objects
{
    public class GroupedLogs
    {
        public string Username { get; set; }

        public List<Log> Logs { get; set; } = new List<Log>();

    }
}
