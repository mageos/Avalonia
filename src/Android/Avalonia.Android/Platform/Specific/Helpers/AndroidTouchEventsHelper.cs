using Android.Graphics;
using Android.Views;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.Platform;
using System;

namespace Avalonia.Android.Platform.Specific.Helpers
{
    public class AndroidTouchEventsHelper<TView> : IDisposable where TView : View, ITopLevelImpl, IAndroidView
    {
        private TView _view;
        public bool HandleEvents { get; set; }

        public AndroidTouchEventsHelper(TView view, Func<IInputRoot> getInputRoot, Func<MotionEvent, Point> getPointfunc)
        {
            this._view = view;
            HandleEvents = true;
            _getPointFunc = getPointfunc;
            _getInputRoot = getInputRoot;
        }

        private DateTime _lastTouchMoveEventTime = DateTime.Now;
        private Point? _lastTouchMovePoint;
        private Func<MotionEvent, Point> _getPointFunc;
        private Func<IInputRoot> _getInputRoot;
        private Point _point;

        public bool? DispatchTouchEvent(MotionEvent e, out bool callBase)
        {
            if (!HandleEvents)
            {
                callBase = true;
                return null;
            }

            RawMouseEventType? mouseEventType = null;
            var eventTime = DateTime.Now;
            //Basic touch support
            switch (e.Action)
            {
                case MotionEventActions.Move:
                    //may be bot flood the evnt system with too many event especially on not so powerfull mobile devices
                    if ((eventTime - _lastTouchMoveEventTime).TotalMilliseconds > 10)
                    {
                        mouseEventType = RawMouseEventType.Move;
                    }
                    break;

                case MotionEventActions.Down:
                    mouseEventType = RawMouseEventType.LeftButtonDown;

                    break;

                case MotionEventActions.Up:
                    mouseEventType = RawMouseEventType.LeftButtonUp;
                    break;
            }

            if (mouseEventType != null)
            {
                //if point is in view otherwise it's possible avalonia not to find the proper window to dispatch the event
                _point = _getPointFunc(e);

                double x = _view.GetX();
                double y = _view.GetY();
                double r = x + _view.Width;
                double b = y + _view.Height;

                if (x <= _point.X && r >= _point.X && y <= _point.Y && b >= _point.Y)
                {
                    var inputRoot = _getInputRoot();
                    var mouseDevice = MouseDevice.Instance;

                    //in order the controls to work in a predictable way
                    //we need to generate mouse move before first mouse down event
                    //as this is the way buttons are working every time
                    //otherwise there is a problem sometimes
                    if (mouseEventType == RawMouseEventType.LeftButtonDown)
                    {
                        var me = new RawMouseEventArgs(mouseDevice, (uint)eventTime.Ticks, inputRoot,
                                    RawMouseEventType.Move, _point, InputModifiers.None);
                        _view.Input(me);
                    }

                    var mouseEvent = new RawMouseEventArgs(mouseDevice, (uint)eventTime.Ticks, inputRoot,
                        mouseEventType.Value, _point, InputModifiers.LeftMouseButton);
                    _view.Input(mouseEvent);

                    if (e.Action == MotionEventActions.Move && mouseDevice.Captured == null)
                    {
                        if (_lastTouchMovePoint != null)
                        {
                            //raise mouse scroll event so the scrollers
                            //are moving with the cursor
                            double vectorX = _point.X - _lastTouchMovePoint.Value.X;
                            double vectorY = _point.Y - _lastTouchMovePoint.Value.Y;
                            //based on test correction of 0.02 is working perfect
                            double correction = 0.02;
                            var ps = AndroidPlatform.Instance.LayoutScalingFactor;
                            var mouseWheelEvent = new RawMouseWheelEventArgs(
                                        mouseDevice,
                                        (uint)eventTime.Ticks,
                                        inputRoot,
                                        _point,
                                        new Vector(vectorX * correction / ps, vectorY * correction / ps), InputModifiers.LeftMouseButton);
                            _view.Input(mouseWheelEvent);
                        }
                        _lastTouchMovePoint = _point;
                        _lastTouchMoveEventTime = eventTime;
                    }
                    else if (e.Action == MotionEventActions.Down)
                    {
                        _lastTouchMovePoint = _point;
                    }
                    else
                    {
                        _lastTouchMovePoint = null;
                    }
                }
            }

            callBase = true;
            //if return false events for move and up are not received!!!
            return e.Action != MotionEventActions.Up;
        }

        private Paint _paint;

        public void Dispose()
        {
            HandleEvents = false;
        }
    }
}