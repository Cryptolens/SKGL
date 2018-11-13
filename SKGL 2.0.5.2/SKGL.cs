using System;
using System.Numerics;
using System.Security;

//Copyright (C) 2011-2012 Artem Los, www.clizware.net.
//The author of this code shall get the credits

// This project uses two general algorithms:
//  - Artem's Information Storage Format (Artem's ISF-2)
//  - Artem's Serial Key Algorithm (Artem's SKA-2)

// A great thank to Iberna (https://www.codeplex.com/site/users/view/lberna)
// for getHardDiskSerial algorithm.

[assembly: AllowPartiallyTrustedCallers()]
namespace SKGL
{ 
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
                return base10ToBase26((getEightByteHash(result.ToString()) + result.ToString()));
            }
            else
            {
                // if password is set, return an encrypted 
                return base10ToBase26((getEightByteHash(result.ToString()) + _encText(result.ToString(), _secretPhase)));
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
}