using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
//Copyright (C) 2011-2012 Artem Los, www.clizware.net.
//The author of this code shall get the credits

// This project uses two general algorithms:
//  - Artem's Information Storage Format (Artem's ISF-2)
//  - Artem's Serial Key Algorithm (Artem's SKA-2)

// A great thank to Iberna (https://www.codeplex.com/site/users/view/lberna)
// for getHardDiskSerial algorithm.

using System.Text;
using System.Management;
using System.Security;
using System.Numerics;
using System.Net.NetworkInformation;
using System.Linq;


[assembly: AllowPartiallyTrustedCallers()]
namespace SKGL
{
#region "S E R I A L  K E Y  G E N E R A T I N G  L I B R A R Y"

#region "CONFIGURATION"

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
    public class SerialKeyConfiguration : BaseConfiguration
    {

        #region "V A R I A B L E S"
        private bool[] _Features = new bool[8] {
		false,
		false,
		false,
		false,
		false,
		false,
		false,
		false
		//the default value of the Fetures array.
	};
        public virtual bool[] Features
        {
            //will be changed in validating class.
            get { return _Features; }
            set { _Features = value; }
        }
        private bool _addSplitChar = true;
        public bool addSplitChar
        {
            get { return _addSplitChar; }
            set { _addSplitChar = value; }
        }


        #endregion

    }
#endregion

    #region "ENCRYPTION"
    public class Generate : BaseConfiguration
    {
        //this class have to be inherited because of the key which is shared with both encryption/decryption classes.

        SerialKeyConfiguration skc = new SerialKeyConfiguration();
        methods m = new methods();
        Random r = new Random();
        public Generate()
        {
            // No overloads works with Sub New
        }
        public Generate(SerialKeyConfiguration _serialKeyConfiguration)
        {
            skc = _serialKeyConfiguration;
        }

        private string _secretPhase;
        /// <summary>
        /// If the key is to be encrypted, enter a password here.
        /// </summary>

        public string secretPhase
        {
            get { return _secretPhase; }
            set
            {
                if (value != _secretPhase)
                {
                    _secretPhase = m.twentyfiveByteHash(value);
                }
            }
        }
        /// <summary>
        /// This function will generate a key.
        /// </summary>
        /// <param name="timeLeft">For instance, 30 days.</param>
        public string doKey(int timeLeft)
        {
            return doKey(timeLeft, DateTime.Today); // removed extra argument false
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeLeft">For instance, 30 days</param>
        /// <param name="useMachineCode">Lock a serial key to a specific machine, given its "machine code". Should be 5 digits long.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public object doKey(int timeLeft, int useMachineCode)
        {
            return doKey(timeLeft, DateTime.Today, useMachineCode);
        }

        /// <summary>
        /// This function will generate a key. You may also change the creation date.
        /// </summary>
        /// <param name="timeLeft">For instance, 30 days.</param>
        /// <param name="creationDate">Change the creation date of a key.</param>
        /// <param name="useMachineCode">Lock a serial key to a specific machine, given its "machine code". Should be 5 digits long.</param>
        public string doKey(int timeLeft, System.DateTime creationDate, int useMachineCode = 0)
        {
            if (timeLeft > 999)
            {
                //Checking if the timeleft is NOT larger than 999. It cannot be larger to match the key-length 20.
                throw new ArgumentException("The timeLeft is larger than 999. It can only consist of three digits.");
            }

            if (!string.IsNullOrEmpty(secretPhase) | secretPhase != null)
            {
                //if some kind of value is assigned to the variable "secretPhase", the code will execute it FIRST.
                //the secretPhase shall only consist of digits!
                System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex("^\\d$");
                //cheking the string
                if (reg.IsMatch(secretPhase))
                {
                    //throwing new exception if the string contains non-numrical letters.
                    throw new ArgumentException("The secretPhase consist of non-numerical letters.");
                }
            }

            //if no exception is thown, do following
            string _stageThree = null;
            if (useMachineCode > 0 & useMachineCode <= 99999)
            {
                _stageThree = m._encrypt(timeLeft, skc.Features, secretPhase, useMachineCode, creationDate);
                // stage one
            }
            else
            {
                _stageThree = m._encrypt(timeLeft, skc.Features, secretPhase, r.Next(0, 99999), creationDate);
                // stage one
            }

            //if it is the same value as default, we do not need to mix chars. This step saves generation time.

            if (skc.addSplitChar == true)
            {
                // by default, a split character will be addedr
                Key = _stageThree.Substring(0, 5) + "-" + _stageThree.Substring(5, 5) + "-" + _stageThree.Substring(10, 5) + "-" + _stageThree.Substring(15, 5);
            }
            else
            {
                Key = _stageThree;
            }

            //we also include the key in the Key variable to make it possible for user to get his key without generating a new one.
            return Key;

        }


    }
    #endregion

    #region "DECRYPTION"
    public class Validate : BaseConfiguration
    {
        //this class have to be inherited becuase of the key which is shared with both encryption/decryption classes.

        SerialKeyConfiguration skc = new SerialKeyConfiguration();
        methods _a = new methods();
        public Validate()
        {
            // No overloads works with Sub New
        }
        public Validate(SerialKeyConfiguration _serialKeyConfiguration)
        {
            skc = _serialKeyConfiguration;
        }
        /// <summary>
        /// Enter a key here before validating.
        /// </summary>
        public string Key
        {
            //re-defining the Key
            get { return _key; }
            set
            {
                _res = "";
                _key = value;
            }
        }

        private string _secretPhase = "";
        /// <summary>
        /// If the key has been encrypted, when it was generated, please set the same secretPhase here.
        /// </summary>
        public string secretPhase
        {
            get { return _secretPhase; }
            set
            {
                if (value != _secretPhase)
                {
                    _secretPhase = _a.twentyfiveByteHash(value);
                }
            }
        }


        private string _res = "";

        private void decodeKeyToString()
        {
            // checking if the key already have been decoded.
            if (string.IsNullOrEmpty(_res) | _res == null)
            {

                string _stageOne = "";

                Key = Key.Replace("-", "");

                //if the admBlock has been changed, the getMixChars will be executed.


                _stageOne = Key;


                _stageOne = Key;

                // _stageTwo = _a._decode(_stageOne)

                if (!string.IsNullOrEmpty(secretPhase) | secretPhase != null)
                {
                    //if no value "secretPhase" given, the code will directly decrypt without using somekind of encryption
                    //if some kind of value is assigned to the variable "secretPhase", the code will execute it FIRST.
                    //the secretPhase shall only consist of digits!
                    System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex("^\\d$");
                    //cheking the string
                    if (reg.IsMatch(secretPhase))
                    {
                        //throwing new exception if the string contains non-numrical letters.
                        throw new ArgumentException("The secretPhase consist of non-numerical letters.");
                    }
                }
                _res = _a._decrypt(_stageOne, secretPhase);


            }
        }
        private bool _IsValid()
        {
            //Dim _a As New methods ' is only here to provide the geteighthashcode method
            try
            {
                if (Key.Contains("-"))
                {
                    if (Key.Length != 23)
                    {
                        return false;
                    }
                }
                else
                {
                    if (Key.Length != 20)
                    {
                        return false;
                    }
                }
                decodeKeyToString();

                string _decodedHash = _res.Substring(0, 9);
                string _calculatedHash = _a.getEightByteHash(_res.Substring(9, 19)).ToString().Substring(0, 9);
                // changed Math.Abs(_res.Substring(0, 17).GetHashCode).ToString.Substring(0, 8)

                //When the hashcode is calculated, it cannot be taken for sure, 
                //that the same hash value will be generated.
                //learn more about this issue: http://msdn.microsoft.com/en-us/library/system.object.gethashcode.aspx
                if (_decodedHash == _calculatedHash)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                //if something goes wrong, for example, when decrypting, 
                //this function will return false, so that user knows that it is unvalid.
                //if the key is valid, there won't be any errors.
                return false;
            }
        }
        /// <summary>
        /// Checks whether the key has been modified or not. If the key has been modified - returns false; if the key has not been modified - returns true.
        /// </summary>
        public bool IsValid
        {
            get { return _IsValid(); }
        }
        private bool _IsExpired()
        {
            if (DaysLeft > 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        /// <summary>
        /// If the key has expired - returns true; if the key has not expired - returns false.
        /// </summary>
        public bool IsExpired
        {
            get { return _IsExpired(); }
        }
        private System.DateTime _CreationDay()
        {
            decodeKeyToString();
            System.DateTime _date = new System.DateTime();
            _date = new DateTime(Convert.ToInt32(_res.Substring(9, 4)), Convert.ToInt32(_res.Substring(13, 2)), Convert.ToInt32(_res.Substring(15, 2)));

            return _date;
        }
        /// <summary>
        /// Returns the creation date of the key.
        /// </summary>
        public System.DateTime CreationDate
        {
            get { return _CreationDay(); }
        }
        private int _DaysLeft()
        {
            decodeKeyToString();
            int _setDays = SetTime;
            return Convert.ToInt32(((TimeSpan)(ExpireDate - DateTime.Today)).TotalDays); //or viseversa
        }
        /// <summary>
        /// Returns the amount of days the key will be valid.
        /// </summary>
        public int DaysLeft
        {
            get { return _DaysLeft(); }
        }

        private int _SetTime()
        {
            decodeKeyToString();
            return Convert.ToInt32(_res.Substring(17, 3));
        }
        /// <summary>
        /// Returns the actual amount of days that were set when the key was generated.
        /// </summary>
        public int SetTime
        {
            get { return _SetTime(); }
        }
        private System.DateTime _ExpireDate()
        {
            decodeKeyToString();
            System.DateTime _date = new System.DateTime();
            _date = CreationDate;
            return _date.AddDays(SetTime);
        }
        /// <summary>
        /// Returns the date when the key is to be expired.
        /// </summary>
        public System.DateTime ExpireDate
        {
            get { return _ExpireDate(); }
        }
        private bool[] _Features()
        {
            decodeKeyToString();
            return _a.intToBoolean(Convert.ToInt32(_res.Substring(20, 3)));
        }
        /// <summary>
        /// Returns all 8 features in a boolean array
        /// </summary>
        public bool[] Features
        {
            //we already have defined Features in the BaseConfiguration class. 
            //Here we only change it to Read Only.
            get { return _Features(); }
        }

        /// <summary>
        /// If the current machine's machine code is equal to the one that this key is designed for, return true.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool IsOnRightMachine
        {
            get
            {
                int decodedMachineCode = Convert.ToInt32(_res.Substring(23, 5));

                return decodedMachineCode == MachineCode;
            }
        }
    }
    #endregion

    #region "T H E  C O R E  O F  S K G L"

    internal class methods : SerialKeyConfiguration
    {

        //The construction of the key
        protected internal string _encrypt(int _days, bool[] _tfg, string _secretPhase, int ID, System.DateTime _creationDate)
        {
            // This function will store information in Artem's ISF-2
            //Random variable was moved because of the same key generation at the same time.

            int _retInt = Convert.ToInt32(_creationDate.ToString("yyyyMMdd"));
            // today

            decimal result = 0;

            result += _retInt;
            // adding the current date; the generation date; today.
            result *= 1000;
            // shifting three times at left

            result += _days;
            // adding time left
            result *= 1000;
            // shifting three times at left

            result += booleanToInt(_tfg);
            // adding features
            result *= 100000;
            //shifting three times at left

            result += ID;
            // adding random ID

            // This part of the function uses Artem's SKA-2

            if (string.IsNullOrEmpty(_secretPhase) | _secretPhase == null)
            {
                // if not password is set, return an unencrypted key
                return base10ToBase26((getEightByteHash(result.ToString())  +result.ToString()));
            }
            else
            {
                // if password is set, return an encrypted 
                return base10ToBase26((getEightByteHash(result.ToString()) + _encText(result.ToString(), _secretPhase) ));
            }


        }
        protected internal string _decrypt(string _key, string _secretPhase)
        {
            if (string.IsNullOrEmpty(_secretPhase) | _secretPhase == null)
            {
                // if not password is set, return an unencrypted key
                return base26ToBase10(_key);
            }
            else
            {
                // if password is set, return an encrypted 
                string usefulInformation = base26ToBase10(_key);
                return usefulInformation.Substring(0, 9) + _decText(usefulInformation.Substring(9), _secretPhase);
            }

        }
        //Deeper - encoding, decoding, et cetera.

        //Convertions, et cetera.----------------
        protected internal int booleanToInt(bool[] _booleanArray)
        {
            int _aVector = 0;
            //
            //In this function we are converting a binary value array to a int
            //A binary array can max contain 4 values.
            //Ex: new boolean(){1,1,1,1}

            for (int _i = 0; _i < _booleanArray.Length; _i++)
            {
                switch (_booleanArray[_i])
                {
                    case true:
                        _aVector += Convert.ToInt32((Math.Pow(2, (_booleanArray.Length - _i - 1))));
                        // times 1 has been removed
                        break;
                }
            }
            return _aVector;
        }
        protected internal bool[] intToBoolean(int _num)
        {
            //In this function we are converting an integer (created with privious function) to a binary array

            int _bReturn = Convert.ToInt32(Convert.ToString(_num, 2));
            string _aReturn = Return_Lenght(_bReturn.ToString(), 8);
            bool[] _cReturn = new bool[8];


            for (int i = 0; i <= 7; i++)
            {
                _cReturn[i] = _aReturn.ToString().Substring(i, 1) == "1" ? true : false;
            }
            return _cReturn;
        }
        protected internal string _encText(string _inputPhase, string _secretPhase)
        {
            //in this class we are encrypting the integer array.
            string _res = "";

            for (int i = 0; i <= _inputPhase.Length - 1; i++)
            {
                _res += modulo(Convert.ToInt32(_inputPhase.Substring(i, 1)) + Convert.ToInt32(_secretPhase.Substring(modulo(i, _secretPhase.Length), 1)), 10);
            }

            return _res;
        }
        protected internal string _decText(string _encryptedPhase, string _secretPhase)
        {
            //in this class we are decrypting the text encrypted with the function above.
            string _res = "";

            for (int i = 0; i <= _encryptedPhase.Length - 1; i++)
            {
                _res += modulo(Convert.ToInt32(_encryptedPhase.Substring(i, 1)) - Convert.ToInt32(_secretPhase.Substring(modulo(i, _secretPhase.Length), 1)), 10);
            }

            return _res;
        }
        protected internal string Return_Lenght(string Number, int Lenght)
        {
            // This function create 3 lenght char ex: 39 to 039
            if ((Number.ToString().Length != Lenght))
            {
                while (!(Number.ToString().Length == Lenght))
                {
                    Number = "0" + Number;
                }
            }
            return Number;
            //Return Number

        }
        protected internal int modulo(int _num, int _base)
        {
            // canged return type to integer.
            //this function simply calculates the "right modulo".
            //by using this function, there won't, hopefully be a negative
            //number in the result!
            return _num - _base * Convert.ToInt32(Math.Floor((decimal)_num / (decimal)_base));
        }
        protected internal string twentyfiveByteHash(string s)
        {
            int amountOfBlocks = s.Length / 5;
            string[] preHash = new string[amountOfBlocks + 1];

            if (s.Length <= 5)
            {
                //if the input string is shorter than 5, no need of blocks! 
                preHash[0] = getEightByteHash(s).ToString();
            }
            else if (s.Length > 5)
            {
                //if the input is more than 5, there is a need of dividing it into blocks.
                for (int i = 0; i <= amountOfBlocks - 2; i++)
                {
                    preHash[i] = getEightByteHash(s.Substring(i * 5, 5)).ToString();
                }

                preHash[preHash.Length - 2] = getEightByteHash(s.Substring((preHash.Length - 2) * 5, s.Length - (preHash.Length - 2) * 5)).ToString();
            }
            return string.Join("", preHash);
        }
        protected internal int getEightByteHash(string s, int MUST_BE_LESS_THAN = 1000000000)
        {
            //This function generates a eight byte hash

            //The length of the result might be changed to any length
            //just set the amount of zeroes in MUST_BE_LESS_THAN
            //to any length you want
            uint hash = 0;

            foreach (byte b in System.Text.Encoding.Unicode.GetBytes(s))
            {
                hash += b;
                hash += (hash << 10);
                hash ^= (hash >> 6);
            }

            hash += (hash << 3);
            hash ^= (hash >> 11);
            hash += (hash << 15);

            int result = (int)(hash % MUST_BE_LESS_THAN);

            //we want the result to not be zero, as this would thrown an exception in check.
            if (result == 0)
                result = 1;


            int check = MUST_BE_LESS_THAN / result;

            if (check > 1)
            {
                result *= check;
            }

            //when result is less than MUST_BE_LESS_THAN, multiplication of result with check will be in that boundary.
            //otherwise, we have to divide by 10.
            if (MUST_BE_LESS_THAN == result)
                result /= 10;


            return result;
        }
        protected internal string base10ToBase26(string s)
        {
            // This method is converting a base 10 number to base 26 number.
            // Remember that s is a decimal, and the size is limited. 
            // In order to get size, type Decimal.MaxValue.
            //
            // Note that this method will still work, even though you only 
            // can add, subtract numbers in range of 15 digits.
            char[] allowedLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

            decimal num = Convert.ToDecimal(s);
            int reminder = 0;

            char[] result = new char[s.ToString().Length + 1];
            int j = 0;


            while ((num >= 26))
            {
                reminder = Convert.ToInt32(num % 26);
                result[j] = allowedLetters[reminder];
                num = (num - reminder) / 26;
                j += 1;
            }

            result[j] = allowedLetters[Convert.ToInt32(num)];
            // final calculation

            string returnNum = "";

            for (int k = j; k >= 0; k -= 1)  // not sure
            {
                returnNum += result[k];
            }
            return returnNum;

        }
        protected internal string base26ToBase10(string s)
        {
            // This function will convert a number that has been generated
            // with functin above, and get the actual number in decimal
            //
            // This function requieres Mega Math to work correctly.

            string allowedLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            System.Numerics.BigInteger result = new System.Numerics.BigInteger();


            for (int i = 0; i <= s.Length - 1; i += 1)
            {
                BigInteger pow = powof(26, (s.Length - i - 1));

                result = result + allowedLetters.IndexOf(s.Substring(i, 1)) * pow;

            }

            return result.ToString(); //not sure
        }

        protected internal BigInteger powof(int x, int y)
        {
            // Because of the uncertain answer using Math.Pow and ^, 
            // this function is here to solve that issue.
            // It is currently using the MegaMath library to calculate.
            BigInteger newNum = 1;

            if (y == 0)
            {
                return 1;
                // if 0, return 1, e.g. x^0 = 1 (mathematicaly proven!) 
            }
            else if (y == 1)
            {
                return x;
                // if 1, return x, which is the base, e.g. x^1 = x
            }
            else
            {
                for (int i = 0; i <= y - 1; i++)
                {
                    newNum = newNum * x;
                }
                return newNum;
                // if both conditions are not satisfied, this loop
                // will continue to y, which is the exponent.
            }
        }
    }


    #endregion

#endregion
}