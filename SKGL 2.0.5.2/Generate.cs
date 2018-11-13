using SKGL.Base;
using System;

namespace SKGL
{
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
}
