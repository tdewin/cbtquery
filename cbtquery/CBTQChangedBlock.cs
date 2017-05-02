using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cbtquery
{
    public class CBTQChangedBlock
    {
        public CBTQChangedBlock(long offset, long length, long fiximpact)
        {
            this.offset = offset;
            this.length = length;
            this.fiximpact = fiximpact;
        }

        public long offset { get; set; }
        public long length { get; set; }
        public long fiximpact { get; set; }


    }
}
