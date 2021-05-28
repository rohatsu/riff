// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

            try
            {
                if (_thread != null && _thread.IsAlive)
                {
                    _thread.Abort();
                }
            }
            catch (ThreadInterruptedException)
            {
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception ex)
            {
                Log.Info(this, "Error in Shutdown/Abort: {0}", ex.Message);
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
            catch (ThreadAbortException)
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

    public interface IRFBackgroundService
    {
        void StartCommand();

        void StopCommand();

        List<RFInstruction> CustomCommand(string command, string param);
    }

    public abstract class RFBackgroundService : IRFBackgroundService
    {
        protected CancellationTokenSource CancellationSource { get; }

        /// <summary>
        /// Blocking call to run your service, finish with WaitForCancel
        /// </summary>
        public abstract void Start();

        /// <summary>
        /// Cleanup
        /// </summary>
        public abstract void Stop();

        public virtual List<RFInstruction> CustomCommand(string command, string param)
        {
            return null;
        }

        public RFBackgroundService()
        {
            CancellationSource = new CancellationTokenSource();
        }

        public void Shutdown()
        {
            CancellationSource.Cancel();
        }

        public void WaitForCancel()
        {
            CancellationSource.Token.WaitHandle.WaitOne();
        }

        public bool IsExiting()
        {
            return CancellationSource.IsCancellationRequested || RFStatic.IsShutdown;
        }

        public void StartCommand()
        {
            Start();
        }

        public void StopCommand()
        {
            Shutdown();
            Stop();
        }
    }

    internal class RFBackgroundServiceComponent : RFActiveComponent
    {
        private IRFBackgroundService _impl;

        public RFBackgroundServiceComponent(RFComponentContext context, IRFBackgroundService impl) : base(context)
        {
            _impl = impl;
        }

        public List<RFInstruction> Command(string command, string param)
        {
            Log.Info(this, $"Processing service command {command}");
            switch (command.ToLower())
            {
                case RFServiceEvent.START_COMMAND:
                    StartThread();
                    break;
                case RFServiceEvent.STOP_COMMAND:
                    Shutdown();
                    break;
                default:
                    return _impl.CustomCommand(command, param);
            }
            return null;
        }

        protected override void Run()
        {
            _impl.StartCommand();
        }

        protected override void Stop()
        {
            _impl.StopCommand();
        }
    }
}
