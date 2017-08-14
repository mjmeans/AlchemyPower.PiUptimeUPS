using System;
using System.Diagnostics;
using Windows.Devices.Gpio;
using Windows.UI.Xaml;
using Windows.System;

namespace PiUptimeUPS_Sample1
{
    public class MainPageViewModel : ViewModelBase, IDisposable
    {
        private const int BTTY_LOW_PIN = 26;
        private string _gpioStatus;
        private string _gpio26PinValue;
        private GpioPin _gpio26Pin;
        private DispatcherTimer _gpioPollTimer;
        private int _countdown;

        public MainPageViewModel()
        {
            GpioStatus = "n/a";
            Gpio26PinValue = "n/a";
            InitGPIO();
        }

        public string Gpio26PinValue
        {
            get { return this._gpio26PinValue; }
            set { this.SetProperty(ref this._gpio26PinValue, value); }
        }

        public string GpioStatus
        {
            get { return this._gpioStatus; }
            set { this.SetProperty(ref this._gpioStatus, value); }
        }

        void SetGpioStatusText(string value)
        {
            GpioStatus = string.Format("{0} {1}", DateTime.Now.ToLocalTime(), value);
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
            SetGpioStatusText("GPIO pin initialized correctly.");
            _gpioPollTimer = new DispatcherTimer();
            _gpioPollTimer.Interval = TimeSpan.FromMilliseconds(1000);
            _gpioPollTimer.Tick += gpioPollTimer_Tick;
            _gpioPollTimer.Start();
        }

        private void gpioPollTimer_Tick(object sender, object e)
        {
            try
            {
                var gpio26 = _gpio26Pin.Read();
                Gpio26PinValue = gpio26.ToString();
                Debug.WriteLine("{0} {1}", DateTime.Now.ToLocalTime(), gpio26);
                if (gpio26 == GpioPinValue.Low)
                {
                    if (_countdown == 1)
                    {
                        SetGpioStatusText(string.Format("Shutting down NOW!", _countdown));
                        //ShutdownHelper(ShutdownKind.Shutdown);
                        return;
                    }
                    if (_countdown == 0)
                    {
                        _countdown = 10;
                    }
                    else
                    {
                        _countdown -= 1;
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
                Debug.WriteLine("{0} {1} {2}", DateTime.Now.ToLocalTime(), "Error reading pins: ", ex.Message);
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
                    if (_gpioPollTimer != null)
                    {
                        _gpioPollTimer.Stop();
                        _gpioPollTimer = null;
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
