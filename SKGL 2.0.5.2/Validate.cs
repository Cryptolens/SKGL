using SKGL.Base;
using System;

namespace SKGL
{
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
}
