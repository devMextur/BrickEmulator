using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace BrickEmulator.Security
{
    /// <summary>
    /// A non-static Convertor.
    /// </summary>
    class SecurityConvertor
    {
        #region Fields

        public SecurityConvertor() { }

        #endregion

        #region Methods

        public string ObjectToString(Object e)
        {
            return Convert.ToString(e);
        }

        public Int32 ObjectToInt32(Object e)
        {
            return Convert.ToInt32(e);
        }

        public short ObjectToShort(Object e)
        {
            return Convert.ToInt16(e);
        }

        public UInt32 ObjectToUInt32(Object e)
        {
            return Convert.ToUInt32(e);
        }

        public Boolean ObjectToBoolean(Object e)
        {
            return ObjectToInt32(e) == 1;
        }

        public Double ObjectToDouble(Object e)
        {
            return Convert.ToDouble(e);
        }

        public DateTime ObjectToDateTime(Object e)
        {
            return Convert.ToDateTime(e);
        }

        #endregion
    }
}
