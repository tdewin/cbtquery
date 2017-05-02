using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cbtquery
{
    public class CBTQChangeCalc
    {
        public long offset { get; set; }
        public long length { get; set; }
        public List<CBTQChangedBlock> cbtreal { get; set; }
        public List<CBTQChangedBlock> cbtfix { get; set; }
        public long blocksize { get; set; }
        public long cbtrealtotal { get; set; }
        public double cbtrealtotalmb { get; set; }
        public long cbtfixtotal { get; set; }
        public double cbtfixtotalmb { get; set; }
        public long cbtfixchangedblocks { get; set; }
        public bool[] cbtfixbitmap { get; set; }
        public long cbtrealminblock { get; set; }
        public bool[] cbtrealbitmap { get; set; }
        public long cbtrealchangedblocks { get; internal set; }

        public CBTQChangeCalc()
        {
            cbtreal = new List<CBTQChangedBlock>();
            cbtfix = new List<CBTQChangedBlock>();

            cbtrealbitmap = null;
            cbtfixbitmap = null;

            cbtrealminblock = -1;
            blocksize = -1;
            

        }

    }
}
