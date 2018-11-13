using System;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Security;

namespace SKGL.Base
{
    public abstract class BaseConfiguration
    {
        //Put all functions/variables that should be shared with
        //all other classes that inherit this class.
        //
        //note, this class cannot be used as a normal class that
        //you define because it is MustInherit.

        protected internal string _key = "";
        /// <summary>
        /// The key will be stored here
        /// </summary>
        public virtual string Key
        {
            //will be changed in both generating and validating classe.
            get { return _key; }
            set { _key = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public virtual int MachineCode
        {
            get { return getMachineCode(); }
        }

        [SecuritySafeCritical]
        private static int getMachineCode()
        {
            // please see https://skgl.codeplex.com/workitem/2246 for a list of developers of this code.

            methods m = new methods();

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_Processor");
            string collectedInfo = "";
            // here we will put the informa
            foreach (ManagementObject share in searcher.Get())
            {
                // first of all, the processorid
                collectedInfo += share.GetPropertyValue("ProcessorId");
            }

            searcher.Query = new ObjectQuery("select * from Win32_BIOS");
            foreach (ManagementObject share in searcher.Get())
            {
                //then, the serial number of BIOS
                collectedInfo += share.GetPropertyValue("SerialNumber");
            }

            searcher.Query = new ObjectQuery("select * from Win32_BaseBoard");
            foreach (ManagementObject share in searcher.Get())
            {
                //finally, the serial number of motherboard
                collectedInfo += share.GetPropertyValue("SerialNumber");
            }

            // patch luca bernardini
            if (string.IsNullOrEmpty(collectedInfo) | collectedInfo == "00" | collectedInfo.Length <= 3)
            {
                collectedInfo += getHddSerialNumber();
            }

            // In case we have message "To be filled by O.E.M." - there is incorrect motherboard/BIOS serial number 
            // - we should relay to NIC
            if (collectedInfo.Contains("To be filled by O.E.M."))
            {
                var nic = GetNicInfo();

                if (!string.IsNullOrWhiteSpace(nic))
                    collectedInfo += nic;
            }

            return m.getEightByteHash(collectedInfo, 100000);

        }

        /// <summary>
        /// Enumerate all Nic adapters, take first one, who has MAC address and return it.
        /// </summary>
        /// <remarks> Function MUST! be updated to select only real NIC cards (and filter out USB and PPTP etc interfaces).
        /// Otherwise user can run in this scenario: a) Insert USB NIC b) Generate machine code c) Remove USB NIC...
        /// </remarks>
        /// <returns>MAC address of NIC adapter</returns>
        [SecuritySafeCritical]
        private static string GetNicInfo()
        {
            var nics = NetworkInterface.GetAllNetworkInterfaces();
            var mac = string.Empty;

            foreach (var adapter in nics.Where(adapter => string.IsNullOrWhiteSpace(mac)))
                mac = adapter.GetPhysicalAddress().ToString();

            return mac;
        }

        /// <summary>
        /// Read the serial number from the hard disk that keep the bootable partition (boot disk)
        /// </summary>
        /// <returns>
        /// If succedes, returns the string rappresenting the Serial Number.
        /// String.Empty if it fails.
        /// </returns>
        [SecuritySafeCritical]
        private static string getHddSerialNumber()
        {
            // --- Win32 Disk 
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("\\root\\cimv2", "select * from Win32_DiskPartition WHERE BootPartition=True");

            uint diskIndex = 999;
            foreach (ManagementObject partition in searcher.Get())
            {
                diskIndex = Convert.ToUInt32(partition.GetPropertyValue("DiskIndex")); // should be DiskIndex
                break; // TODO: might not be correct. Was : Exit For
            }

            // I haven't found the bootable partition. Fail.
            if (diskIndex == 999)
                return string.Empty;
             
            // --- Win32 Disk Drive
            searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive where Index = " + diskIndex.ToString());

            string deviceName = "";
            foreach (ManagementObject wmi_HD in searcher.Get())
            {
                deviceName = wmi_HD.GetPropertyValue("Name").ToString();
                break; // TODO: might not be correct. Was : Exit For
            }


            // I haven't found the disk drive. Fail
            if (string.IsNullOrEmpty(deviceName.Trim()))
                return string.Empty;

            // -- Some problems in query parsing with backslash. Using like operator
            if (deviceName.StartsWith("\\\\.\\"))
            {
                deviceName = deviceName.Replace("\\\\.\\", "%");
            }

            // --- Physical Media
            searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMedia WHERE Tag like '" + deviceName + "'");
            string serial = string.Empty;
            foreach (ManagementObject wmi_HD in searcher.Get())
            {
                serial = wmi_HD.GetPropertyValue("SerialNumber").ToString();
                break; // TODO: might not be correct. Was : Exit For
            }

            return serial;

        }

    }
}
