using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using BrickEmulator.Network;

namespace BrickEmulator.HabboHotel.Processing
{
    /// <summary>
    /// Handles the main intervals, ofcourse without threading <3
    /// </summary>
    class ProgressWorker : IDisposable
    {
        #region Fields
        /// <summary>
        /// x Amount of pixels given every interval.
        /// </summary>
        private const int PIXELS_GIVEN = 50;

        /// <summary>
        /// x Time the timer activates the interval in (milisec)
        /// </summary>
        private const int PIXELS_REFILL = 1000 * 60 * 5;

        private Timer Timer;
        #endregion

        #region Constructors
        public ProgressWorker()
        {
            Timer = new Timer(new TimerCallback(Interval), Timer, 5000, PIXELS_REFILL);
        }
        #endregion

        #region Methods
        private void Interval(Object e)
        {
            foreach (SocketClient Client in BrickEngine.GetSocketShield().Sessions)
            {
                if (Client.GetClient().IsValidUser)
                {
                    Client.GetClient().GetUser().Pixels += PIXELS_GIVEN;
                    Client.GetClient().GetUser().UpdatePixels(true, PIXELS_GIVEN);
                }
            }

            if (BrickEngine.GetUserReactor().NeedsRespectUpdate)
            {
                BrickEngine.GetUserReactor().UpdateRespect(true);
            }
        }

        #endregion

        #region IDisposable members

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
