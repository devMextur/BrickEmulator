using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrickEmulator.HabboHotel.Processing
{
    /// <summary>
    /// Reactor handlers the TimerHandlers.
    /// </summary>
    class ProgressReactor
    {
        #region Fields
        private readonly ProgressCollector Collector = new ProgressCollector();
        private readonly ProgressWorker Worker = new ProgressWorker();
        #endregion

        #region Constructors
        public ProgressReactor() { }
        #endregion

        #region Members
        public ProgressCollector GetCollector()
        {
            return Collector;
        }

        public ProgressWorker GetWorker()
        {
            return Worker;
        }
        #endregion
    }
}
