using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Linq;
//Copyright (C) 2014 Steve Aitken, Artem Los
//All rights reserved.

//This code is based on SKGL 2.0.5.2 with improvements and changes by Steve Aitken

using System.Text;
using System.Management;
using System.Security;
using System.Numerics;


namespace SKGL.Plus
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


        private static int getMachineCode()
        {
            //      * Copyright (C) 2012 Artem Los, All rights reserved.
            //      * 
            //      * This code will generate a 5 digits long key, finger print, of the system
            //      * where this method is being executed. However, that might be changed in the
            //      * hash function "GetStableHash", by changing the amount of zeroes in
            //      * MUST_BE_LESS_OR_EQUAL_TO to the one you want to have. Ex 1000 will return 
            //      * 3 digits long hash.
            methods m = new methods();
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMedia");

            string collectedInfo = "";

            collectedInfo += System.Environment.MachineName;
            var x = new List<string>();
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                // probably need to do some filtering on ni.NetworkInterfaceType here
                //collectedInfo +=ni.GetPhysicalAddress();
                x.Add(ni.GetPhysicalAddress().ToString());
            }
            x.Sort();
            collectedInfo += x[x.Count - 1];
            x.Clear();

            foreach (ManagementObject wmi_HD in searcher.Get())
            {
                // get the hardware serial no.
                if (wmi_HD["SerialNumber"] == null && wmi_HD["Removable"] == null && wmi_HD["Replaceable"] == null)
                {
                    // Don't add
                }
                else
                {
                    //serialNo = wmi_HD["SerialNumber"].ToString();
                    //hotSwap = Convert.ToBoolean(wmi_HD["HotSwappable"]);
                    //Console.WriteLine(hotSwap);
                    x.Add(wmi_HD["SerialNumber"].ToString());
                }
            }
            x.Sort();
            collectedInfo += string.Join("", x);

            return m.getEightByteHash(collectedInfo, 100000);
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
            r = new Random(DateTime.Now.Millisecond);
        }
        public Generate(SerialKeyConfiguration _serialKeyConfiguration)
        {
            skc = _serialKeyConfiguration;
            r = new Random(DateTime.Now.Millisecond);
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
                if ((value != _secretPhase) && (!string.IsNullOrEmpty(value)))
                {
                    _secretPhase = m.twentyfiveByteHash(value);
                }
            }
        }

        private int _numUsers;

        public int numUsers
        {
            get { return _numUsers; }
            set
            {
                _numUsers = value;
            }
        }

        /// <summary>
        /// This function will generate a key.
        /// </summary>
        /// <param name="timeLeft">For instance, 30 days.</param>
        public string doKey(int timeLeft, int users = 0, int assets = 0, int concurrentUsers = 0)
        {
            return doKey(timeLeft, DateTime.Today, users, assets, concurrentUsers); // removed extra argument false
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeLeft">For instance, 30 days</param>
        /// <param name="useMachineCode">Lock a serial key to a specific machine, given its "machine code". Should be 5 digits long.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public object doKey(int timeLeft, int useMachineCode, int users = 0, int assets = 0, int concurrentUsers = 0)
        {
            return doKey(timeLeft, DateTime.Today, useMachineCode, users, assets, concurrentUsers);
        }

        /// <summary>
        /// This function will generate a key. You may also change the creation date.
        /// </summary>
        /// <param name="timeLeft">For instance, 30 days.</param>
        /// <param name="creationDate">Change the creation date of a key.</param>
        /// <param name="useMachineCode">Lock a serial key to a specific machine, given its "machine code". Should be 5 digits long.</param>
        public string doKey(int timeLeft, System.DateTime creationDate, int useMachineCode = 0, int users = 0, int assets = 0, int concurrentUsers = 0)
        {
            if (timeLeft > 999)
            {
                //Checking if the timeleft is NOT larger than 999..
                throw new ArgumentException("The timeLeft is larger than 999. It can only consist of three digits.");
            }
            if (users > 100000)
            {
                //Checking if the timeleft is NOT larger than 999. .
                throw new ArgumentException("Users is larger than 99,999. It can only consist of five digits.");
            }
            if (assets > 100)
            {
                //Checking if the timeleft is NOT larger than 999. .
                throw new ArgumentException("Assets is larger than 99. It can only consist of two digits.");
            }
            if (MachineCode > 99999)
            {
                //Checking if the timeleft is NOT larger than 999. .
                throw new ArgumentException("Machine ID is larger than 99999. It can only consist of five digits.");
            }
            if (concurrentUsers > 9999)
            {
                //Checking if the timeleft is NOT larger than 999. .
                throw new ArgumentException("concurrentUsers is larger than 9999. It can only consist of four digits.");
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
                _stageThree = m._encrypt(users, timeLeft, skc.Features, secretPhase, useMachineCode, creationDate, assets, concurrentUsers);
                // stage one
            }
            else
            {
                _stageThree = m._encrypt(users, timeLeft, skc.Features, secretPhase, r.Next(0, 99999), creationDate, assets, concurrentUsers);
                // stage one
            }

            //if it is the same value as default, we do not need to mix chars. This step saves generation time.

            if (skc.addSplitChar == true)
            {
                // by default, a split character will be added - this loop adds a split at every 5 characters.
                Key = "";
                var i = 0;
                foreach (char c in _stageThree)
                {
                    if (i % 5 == 0 && i != 0)
                    {
                        Key = Key + "-";
                    }
                    Key = Key + c;
                    i++;
                }
            }
            else
            {
                Key = _stageThree;
            }



            //we also include the key in the Key variable to make it possible for user to get this key without generating a new one.
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
                if ((value != _secretPhase) && (!string.IsNullOrEmpty(value)))
                {
                    _secretPhase = _a.twentyfiveByteHash(value);
                    _res = "";
                }
            }
        }

        /// <summary>
        /// This is the decrypted, decoded key, is only set if the key has been processed, and is reset to blank if the key is changed.
        /// </summary>
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
                if (string.IsNullOrEmpty(_res))
                {
                    decodeKeyToString();
                    string _decodedHash = _res.Substring(0, 9);
                    string _calculatedHash = _a.getEightByteHash(_res.Substring(9, _res.Length - 9)).ToString().Substring(0, 9);
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
                        //reset the decoded string
                        _res = "";
                        return false;
                    }
                }
                else
                {//_res contains decrypted valid key - so this is valid
                    return true;
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
            if (this.IsValid)
            {
                System.DateTime _date = new System.DateTime();
                _date = new DateTime(Convert.ToInt32(_res.Substring(9, 4)), Convert.ToInt32(_res.Substring(13, 2)), Convert.ToInt32(_res.Substring(15, 2)));
                return _date;
            }
            else
            {
                return DateTime.MinValue;
            }
        }
        /// <summary>
        /// Returns the creation date of the key.
        /// </summary>
        public System.DateTime CreationDate
        {
            get { if (IsValid) return _CreationDay(); else return DateTime.MinValue; }
        }
        private int _DaysLeft()
        {
            int _setDays = SetTime;
            return Convert.ToInt32(((TimeSpan)(ExpireDate - DateTime.Today)).TotalDays); //or viseversa
        }
        /// <summary>
        /// Returns the amount of days the key will be valid.
        /// </summary>
        public int DaysLeft
        {
            get { if (IsValid) return _DaysLeft(); else return 0; }
        }

        private int _ConcurrentUsers()
        {
            if (_res.Length > 35)
            {
                return Convert.ToInt32(_res.Substring(35, 4));
            }
            else//old license, no concurrent users.
                return 0;
        }

        public int ConcurrentUsers
        {
            get { if (IsValid) return _ConcurrentUsers(); else return 0; }
        }

        public int MaxUsers
        {
            get { if (IsValid) return _MaxUsers(); else return 0; }
        }

        private int _MaxUsers()
        {
            return Convert.ToInt32(_res.Substring(28, 5));
        }

        public int MaxAssets
        {
            get { if (IsValid) return _MaxAssets(); else return 0; }
        }

        private int _MaxAssets()
        {
            return Convert.ToInt32(_res.Substring(33, 2));
        }
        private int _SetTime()
        {
            return Convert.ToInt32(_res.Substring(17, 3));
        }
        /// <summary>
        /// Returns the actual amount of days that were set when the key was generated.
        /// </summary>
        public int SetTime
        {
            get { if (IsValid) return _SetTime(); else return 0; }
        }
        private System.DateTime _ExpireDate()
        {
            if (IsValid)
            {
                System.DateTime _date = new System.DateTime();
                _date = CreationDate;
                if (SetTime > 0)
                {
                    return _date.AddDays(SetTime);
                }
                else
                {
                    return DateTime.MaxValue;
                }
            }
            else
            {
                return DateTime.MinValue;
            }
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
            return _a.intToBoolean(Convert.ToInt32(_res.Substring(20, 3)));
        }
        /// <summary>
        /// Returns all 8 features in a boolean array
        /// </summary>
        public bool[] Features
        {
            //we already have defined Features in the BaseConfiguration class. 
            //Here we only change it to Read Only.
            get { if (IsValid) return _Features(); else return new bool[] { false, false, false, false, false, false, false, false }; }
        }

        public int DecodedMachineCode
        {
            get
            {
                if (IsValid)
                {
                    return Convert.ToInt32(_res.Substring(23, 5));
                }
                else
                    return -1;
            }
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
                if (IsValid)
                {
                    int decodedMachineCode = Convert.ToInt32(_res.Substring(23, 5));
                    return decodedMachineCode == MachineCode || decodedMachineCode == 1;
                }
                else
                {
                    return false;
                }
            }
        }


        public override string ToString()
        {
            return string.Format("Valid: {0} \n Days Left: {1} \n Features: {2},{3},{4},{5},{6},{7},{8},{9} \n machine code: {10} \n current machine code:{11} \n Valid on machine: {12} \n Users: {13} \n Assets: {14}\n ConcurrentUsers: {15}\n",
                                        this.IsValid,
                                        this.DaysLeft,
                                        this.Features[0],
                                        this.Features[1],
                                        this.Features[2],
                                        this.Features[3],
                                        this.Features[4],
                                        this.Features[5],
                                        this.Features[6],
                                        this.Features[7],
                                        this.DecodedMachineCode,
                                        this.MachineCode,
                                        this.IsOnRightMachine,
                                        this.MaxUsers,
                                        this.MaxAssets,
                                        this.ConcurrentUsers);
        }

    }
    #endregion

    #region "T H E  C O R E  O F  S K G L"

    internal class methods : SerialKeyConfiguration
    {

        //The construction of the key
        protected internal string _encrypt(int _users, int _days, bool[] _tfg, string _secretPhase, int ID, System.DateTime _creationDate, int _assets = 0, int _concurrentUsers = 0)
        {
            // This function will store information in Artem's ISF-2
            Random rnd = new Random();
            int _retInt = Convert.ToInt32(_creationDate.ToString("yyyyMMdd"));
            // today
            string keygen = _retInt.ToString("D8");
            // adding the current date; the generation date; today.
            keygen += _days.ToString("D3");
            // adding time left
            keygen += booleanToInt(_tfg).ToString("D3");
            // adding features
            keygen += ID.ToString("D5");
            // adding machineID
            keygen += _users.ToString("D4");
            keygen += _assets.ToString("D3");
            keygen += _concurrentUsers.ToString("D4");
            //add a random number to the end to make a unique license - so that we could revoke it if we want to.
            keygen += rnd.Next(0, 10000).ToString("D5");
            // This part of the function uses Artem's SKA-2

            if (string.IsNullOrEmpty(_secretPhase) | _secretPhase == null)
            {
                // if not password is set, return an unencrypted key
                return base10ToBase36((getEightByteHash(keygen) + keygen));
            }
            else
            {
                // if password is set, return an encrypted 
                return base10ToBase36((getEightByteHash(keygen) + _encText(keygen, _secretPhase)));
            }


        }
        protected internal string _decrypt(string _key, string _secretPhase)
        {
            if (string.IsNullOrEmpty(_secretPhase) | _secretPhase == null)
            {
                // if not password is set, return an unencrypted key
                return base36ToBase10(_key);
            }
            else
            {
                // if password is set, return an encrypted 
                string usefulInformation = base36ToBase10(_key);
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
            string _aReturn = _bReturn.ToString("D8");
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
            int check = MUST_BE_LESS_THAN / result;

            if (check > 1)
            {
                result *= check;
            }

            return result;
        }
        internal string licenseCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890%$#@*&~";
        protected internal string base10ToBase36(string s)
        {
            // This method is converting a base 10 number to base 26 number.
            // Note that this method will still work, even though you only 
            // it is limited at 232 characters with a bigint.
            char[] allowedLetters = licenseCharacters.ToCharArray();
            var numletters = allowedLetters.Count();
            BigInteger num = BigInteger.Parse(s);
            int reminder = 0;

            char[] result = new char[s.ToString().Length + 1];
            int j = 0;


            while ((num >= numletters))
            {
                reminder = (int)(num % numletters);
                result[j] = allowedLetters[reminder];
                num = (num - reminder) / numletters;
                j += 1;
            }

            result[j] = allowedLetters[(int)(num)];
            // final calculation

            string returnNum = "";

            for (int k = j; k >= 0; k -= 1)  // not sure
            {
                returnNum += result[k];
            }
            return returnNum;

        }
        protected internal string base36ToBase10(string s)
        {
            // This function will convert a number that has been generated
            // with functin above, and get the actual number in decimal
            //
            // This function requieres Mega Math to work correctly.

            string allowedLetters = licenseCharacters;
            System.Numerics.BigInteger result = new System.Numerics.BigInteger();
            for (int i = 0; i <= s.Length - 1; i += 1)
            {
                BigInteger pow = powof(allowedLetters.Length, (s.Length - i - 1));
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
