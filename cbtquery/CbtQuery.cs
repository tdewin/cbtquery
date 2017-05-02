using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;


using Vim25Api;
using AppUtil;

namespace cbtquery
{
    public class CBTQuery
    {


        public AppUtil.AppUtil cb { get; set; }
        public AppUtil.ServiceUtil su { get; set; }
        public VimService vimservice { get; private set; }

        public CBTQuery(AppUtil.AppUtil cb)
        {
            this.cb = cb;
            
            if (cb != null)
            {
                this.su = cb.getServiceUtil();
                this.vimservice = cb.getConnection()._service;
            } else
            {
                this.su = null;
                this.vimservice = null;
            }

        }

        public void LsVm()
        {
            var vms = cb._svcUtil.getEntitiesByType("VirtualMachine", null);
            foreach (KeyValuePair<ManagedObjectReference, Dictionary<string, object>> vm in vms)
            {
                System.Console.WriteLine(String.Format("{0,5} {1}", vm.Key.Value, vm.Value["name"]));

            }

        }

        public void PrepMon(string name)
        {
            var vm = cb._svcUtil.getEntityByName("VirtualMachine", name);
            if (vm != null)
            {
                System.Console.WriteLine(String.Format("Found VM with name {0} and mor {1}", name, vm.Value));
                VirtualMachineConfigInfo config = (VirtualMachineConfigInfo)cb.getServiceUtil().GetDynamicProperty(vm, "config");

                if (config != null)
                {
                    var devs = config.hardware.device;
                    for (int i = 0; i < devs.Length; i++)
                    {
                        var dev = devs[i];

                        if (dev.GetType() == typeof(Vim25Api.VirtualDisk))
                        {
                            var disk = (Vim25Api.VirtualDisk)dev;
                            System.Console.WriteLine("");
                            System.Console.WriteLine(String.Format("Found disk {0,15} with id {1,8} and size {2,20} : ", disk.deviceInfo.label, disk.key, disk.capacityInKB));
                            System.Console.WriteLine(String.Format("  $ cbtquery --action diskmon --vmmoref {0} --diskid {1} --blocksize 1024", vm.Value, disk.key));
                        }
                    }
                    System.Console.WriteLine("");
                } else
                {
                    System.Console.WriteLine("Could not query vm config");
                }
            } else
            {
                System.Console.WriteLine("VM not found");
            }

        }

        public CBTQVMwithRef GetVm(string vmid)
        {
            var vmMor = new ManagedObjectReference();
            vmMor.type = "VirtualMachine";
            vmMor.Value = vmid;

            var name = cb.getServiceUtil().getProp(vmMor, "name");

            CBTQVMwithRef vr = null;
            if (name != null)
            {
                vr = new CBTQVMwithRef(name, vmMor);
            }

            return vr;
        }
        public void ResetCBT(string vmmoref)
        {
            var vr = GetVm(vmmoref);
            if (vr != null)
            {

                ChCBTExec(vr.Mor, false);
                System.Console.WriteLine("Disabled CBT");
                MkSnapWithList(vmmoref, "cbtreset", false, true,100);
                System.Console.WriteLine("Making / Deleting Snapshot");
                ChCBTExec(vr.Mor, true);
                System.Console.WriteLine("Enabling CBT");
            }
        }

        public void ChCBTExec(ManagedObjectReference vm, bool cbtbool)
        {
            VirtualMachineConfigSpec spec = new VirtualMachineConfigSpec();
            spec.changeTrackingEnabledSpecified = true;
            spec.changeTrackingEnabled = cbtbool;

            ManagedObjectReference taskMor = vimservice.ReconfigVM_Task(vm, spec);
            String res = cb.getServiceUtil().WaitForTask(taskMor);
            if (res.Equals("sucess"))
            {
                VirtualMachineConfigInfo realcfg = (VirtualMachineConfigInfo)cb.getServiceUtil().GetDynamicProperty(vm, "config");
                if (realcfg != null)
                {
                    if(realcfg.changeTrackingEnabled != cbtbool)
                    {
                        throw new Exception("Changeblocktracking updated but config does not reflect it");
                    } 
                } else
                {
                    throw new Exception("Executed CBT change to " + cbtbool+" but did not succeed in asking update");
                }

            } else
            {
                throw new Exception("Not Succesful cbt" + res);
            }
        }
        public void ChCBT(string vmmoref, int cbtenable)
        {
            var vr = GetVm(vmmoref);

            bool cbtbool = (cbtenable == 1);

            if (vr != null)
            {

                //VirtualMachineConfigSpec spec = new VirtualMachineConfigSpec();
                //spec.changeTrackingEnabledSpecified = true;
                //spec.changeTrackingEnabled = cbtbool;

                //ManagedObjectReference taskMor = vimservice.ReconfigVM_Task(vr.Mor, spec);
                //String res = cb.getServiceUtil().WaitForTask(taskMor);
                //if (res.Equals("sucess"))
                //{
                //    Console.WriteLine("Command Executed with Success, checking real status");
                //    VirtualMachineConfigInfo realcfg = (VirtualMachineConfigInfo)cb.getServiceUtil().GetDynamicProperty(vr.Mor, "config");
                //    if (realcfg != null)
                //    {
                //        Console.WriteLine("CBT Enabled " + realcfg.changeTrackingEnabled);
                //    }
                //    else { Console.WriteLine("Could not get config"); }

                //}
                //else
                //{
                //    Console.WriteLine("Was not able to changed cbt " + res);
                //}

                ChCBTExec(vr.Mor, cbtbool);
                Console.WriteLine("Changed succesfully");


              
            }
            else
            {
                Console.WriteLine("Could not find VM with this MOR");
            }
        }
        private ManagedObjectReference MkSnapExecute(CBTQVMwithRef vr, string snapname)
        {
            if (vr != null)
            {
                var d = DateTime.Now;
                ManagedObjectReference taskMor = cb.getConnection()._service.CreateSnapshot_Task(vr.Mor, snapname, String.Format("CBT Query Snapshot {0}", d.ToUniversalTime()), false, false);
                String res = cb.getServiceUtil().WaitForTask(taskMor);
                if (res.Equals("sucess"))
                {
                    var info = (TaskInfo)cb.getServiceUtil().GetDynamicProperty(taskMor, "info");
                    var snapshotMor = (ManagedObjectReference)info.result;
                    return snapshotMor;
                }
                else
                {
                    throw new Exception("Snapshot Creation failed " + res);
                }
            }
            throw new Exception("Could not find VM");
        }
        public void MkSnap(string vmmoref, string snapname)
        {
            MkSnapWithList(vmmoref, snapname, false,false,0);
        }


        public void MkSnapWithList(string vmmoref, string snapname, bool dolist,bool instantremove,int waittime)
        {
            var vr = GetVm(vmmoref);
            bool removechildren = false;
            bool consolidate = true;

            if (vr != null)
            {
                System.Console.WriteLine("Virtual Machine Found : " + vr.Name);
                System.Console.WriteLine("Making snapshot now");
                try
                {
                    var snapshotMor = MkSnapExecute(vr, snapname);
                    Console.WriteLine("Created Successfully : " + snapname + " id " + snapshotMor.Value);
                    if (snapshotMor != null)
                    {
                        if (dolist)
                        {
                            LsDisk(snapshotMor.Value);
                        }
                        if (instantremove)
                        {
                            //give time to system to other session to pick up
                            if (waittime == 0)
                            {
                                waittime = 1500;
                            }
                            System.Threading.Thread.Sleep(waittime);
                            ManagedObjectReference taskMor = cb.getConnection()._service.RemoveSnapshot_Task(snapshotMor, removechildren, consolidate, false);
                            String res = cb.getServiceUtil().WaitForTask(taskMor);
                            if (res.Equals("sucess"))
                            {

                                Console.WriteLine("Removed Successfully : " + snapshotMor.Value);
                            }
                        }
                    }
                }
                catch (Exception e) { Console.WriteLine("Failed : " + e.Message); }
            }
            else
            {
                Console.WriteLine("Could not find VM with this MOR");
            }

        }

        public void RmSnapMoRef(String snapmoref)
        {
            bool removechildren = false;
            bool consolidate = true;

            ManagedObjectReference snapmor = new ManagedObjectReference();
            snapmor.type = "VirtualMachineSnapshot";
            snapmor.Value = snapmoref;

            ManagedObjectReference vmref = null;
            try
            {
                vmref = cb.getServiceUtil().GetMoRefProp(snapmor, "vm");

            }
            catch
            {
                Console.WriteLine("Could not find snapshot");
            }
            if (vmref != null)
            {
                var vmname = cb.getServiceUtil().getProp(vmref, "name");
                System.Console.WriteLine("Found snapshot on VM : " + vmname);
                ManagedObjectReference taskMor = cb.getConnection()._service.RemoveSnapshot_Task(snapmor, removechildren, consolidate, false);
                String res = cb.getServiceUtil().WaitForTask(taskMor);
                if (res.Equals("sucess"))
                {

                    Console.WriteLine("Removed Successfully : " + snapmoref);
                }
            }
        }


        public void LsSnap(string vmmoref)
        {
            var vr = GetVm(vmmoref);
            if (vr != null)
            {
                System.Console.WriteLine("VM Name : " + vr.Name);
                var snapinfo = (VirtualMachineSnapshotInfo)cb.getServiceUtil().GetDynamicProperty(vr.Mor, "snapshot");
                if (snapinfo != null)
                {
                    LsSnapTree(snapinfo.rootSnapshotList, 0);
                }
                else
                {
                    System.Console.WriteLine("Could not find any snapshot");
                }
            }
            else
            {
                System.Console.WriteLine("Could not find VM");
            }
        }

        public void LsDiskLatest(string vmmoref)
        {
            var vr = GetVm(vmmoref);
            if (vr != null)
            {
                System.Console.WriteLine("VM Name : " + vr.Name);
                var snapinfo = (VirtualMachineSnapshotInfo)cb.getServiceUtil().GetDynamicProperty(vr.Mor, "snapshot");
                if (snapinfo != null)
                {
                    Console.WriteLine("Latest Snapshot " + snapinfo.currentSnapshot.Value);
                    LsDisk(snapinfo.currentSnapshot.Value);
                }
                else
                {
                    System.Console.WriteLine("No snapshot on VM");
                }

            }
            else
            {
                System.Console.WriteLine("Could not find VM");
            }
        }
        public void LsDisk(string snapmoref)
        {
            try
            {
                Dictionary<long, CBTQListedDisk> disks = LsDiskExecute(snapmoref);
                foreach (CBTQListedDisk disk in disks.Values)
                {
                    System.Console.WriteLine(String.Format("{0} Disk ID : {1} CBT Timestamp : \"{2}\" Size : {3} KB", disk.label, disk.key, disk.cbtTimestamp, disk.diskSizeKb));
                }

            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.Message);
            }
        }
        public Dictionary<long, CBTQListedDisk> LsDiskExecute(string snapmoref)
        {
            ManagedObjectReference snapmor = new ManagedObjectReference();
            snapmor.type = "VirtualMachineSnapshot";
            snapmor.Value = snapmoref;

            Dictionary<long, CBTQListedDisk> disks = new Dictionary<long, CBTQListedDisk>();

            VirtualMachineConfigInfo snapconfig = null;
            try
            {
                snapconfig = (VirtualMachineConfigInfo)cb.getServiceUtil().GetDynamicProperty(snapmor, "config");

            }
            catch
            {
                throw new Exception("Could not find snapshot");
            }
            if (snapconfig != null)
            {


                var devs = snapconfig.hardware.device;
                for (int i = 0; i < devs.Length; i++)
                {
                    var dev = devs[i];

                    if (dev.GetType() == typeof(Vim25Api.VirtualDisk))
                    {
                        var disk = (Vim25Api.VirtualDisk)dev;
                        string tms = null;

                        if (disk.backing.GetType() == typeof(VirtualDiskFlatVer2BackingInfo))
                        {
                            var backing = (VirtualDiskFlatVer2BackingInfo)disk.backing; tms = backing.changeId;
                        }
                        else if (disk.backing.GetType() == typeof(VirtualDiskRawDiskMappingVer1BackingInfo))
                        {
                            var backing = (VirtualDiskRawDiskMappingVer1BackingInfo)disk.backing; tms = backing.changeId;
                        }
                        else if (disk.backing.GetType() == typeof(VirtualDiskSparseVer2BackingInfo))
                        {
                            var backing = (VirtualDiskSparseVer2BackingInfo)disk.backing; tms = backing.changeId;
                        }

                        if (tms != null)
                        {
                            disks.Add(disk.key, new CBTQListedDisk(dev.deviceInfo.label, disk.key, tms, disk.capacityInKB));
                        }
                    }

                }
            }
            return disks;
        }

        public void LsChangedBlock(string snapmoref, int diskid, string timestamp)
        {
            ManagedObjectReference snapmor = new ManagedObjectReference();
            snapmor.type = "VirtualMachineSnapshot";
            snapmor.Value = snapmoref;

            ManagedObjectReference vmref = null;
            try
            {
                vmref = cb.getServiceUtil().GetMoRefProp(snapmor, "vm");

            }
            catch
            {
                Console.WriteLine("Could not find snapshot");
            }
            if (vmref != null)
            {
                var res = vimservice.QueryChangedDiskAreas(vmref, snapmor, diskid, 0, timestamp);
                if (res != null)
                {
                    CBTQChangeCalc c = LsFixedBlockCalc(res, 0,false);


                    System.Console.WriteLine(String.Format("Disk Offset : {0}", c.offset));
                    System.Console.WriteLine(String.Format("Disk Length : {0}", c.length));
                    System.Console.WriteLine("###########################");


                    if (c.cbtreal.Count > 0)
                    {


                        foreach (CBTQChangedBlock block in c.cbtreal)
                        {
                            System.Console.WriteLine("Start {0,-20} Length {1,-20} ", block.offset, block.length);
                        }

                        System.Console.WriteLine("###########################");
                        System.Console.WriteLine(String.Format("Total Bytes CBT Flagged \t: {0}", c.cbtrealtotal));
                        System.Console.WriteLine(String.Format("Total Bytes CBT Flagged (MB)\t: {0}", c.cbtrealtotalmb));
                        System.Console.WriteLine("###########################");

                    }
                    else
                    {
                        System.Console.WriteLine("Could not find changed blocks");
                    }
                }
            }
        }


        public CBTQChangeCalc LsFixedBlockCalc(DiskChangeInfo res, long blocksizekb,bool makecbtbitmap)
        {
            CBTQChangeCalc calcinfo = null;






            if (res != null)
            {
                calcinfo = new CBTQChangeCalc();
                long blocksize = blocksizekb * 1024;
                bool[] cbtfixbitmap = null;
                if (blocksize > 0)
                {
                    //+1 for misalignment on the block level, normally does not happen but just in case
                    cbtfixbitmap = new bool[(res.length / blocksize) + 1];
                    for (var i = 0; i < cbtfixbitmap.Length; i++) { cbtfixbitmap[i] = false; }
                }
                bool[] cbtbitmap = null;
                long cbtblocksizemin = 1099511627776;


                calcinfo.offset = res.startOffset;
                calcinfo.length = res.length;
                calcinfo.blocksize = blocksize;

                //when flagging, instead of jumper per block, we will jump per maxjumpblockopt except at the end
                //this should significantely speed up calculation of the bitmap
                //to big is also not good because at the end more subjump will have to occure
                long maxjumpblockopt = blocksize / 4;
                long endmaxjumpblockopt = 1024;


                if (res.changedArea != null && res.changedArea.Length > 0)
                {
                    long changedarea = 0;

                    for (var i = 0; i < res.changedArea.Length; i++)
                    {
                        if (res.changedArea[i].length < cbtblocksizemin)
                        {
                            cbtblocksizemin = res.changedArea[i].length;
                        }
                    }

                    //make sure that jumps are smaller than the blocksize and the variable block size in CBT

                    long oneforthblock = cbtblocksizemin / 4;
                    if (oneforthblock < maxjumpblockopt)
                    {
                        maxjumpblockopt = oneforthblock;
                    }
                    System.Console.WriteLine("Jump size used: " + maxjumpblockopt);

                    if (makecbtbitmap)
                    {


                        if (cbtblocksizemin != 1099511627776 && cbtblocksizemin > 0)
                        {
                            cbtbitmap = new bool[(res.length / cbtblocksizemin) + 1];
                            for (var i = 0; i < cbtbitmap.Length; i++) { cbtbitmap[i] = false; }
                        }
                        else
                        {
                            makecbtbitmap = false;
                            System.Console.WriteLine("Unusual block size, should not happen");
                        }
                    }



                    for (var i = 0; i < res.changedArea.Length; i++)
                    {
                        var changedblock = res.changedArea[i];
                        var start = changedblock.start;
                        var length = changedblock.length;



                        if (blocksize > 0)
                        {

                            long end = (start + length);

                            long flagged = 0;

                            long cbtflagged = 0;
                            //System.Console.WriteLine(fixstart);
                            //System.Console.WriteLine(fixend);
                            long prevtblock = -1;

                            long prevcbttblock = -1;

                            for (long m = start; m < end;)
                            {
                                long touchingblock = m / blocksize;

                                if (touchingblock >= 0 && touchingblock < cbtfixbitmap.Length && touchingblock != prevtblock)
                                {
                                    cbtfixbitmap[touchingblock] = true;
                                    flagged++;
                                    prevtblock = touchingblock;
                                }

                                if (makecbtbitmap)
                                {
                                    long cbttblock = m / cbtblocksizemin;
                                    if (cbttblock >= 0 && cbttblock < cbtbitmap.Length && cbttblock != prevcbttblock)
                                    {
                                        cbtbitmap[cbttblock] = true;
                                        cbtflagged++;
                                        prevcbttblock = cbttblock;
                                    }
                                }

                                //big continious region jumping
                                //if at the end, make sure we jump per 1kb to match the size at that point
                                //should give the most accurate prediction
                                long newm = m + maxjumpblockopt;
                                if (newm < end)
                                {
                                    m = newm;
                                }
                                else
                                {
                                    m += endmaxjumpblockopt;
                                }
                            }

                            //System.Console.WriteLine("Start {0,-20} Length {1,-20} Flagged Fixed Blocks {2,-5}", changedblock.start, changedblock.length, flagged);

                            calcinfo.cbtreal.Add(new CBTQChangedBlock(changedblock.start, changedblock.length, flagged));
                        }
                        else
                        {

                            calcinfo.cbtreal.Add(new CBTQChangedBlock(changedblock.start, changedblock.length, 0));
                        }
                        changedarea += changedblock.length;
                    }

                    calcinfo.cbtrealtotal = changedarea;
                    calcinfo.cbtrealtotalmb = Math.Round((((double)changedarea) / 1024 / 1024), 2);

                    if (makecbtbitmap)
                    {
                        calcinfo.cbtrealminblock = cbtblocksizemin;
                        calcinfo.cbtrealbitmap = cbtbitmap;

                        long cbtrealflagged = 0;
                        for (var i = 0; i < cbtbitmap.Length; i++)
                        {
                            if (cbtbitmap[i])
                            {
                                cbtrealflagged++;
                            }
                        }
                        calcinfo.cbtrealchangedblocks = cbtrealflagged;

                    }

                    if (blocksize > 0)
                    {
                        long fixedflagged = 0;
                        for (var i = 0; i < cbtfixbitmap.Length; i++)
                        {
                            if (cbtfixbitmap[i])
                            {
                                long begin = i * blocksize;
                                for (; i < cbtfixbitmap.Length && cbtfixbitmap[i]; i++) { fixedflagged += 1; };
                                long end = i * blocksize;

                                calcinfo.cbtfix.Add(new CBTQChangedBlock(begin, (end - begin), (end - begin) / blocksize));

                            }
                        }


                        long fixedflaggedb = fixedflagged * blocksize;
                        calcinfo.cbtfixtotal = fixedflaggedb;
                        calcinfo.cbtfixtotalmb = Math.Round((((double)fixedflaggedb) / 1024 / 1024), 2);
                        calcinfo.cbtfixchangedblocks = fixedflagged;
                        calcinfo.cbtfixbitmap = cbtfixbitmap;


                    }
                }
                else
                {
                    calcinfo.cbtfixchangedblocks = 0;
                    calcinfo.cbtfixtotal = 0;
                    calcinfo.cbtfixtotalmb = 0;
                    calcinfo.cbtfixbitmap = cbtfixbitmap;

                    if (makecbtbitmap)
                    {

                        calcinfo.cbtrealminblock = 64 * 1024;
                        calcinfo.cbtrealbitmap = new bool[res.length / calcinfo.cbtrealminblock];
                        for (var i = 0; i < calcinfo.cbtrealbitmap.Length; i++) { calcinfo.cbtrealbitmap[i] = false; }
                        calcinfo.cbtrealtotal = 0;
                        calcinfo.cbtrealtotalmb = 0;
                        calcinfo.cbtrealchangedblocks = 0;



                    }
                }
            }
            return calcinfo;
        }

        public VirtualMachineSnapshotTree FindSnapInfo(VirtualMachineSnapshotTree[] tree, String snapmoref)
        {
            for (int i = 0; i < tree.Length; i++)
            {
                VirtualMachineSnapshotTree leaf = tree[i];
                if (leaf.snapshot.Value.Equals(snapmoref))
                {
                    return leaf;
                }
                else if (tree[i].childSnapshotList.Length > 0)
                {
                    VirtualMachineSnapshotTree testchild = FindSnapInfo(leaf.childSnapshotList, snapmoref);
                    if (testchild != null)
                    {
                        return testchild;
                    }
                }
            }
            return null;
        }

        public void DiskMonPrint(CBTQChangeCalc c,string vmmoref,int display)
        {
            


            System.Console.WriteLine(String.Format("Disk Offset : {0,30} Length : {1,30}", c.offset, c.length));



            if (c.cbtreal.Count > 0)
            {

                if (display == 1)
                {
                    foreach (CBTQChangedBlock block in c.cbtreal)
                    {
                        System.Console.WriteLine("Start {0,-20} Length {1,-20} Flagged Fixed Blocks {2,-5}", block.offset, block.length, block.fiximpact);
                    }

                    System.Console.WriteLine("");

                    foreach (CBTQChangedBlock block in c.cbtfix)
                    {
                        System.Console.WriteLine("Fix Blocks Start {0,-20} Length {1,-20} Fixed Blocks {2,-5}", block.offset, block.length, block.fiximpact);
                    }

                    System.Console.WriteLine("");
                }
                else if (display == 2)
                {
                    MakePNG(c, vmmoref);
                }
                System.Console.WriteLine(String.Format(
                        "{0,14} | {1,10} | {2,10} | {3,14} | {4,10}", "Bytes CBT", "MBytes CBT", "Blocks Fix", "Bytes Fix", "MBytes Fix"
                    ));
                System.Console.WriteLine(String.Format(
                        "{0,14} | {1,10} | {2,10} | {3,14} | {4,10}", c.cbtrealtotal, c.cbtrealtotalmb, c.cbtfixchangedblocks, c.cbtfixtotal, c.cbtfixtotalmb
                    ));
                System.Console.WriteLine("");


            }
            else
            {
                System.Console.WriteLine("Could not find changed blocks");
                if (display == 2)
                {
                    MakePNG(c, vmmoref);
                }
            }
        }

        public void DiskMon(string vmmoref, int diskid, long blocksizekb)
        {
            DiskMonExtended(vmmoref, diskid, blocksizekb, 0);
        }
        public void DiskMonExtended(string vmmoref, int diskid, long blocksizekb, int display)
        {
            var vr = GetVm(vmmoref);
            if (vr != null)
            {
                ManagedObjectReference csnap = null;
                DateTime cdate = new DateTime(0);
                String changeId = null;

                bool allok = true;
                while (allok)
                {
                    var snapinfo = (VirtualMachineSnapshotInfo)cb.getServiceUtil().GetDynamicProperty(vr.Mor, "snapshot");
                    if (snapinfo != null)
                    {
                        ManagedObjectReference querycsnap = snapinfo.currentSnapshot;


                        if (querycsnap != null)
                        {
                            VirtualMachineSnapshotTree info = FindSnapInfo(snapinfo.rootSnapshotList, querycsnap.Value); ;
                            if (info != null)
                            {
                                if (csnap != null)
                                {
                                    if (!csnap.Value.Equals(querycsnap.Value) && info.createTime > cdate)
                                    {
                                        System.Console.WriteLine("New snapshot '" + info.name + "' detected, calculating diff");
                                        Dictionary<long, CBTQListedDisk> disks = LsDiskExecute(querycsnap.Value);
                                        if (disks.ContainsKey(diskid))
                                        {
                                            csnap = querycsnap;
                                            cdate = info.createTime;
                                            System.Console.WriteLine("New CBT Changeid " + disks[diskid].cbtTimestamp);


                                            var res = vimservice.QueryChangedDiskAreas(vr.Mor, csnap, diskid, 0, changeId);
                                            if (res != null)
                                            {
                                                CBTQChangeCalc c = LsFixedBlockCalc(res, blocksizekb, (display == 2));
                                                DiskMonPrint(c, vmmoref, display);
                                            }



                                            changeId = disks[diskid].cbtTimestamp;
                                        }
                                        else
                                        {
                                            allok = false;
                                            System.Console.WriteLine("VM does not contain disk id, halting");
                                        }
                                    }
                                }
                                else
                                {
                                    System.Console.WriteLine("Detected First Reference Snap '" + info.name + "'");
                                    Dictionary<long, CBTQListedDisk> disks = LsDiskExecute(querycsnap.Value);
                                    if (disks.ContainsKey(diskid))
                                    {
                                        csnap = querycsnap;
                                        cdate = info.createTime;
                                        changeId = disks[diskid].cbtTimestamp;

                                        System.Console.WriteLine("CBT Changeid " + changeId);

                                        var res = vimservice.QueryChangedDiskAreas(vr.Mor, csnap, diskid, 0, "*");
                                        if (res != null)
                                        {
                                            CBTQChangeCalc c = LsFixedBlockCalc(res, blocksizekb, (display == 2));
                                            DiskMonPrint(c, vmmoref,display);
                                            
                                        }

                                    }
                                    else
                                    {
                                        allok = false;
                                        System.Console.WriteLine("VM does not contain disk id, halting");
                                    }
                                }

                                if (info.name.Equals("stopdiskmon"))
                                {
                                    System.Console.WriteLine("Detected 'stopdiskmon' snapshot");
                                    allok = false;
                                }
                            }
                        }
                    }
                    System.Threading.Thread.Sleep(500);
                }
            }
            else
            {
                System.Console.WriteLine("Could not find VM");
            }
        }

        //Copy paste from : http://www.somacon.com/p576.php
        public string GetBytesReadable(long i)
        {
            // Get absolute value
            long absolute_i = (i < 0 ? -i : i);
            // Determine the suffix and readable value
            string suffix;
            double readable;
            if (absolute_i >= 0x1000000000000000) // Exabyte
            {
                suffix = "EB";
                readable = (i >> 50);
            }
            else if (absolute_i >= 0x4000000000000) // Petabyte
            {
                suffix = "PB";
                readable = (i >> 40);
            }
            else if (absolute_i >= 0x10000000000) // Terabyte
            {
                suffix = "TB";
                readable = (i >> 30);
            }
            else if (absolute_i >= 0x40000000) // Gigabyte
            {
                suffix = "GB";
                readable = (i >> 20);
            }
            else if (absolute_i >= 0x100000) // Megabyte
            {
                suffix = "MB";
                readable = (i >> 10);
            }
            else if (absolute_i >= 0x400) // Kilobyte
            {
                suffix = "KB";
                readable = i;
            }
            else
            {
                return i.ToString("0 B"); // Byte
            }
            // Divide by 1024 to get fractional value
            readable = (readable / 1024);
            // Return formatted number with suffix
            return readable.ToString("0.## ") + suffix;
        }

        public void DrawPNG(long blocksize,long disklength,bool[] bitmap,long tainted,string filename,int widthblock,int heightblock,int spacer,int blocksonrow,int offsetx,int offsety,bool rowgroups)
        {
            long blocks = disklength / blocksize;
            if (disklength % blocksize != 0)
            {
                blocks += 1;
            }

            SolidBrush unflagBrush = null; //new SolidBrush(Color.FromArgb(255, 179, 255, 179));
            unflagBrush = new SolidBrush(Color.FromArgb(255, 190, 190, 190));

            SolidBrush flagBrush = null; //new SolidBrush(Color.FromArgb(255, 0, 77, 0));
            flagBrush = new SolidBrush(Color.FromArgb(255, 86, 211, 106));


            SolidBrush lightcolor =  new SolidBrush(Color.FromArgb(255, 255, 255, 255));
            SolidBrush darkcolor = new SolidBrush(Color.FromArgb(255, 230, 230, 230));
           



            //calcing here
            int widthblockspacer = widthblock + spacer;
            int heightblockspacer = heightblock + spacer;

            

            int rows = (int)(blocks / blocksonrow);
            if (blocks % blocksonrow != 0)
            {
                rows++;
            }

            int rowspliter = 16;
            int rowindicator = 10;
            int rowindicatorreservation = (rowgroups) ? rowindicator + spacer : 0;
            int groupspacer = 2;
            int groupspacerreal = (rowgroups)? groupspacer:0;
            
            if (rows < 32)
            {
                rowspliter = 8;
            } 
            int splits = rows / rowspliter;
            if (splits > 0) { splits -= 1; }

            int textheight = 8;

            int texthsp = (textheight >0)?(textheight+5):0;

            int totalwidth = offsetx + rowindicatorreservation + spacer + blocksonrow * widthblockspacer + offsetx ;
            int totalheight = offsety  + spacer + rows * heightblockspacer +  offsety + (splits*groupspacerreal)+texthsp;

            Bitmap bmp = new Bitmap(totalwidth, totalheight + texthsp);
            Graphics g = Graphics.FromImage(bmp);
            g.FillRectangle(System.Drawing.Brushes.White, 0, 0, totalwidth, totalheight);

            //System.Console.WriteLine(c.cbtfixbitmap.Length);
            //System.Console.WriteLine(blocks);

            long bmaplength = bitmap.Length;



            int blockrow = 0;

            int xident = offsetx + rowindicatorreservation + spacer;
            int yident = offsety + spacer;

            int bx = offsetx + spacer;
            int bwdith = rowindicator;

            SolidBrush fcolor = new SolidBrush(Color.FromArgb(255, 40, 40, 40));
            if (textheight > 0)
            {
                FontFamily fontFamily = new FontFamily("Verdana");
                Font font = new Font(
                   fontFamily,
                   textheight,
                   FontStyle.Regular,
                   GraphicsUnit.Pixel);
                String footnote = String.Format("BLKSZ {0} | DISK {1} | CHNGD {2} ({3}) | BLKS {4} | BLK/ROW: {5} ({6}) | ROWS: {7}", GetBytesReadable(blocksize), GetBytesReadable(disklength),tainted, GetBytesReadable(tainted*blocksize), blocks, blocksonrow, GetBytesReadable(blocksize*blocksonrow), rows);
                if (rowgroups && splits > 0)
                {
                    footnote = String.Format("{0} | ROWGRP {1} ({2})", footnote, rowspliter, GetBytesReadable(rowspliter* blocksize * blocksonrow));
                }

                SizeF tsize = g.MeasureString(footnote, font);
                int txtwidth = (int)Math.Ceiling(tsize.Width);

                StringFormat stringFormat = new StringFormat();
                stringFormat.Alignment = StringAlignment.Far;
                stringFormat.LineAlignment = StringAlignment.Far;

                if (txtwidth <= blocksonrow * widthblockspacer)
                {
                    
                    g.DrawString(footnote, font, fcolor, new RectangleF(offsetx + rowindicatorreservation + spacer, totalheight - tsize.Height - offsety, blocksonrow * widthblockspacer, tsize.Height),stringFormat);

                }
                
            }

            for (long i = 0; i < bmaplength && i < blocks; i++)
            {
                
                int ccol = ((int)(i % blocksonrow));
                int crow = (int)(i / blocksonrow);

                

                if (rowgroups && ccol == 0 && crow%rowspliter == 0)
                {
                    blockrow = crow / rowspliter;

                    SolidBrush bbr = lightcolor;
                    if (blockrow % 2 == 0)
                    {
                        bbr = darkcolor;
                    }

                    
                    
                    int by = offsety + spacer + (crow) * heightblockspacer + (blockrow * groupspacerreal);
                    int bheight = heightblockspacer * rowspliter - spacer;

                    if (crow+rowspliter > rows)
                    {
                        bheight = heightblockspacer * (rows - crow);
                    }

                    //System.Console.WriteLine(String.Format("{5} {6} {0} {1} {2} {3} {4}",(blockrow %2), bx, by, bwdith, bheight,blockrow,ccol));
                    g.FillRectangle(bbr, bx, by, bwdith, bheight);
                    
                }


                SolidBrush p = unflagBrush;
                if (bitmap[i])
                {
                    p = flagBrush;
                }

                int xcord = xident + ccol * widthblockspacer;
                int ycord = yident + (crow) * heightblockspacer + (blockrow * groupspacerreal);

                g.FillRectangle(p, xcord, ycord, widthblock, heightblock);
            }

            g.Dispose();
            bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
            bmp.Dispose();
        }

        public void MakePNG(CBTQChangeCalc c,string vmmoref)
        {
            //mod here
            int widthblock = 5;
            int heightblock = 5;
            int spacer = 1;
            int blocksonrow = 128;
            int offsetx = 10;
            int offsety = 10;

            DateTime d = DateTime.UtcNow;

            String fixname = String.Format("fix-{0,4:D4}{1,2:D2}{2,2:D2}t{3,2:D2}{4,2:D2}{5,2:D2}z-{6}-{7}.png", d.Year, d.Month, d.Day, d.Hour, d.Minute, d.Second, c.blocksize, vmmoref);
            DrawPNG(c.blocksize, c.length, c.cbtfixbitmap,c.cbtfixchangedblocks, fixname, widthblock, heightblock, spacer, blocksonrow, offsetx, offsety,true);

            if(c.cbtrealbitmap != null)
            {
                String cbtname = String.Format("cbt-{0,4:D4}{1,2:D2}{2,2:D2}t{3,2:D2}{4,2:D2}{5,2:D2}z-{6}-{7}.png", d.Year, d.Month, d.Day, d.Hour, d.Minute, d.Second, c.cbtrealminblock, vmmoref);
                DrawPNG(c.cbtrealminblock, c.length, c.cbtrealbitmap, c.cbtrealchangedblocks, cbtname, widthblock, heightblock, spacer, blocksonrow, offsetx, offsety,true);
            }

        }

        public void LsFixedBlockChangedBlock(string snapmoref, int diskid, string timestamp, long blocksizekb)
        {
            ManagedObjectReference snapmor = new ManagedObjectReference();
            snapmor.type = "VirtualMachineSnapshot";
            snapmor.Value = snapmoref;

            ManagedObjectReference vmref = null;
            try
            {
                vmref = cb.getServiceUtil().GetMoRefProp(snapmor, "vm");

            }
            catch
            {
                Console.WriteLine("Could not find snapshot");
            }
            if (vmref != null)
            {

                var res = vimservice.QueryChangedDiskAreas(vmref, snapmor, diskid, 0, timestamp);
                if (res != null)
                {
                    CBTQChangeCalc c = LsFixedBlockCalc(res, blocksizekb,false);


                    System.Console.WriteLine(String.Format("Disk Offset : {0}", c.offset));
                    System.Console.WriteLine(String.Format("Disk Length : {0}", c.length));
                    System.Console.WriteLine("###########################");


                    if (c.cbtreal.Count > 0)
                    {


                        foreach (CBTQChangedBlock block in c.cbtreal)
                        {
                            System.Console.WriteLine("Start {0,-20} Length {1,-20} Flagged Fixed Blocks {2,-5}", block.offset, block.length, block.fiximpact);
                        }

                        System.Console.WriteLine("###########################");
                        System.Console.WriteLine(String.Format("Total Bytes CBT Flagged \t: {0}", c.cbtrealtotal));
                        System.Console.WriteLine(String.Format("Total Bytes CBT Flagged (MB)\t: {0}", c.cbtrealtotalmb));
                        System.Console.WriteLine("###########################");

                        foreach (CBTQChangedBlock block in c.cbtfix)
                        {
                            System.Console.WriteLine("Fix Blocks Start {0,-20} Length {1,-20} Fixed Blocks {2,-5}", block.offset, block.length, block.fiximpact);
                        }


                        System.Console.WriteLine("###########################");
                        System.Console.WriteLine(String.Format("Total Fixed CBT Flagged \t: {0}", c.cbtfixchangedblocks));
                        System.Console.WriteLine(String.Format("Total Fixed Bytes CBT Flagged \t: {0}", c.cbtfixtotal));
                        System.Console.WriteLine(String.Format("Total Fixed Bytes CBT Flagged (MB)\t: {0}", c.cbtfixtotalmb));
                    }
                    else
                    {
                        System.Console.WriteLine("Could not find changed blocks");
                    }
                }
            }
        }

        private void LsSnapTree(VirtualMachineSnapshotTree[] rootSnapshotList, int depth)
        {
            var depthstr = "";
            if (depth > 0)
            {
                char[] dc = new char[depth + 3];
                for (var i = 0; i < dc.Length; i++) { dc[i] = ' '; }
                dc[depth] = '|';
                dc[depth + 1] = '-';
                depthstr = new String(dc);
            }

            for (int s = 0; s < rootSnapshotList.Length; s++)
            {
                var snap = rootSnapshotList[s];

                System.Console.WriteLine(String.Format("{0}{1} ({2})", depthstr, snap.name, snap.snapshot.Value));
                if (snap.childSnapshotList != null && snap.childSnapshotList.Length > 0)
                {
                    LsSnapTree(snap.childSnapshotList, depth + 1);
                }
            }
        }

        //Copied from samples VMSnapshot and altered
        public void RmSnap(string vmid, string snapshotName)
        {
            var vr = GetVm(vmid);
            if (vr != null)
            {
                var vmName = vr.Name;

                bool removechildren = false;
                bool consolidate = true;

                System.Console.WriteLine("Virtual Machine Found : " + vr.Name);
                ManagedObjectReference snapmor = getSnapshotReference(
                                                 vr.Mor, vmName,
                                                 snapshotName);
                if (snapmor != null)
                {
                    ManagedObjectReference taskMor
                       = cb.getConnection()._service.RemoveSnapshot_Task(snapmor, removechildren, consolidate, false);
                    String res = cb.getServiceUtil().WaitForTask(taskMor);
                    if (res.Equals("sucess"))
                    {
                        Console.WriteLine("Removed Successfully : " + snapshotName);
                    }
                }
                else
                {
                    Console.WriteLine("Snapshot not found");
                }
            }
            else
            {
                Console.WriteLine("Could not find VM with this MOR");
            }

        }

        //Copied from samples VMSnapshot
        private ManagedObjectReference getSnapshotReference(ManagedObjectReference vmmor,
                                                    String vmName,
                                                    String snapName)
        {
            VirtualMachineSnapshotInfo snapInfo = getSnapshotInfo(vmmor, vmName);
            ManagedObjectReference snapmor = null;
            if (snapInfo != null)
            {
                VirtualMachineSnapshotTree[] snapTree = snapInfo.rootSnapshotList;
                snapmor = traverseSnapshotInTree(snapTree, snapName, false);
            }
            else
            {
                Console.WriteLine("No Snapshot named : " + snapName
                                 + " found for VirtualMachine : " + vmName);
            }
            return snapmor;
        }
        //Copied from samples VMSnapshot
        private VirtualMachineSnapshotInfo getSnapshotInfo
       (ManagedObjectReference vmmor, String vmName)
        {
            ObjectContent[] snaps = cb.getServiceUtil().GetObjectProperties(
               null, vmmor, new String[] { "snapshot" }
            );

            VirtualMachineSnapshotInfo snapInfo = null;
            if (snaps != null && snaps.Length > 0)
            {
                ObjectContent snapobj = snaps[0];
                DynamicProperty[] snapary = snapobj.propSet;
                if (snapary != null && snapary.Length > 0)
                {
                    snapInfo = ((VirtualMachineSnapshotInfo)(snapary[0]).val);
                }
            }
            else
            {
                Console.WriteLine("No Snapshots found for VirtualMachine : "
                                  + vmName);
            }
            return snapInfo;
        }
        //Copied from samples VMSnapshot
        private ManagedObjectReference traverseSnapshotInTree(
                                   VirtualMachineSnapshotTree[] snapTree,
                                   String findName,
                                   Boolean print)
        {
            ManagedObjectReference snapmor = null;
            if (snapTree == null)
            {
                return snapmor;
            }
            for (int i = 0; i < snapTree.Length && snapmor == null; i++)
            {
                VirtualMachineSnapshotTree node = snapTree[i];

                Console.WriteLine("Snapshot Name : " + node.name);


                if (findName != null && node.name.Equals(findName))
                {
                    snapmor = node.snapshot;
                }
                else
                {
                    VirtualMachineSnapshotTree[] childTree = node.childSnapshotList;
                    snapmor = traverseSnapshotInTree(childTree, findName, print);
                }
            }

            return snapmor;
        }


    }
}
