using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vim25Api;

namespace cbtquery
{
    public class CBTQVMwithRef
    {
        public CBTQVMwithRef(string name, ManagedObjectReference mor)
        {
            Name = name;
            Mor = mor;
        }

        public string Name { get; set; }
        public ManagedObjectReference Mor { get; set; }


    }
}
