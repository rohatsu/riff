// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Threading;

namespace RIFF.Core
{
    internal abstract class RFActiveComponent
    {
        public IRFLog Log { get { return RFStatic.Log; } }

        protected string _componentID;

        protected RFComponentContext _context;

        protected bool _isExiting;

        protected object _sync;

        protected Thread _thread;

        public RFActiveComponent(RFComponentContext context)
        {
            _context = context;
            _isExiting = false;
            _sync = new object();
            _componentID = RFComponentCounter.GetNextComponentID();
        }

        public void Shutdown()
        {
            Log.Debug(this, "Shutdown signal.");
            lock (_sync)
            {
                _isExiting = true;
            }
            try
            {
                Stop();
            }
            catch (Exception ex)
            {
                Log.Warning(this, "Error in RFActiveComponent.Stop: {0}", ex.Message);
            }
        }

        public Thread StartThread()
        {
            _thread = new Thread(InternalRun);
            _context.ActiveComponents.Add(this);
            _thread.Start();
            return _thread;
        }

        public override string ToString()
        {
            return String.Format("{0}:{1}", _componentID, GetType().Name);
        }

        protected void InternalRun()
        {
            Log.Debug(this, "Starting.");
            try
            {
                Run();
            }
            catch (ThreadInterruptedException)
            {
                Log.Debug(this, "Aborting RFActiveComponent thread");
            }
            catch (Exception ex)
            {
                Log.Warning(this, "Error in RFActiveComponent.Run: {0}", ex.Message);
            }
            Log.Debug(this, "Exiting.");
            Shutdown();
        }

        protected bool IsExiting()
        {
            lock (_sync)
            {
                return _isExiting;
            }
        }

        protected abstract void Run();

        protected virtual void Stop()
        {
        }
    }
}
