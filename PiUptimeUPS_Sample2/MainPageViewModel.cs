using System;
using System.Diagnostics;
using Windows.ApplicationModel.Core;
using Windows.Devices.Gpio;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.System;
using System.Threading.Tasks;

namespace PiUptimeUPS_Sample2
{
    public class MainPageViewModel : ViewModelBase, IDisposable
    {
        private const int BTTY_LOW_PIN = 26;
        private string _gpioStatus;
        private GpioPin _gpio26Pin;
        private DispatcherTimer _shutdownTimeoutTimer;
        private int _countdown;

        public MainPageViewModel()
        {
            GpioStatus = "n/a";
            InitGPIO();
        }

        public string GpioStatus
        {
            get { return this._gpioStatus; }
            set { this.SetProperty(ref this._gpioStatus, value); }
        }

        void SetGpioStatusText(string value)
        {
            var t = string.Format("{0} {1}", DateTime.Now.ToLocalTime(), value);
            GpioStatus = t;
            Debug.WriteLine(t);
        }

        private void InitGPIO()
        {

            var gpio = GpioController.GetDefault();
            if (gpio == null)
            {
                SetGpioStatusText("There is no GPIO controller on this device.");
                return;
            }
            _gpio26Pin = gpio.OpenPin(BTTY_LOW_PIN);
            _gpio26Pin.SetDriveMode(GpioPinDriveMode.InputPullUp);
            _gpio26Pin.DebounceTimeout = TimeSpan.FromMilliseconds(100);
            _gpio26Pin.ValueChanged += _gpio26Pin_ValueChanged;
            SetGpioStatusText("GPIO pin initialized correctly. Waiting for event.");
            _shutdownTimeoutTimer = new DispatcherTimer();
            _shutdownTimeoutTimer.Interval = TimeSpan.FromMilliseconds(1000);
            _shutdownTimeoutTimer.Tick += _shutdownTimeoutTimer_Tick;
        }

        private void _gpio26Pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            var window = CoreApplication.MainView.CoreWindow;
            switch (args.Edge)
            {
                case GpioPinEdge.FallingEdge:
                    _countdown = 10;
                    window.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        SetGpioStatusText(string.Format("Shutting down in {0} seconds!", _countdown));
                        _shutdownTimeoutTimer.Start();
                    });
                    break;
                default:
                    window.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        _shutdownTimeoutTimer.Stop();
                        SetGpioStatusText("Shutdown aborted.");
                    });
                    _countdown = 0;
                    break;
            }
        }

        private void _shutdownTimeoutTimer_Tick(object sender, object e)
        {
            try
            {
                var gpio26 = _gpio26Pin.Read();
                if (gpio26 == GpioPinValue.Low)
                {
                    _countdown -= 1;
                    if (_countdown == 0)
                    {
                        SetGpioStatusText(string.Format("Shutting down NOW!", _countdown));
                        //ShutdownHelper(ShutdownKind.Shutdown);
                        return;
                    }
                    SetGpioStatusText(string.Format("Shutting down in {0} seconds!",_countdown));
                }
                else
                {
                    _countdown = 0;
                    SetGpioStatusText("Polling");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("{0} {1} {2}", DateTime.Now.ToLocalTime(), "Error in _shutdownTimeoutTimer_Tick: ", ex.Message);
            }
            finally { }
        }

        private void ShutdownHelper(ShutdownKind kind)
        {
            new System.Threading.Tasks.Task(() =>
            {
                ShutdownManager.BeginShutdown(kind, TimeSpan.FromSeconds(0));
            }).Start();
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_shutdownTimeoutTimer != null)
                    {
                        _shutdownTimeoutTimer.Stop();
                        _shutdownTimeoutTimer = null;
                    }
                    if (_gpio26Pin != null)
                    {
                        _gpio26Pin.Dispose();
                        _gpio26Pin = null;
                    }
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
