using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cbtquery
{
    public class CBTQListedDisk
    {
        public CBTQListedDisk(string label, long key, string cbtTimestamp, long diskSizeKb)
        {
            this.label = label;
            this.key = key;
            this.cbtTimestamp = cbtTimestamp;
            this.diskSizeKb = diskSizeKb;
        }

        public String label { get; set; }
        public long key { get; set; }
        public String cbtTimestamp { get; set; }
        public long diskSizeKb { get; set; }

    }
}
