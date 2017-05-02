
using AppUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vim25Api;

namespace cbtquery
{

    
    
    
    
    class Program
    {
        static void Main(string[] args)
        {

            //not to hack original apputil
            string stockesx = Environment.GetEnvironmentVariable("vi_easyserver");
            if (stockesx != null && stockesx != "")
            {
                string[] newargs = new string[args.Length + 4];
                for (int i=0;i < args.Length;i++) { newargs[i] = args[i]; }
                newargs[args.Length] = "--disablesso";
                newargs[args.Length + 1] = "--ignorecert";
                newargs[args.Length + 2] = "--url";
                newargs[args.Length + 3] = String.Format("https://{0}/sdk",stockesx);
                args = newargs;
            }


            var extraoptions = new List<AppUtil.OptionSpec>();
            extraoptions.Add(new AppUtil.OptionSpec("vmmoref", "string", 0, "moref of VM", null));
            extraoptions.Add(new AppUtil.OptionSpec("snapmoref", "string", 0, "moref of VM", null));
            extraoptions.Add(new AppUtil.OptionSpec("diskid", "int", 0, "id of Disk", "-1"));
            extraoptions.Add(new AppUtil.OptionSpec("timestamp", "string", 0, "Disk CBT timestamp", null));
            extraoptions.Add(new AppUtil.OptionSpec("blocksize", "int", 0, "blocksize in kb", "1024"));
            extraoptions.Add(new AppUtil.OptionSpec("action", "string", 0, "lsvm|mksnap|mksnaplsdisk|rmsnap|lssnap|lsdisk|lscb|diskmon", "lsvm"));
            extraoptions.Add(new AppUtil.OptionSpec("snapname", "string", 0, "snapshot name", "tempcbtquerysnap"));
            extraoptions.Add(new AppUtil.OptionSpec("vm", "string", 0, "vm inventory name", ""));
            extraoptions.Add(new AppUtil.OptionSpec("cbt", "int", 0, "enable cbt/disable", "1"));
            extraoptions.Add(new AppUtil.OptionSpec("mode", "int", 0, "specific command mode (int)", "0"));

            try
            {
                var cb = AppUtil.AppUtil.initialize("CBTTrack", extraoptions.ToArray(), args);
                cb.connect();
                

                var cbtq = new CBTQuery(cb);


                var vmmoref = cb.get_option("vmmoref");
                var snapname = cb.get_option("snapname");
                var snapmoref = cb.get_option("snapmoref");
                var vmname = cb.get_option("vm");

                Int32 diskid = -1;
                try { diskid = Int32.Parse(cb.get_option("diskid")); } catch (Exception e) { System.Console.WriteLine(e.Message); }
                 
                var timestamp = cb.get_option("timestamp");

                Int32 cbtenable = -1;
                try { cbtenable = Int32.Parse(cb.get_option("cbt")); } catch (Exception e) { System.Console.WriteLine(e.Message); }

                Int32 blocksize = -1;
                try { blocksize = Int32.Parse(cb.get_option("blocksize")); } catch (Exception e) { System.Console.WriteLine(e.Message); }

                Int32 mode = 0;
                try { mode = Int32.Parse(cb.get_option("mode")); } catch (Exception e) { System.Console.WriteLine(e.Message); }

                switch (cb.get_option("action").ToLower())
                {

                    case "lsvm":
                        cbtq.LsVm();
                        break;
                    case "chcbt":
                        if (vmmoref != null && vmmoref != "" && cbtenable != -1)
                        {
                            cbtq.ChCBT(vmmoref, cbtenable);
                        }
                        else
                        {
                            System.Console.WriteLine("Please supply vmmoref first and that snapsname is not empty");
                        }
                        break;
                    case "resetcbt":
                        if (vmmoref != null && vmmoref != "")
                        {
                            cbtq.ResetCBT(vmmoref);
                        }
                        else
                        {
                            System.Console.WriteLine("Please supply vmmoref first and that snapsname is not empty");
                        }
                        break;
                    case "mksnap":
                        if (vmmoref != null && vmmoref != "" && snapname != null && snapname != "")
                        {
                            cbtq.MkSnap(vmmoref,snapname);
                        }
                        else
                        {
                            System.Console.WriteLine("Please supply vmmoref first and that snapsname is not empty");
                        }
                        break;
                    case "mksnaplsdisk":
                        if (vmmoref != null && vmmoref != "" && snapname != null && snapname != "")
                        {
                            cbtq.MkSnapWithList(vmmoref, snapname,true,false,0);
                        }
                        else
                        {
                            System.Console.WriteLine("Please supply vmmoref first and that snapsname is not empty");
                        }
                        break;
                    case "mkrmsnap":
                        if (vmmoref != null && vmmoref != "" && snapname != null && snapname != "")
                        {
                            cbtq.MkSnapWithList(vmmoref, snapname, false, true,0);
                        }
                        else
                        {
                            System.Console.WriteLine("Please supply vmmoref first and that snapsname is not empty");
                        }
                        break;
                    case "lssnap":
                        if (vmmoref != null && vmmoref != "")
                        {
                            cbtq.LsSnap(vmmoref);
                        }
                        else
                        {
                            System.Console.WriteLine("Please supply vmmoref");
                        }
                        break;
                    case "lsdisk":
                        if (snapmoref != null && snapmoref != "")
                        {
                            cbtq.LsDisk(snapmoref);
                        } else if (vmmoref != null && vmmoref != "")
                        {
                            cbtq.LsDiskLatest(vmmoref);
                        }
                        else
                        {
                            System.Console.WriteLine("Please supply  snapmoref or vmmoref"); 
                        }
                        break;
                    case "rmsnap":
                        if (snapmoref != null && snapmoref != "")
                        {
                            cbtq.RmSnapMoRef(snapmoref);
                        }
                        else if (vmmoref != "" && snapname != "")
                        {
                            cbtq.RmSnap(vmmoref, snapname);
                        }
                        else
                        {
                            System.Console.WriteLine("Please supply (snapmoref) or (vmmoref and make sure snapsname is not empty)");
                        }
                        break;
                    case "lscb":
                        if (snapmoref != null && snapmoref != "" && diskid != -1 && timestamp != null && timestamp != "")
                        {
                            cbtq.LsChangedBlock(snapmoref,diskid,timestamp);
                        } 
                        else
                        {
                            System.Console.WriteLine("Please supply  --snapmoref, --diskid, CBT timestamp");
                        }
                        break;
                    case "lscbfix":
                        if (snapmoref != null && snapmoref != "" && diskid != -1 && timestamp != null && timestamp != "" && blocksize != -1)
                        {
                            cbtq.LsFixedBlockChangedBlock(snapmoref, diskid, timestamp,blocksize);
                        }
                        else
                        {
                            System.Console.WriteLine("Please supply  --snapmoref, --diskid, CBT timestamp");
                        }
                        break;
                    case "diskmon":
                        if (vmmoref != null && vmmoref != "" && diskid != -1  && blocksize != -1)
                        {
                            
                            cbtq.DiskMonExtended(vmmoref, diskid, blocksize,mode);
                        }
                        else
                        {
                            System.Console.WriteLine("Please supply  --vmmoref --diskid --blocksize");
                        }
                        break;
                    case "prepmon":
                        if (vmname != null && vmname != "")
                        {

                            cbtq.PrepMon(vmname);
                        }
                        else
                        {
                            System.Console.WriteLine("Please supply  --vm");
                        }
                        break;
                    default:
                        System.Console.WriteLine("Unknown --action");
                        break;

                }


                cb.disConnect();
            } catch (Exception e)
            {
                System.Console.WriteLine("Something went foobar " + e.Message+" "+e.StackTrace);
            }
        }
    }
}
