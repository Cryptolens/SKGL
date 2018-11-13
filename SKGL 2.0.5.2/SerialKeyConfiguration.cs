using SKGL.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SKGL
{
    public class SerialKeyConfiguration : BaseConfiguration
    {
        private bool[] _Features = new bool[8]; //when define an array with no initial value,   .Net Framework will set primitive array values as false

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


    }
}
