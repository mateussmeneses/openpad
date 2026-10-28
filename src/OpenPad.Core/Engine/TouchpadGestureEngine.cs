using System;

namespace OpenPad.Core.Engine;

/// <summary>
/// High-level gestures recognized on controller touch surfaces (e.g. DualShock 4 / DualSense touchpad).
/// </summary>
public enum TouchGestureType
{
    None,
    SingleTap,
    TwoFingerTap,
    SwipeLeft,
    SwipeRight,
    SwipeUp,
    SwipeDown,
    PinchIn,
    PinchOut
}

/// <summary>
/// Normalized single touch contact point on the touchpad surface [0.0 .. 1.0].
/// </summary>
public record TouchPoint(
    float X,
    float Y,
    bool IsDown
);

/// <summary>
/// Discrete event emitted when a capacitive touchpad gesture is recognized.
/// </summary>
public record TouchGestureEvent(
    TouchGestureType Gesture,
    float Magnitude,
    DateTime Timestamp
);

/// <summary>
/// Multi-touch gesture recognition engine for gamepads equipped with capacitive touch surfaces.
/// Synthesizes taps, two-finger clicks, directional swipes, and pinch-to-zoom gestures.
/// </summary>
public class TouchpadGestureEngine
{
    public double TapMaxDurationMs { get; set; } = 250.0;
    public float TapMaxDistance { get; set; } = 0.08f;
    public float SwipeMinDistance { get; set; } = 0.20f;
    public double SwipeMaxDurationMs { get; set; } = 400.0;

    private TouchPoint _prevTouch0 = new(0f, 0f, false);
    private TouchPoint _prevTouch1 = new(0f, 0f, false);
    private TouchPoint _startTouch0 = new(0f, 0f, false);
    private TouchPoint _startTouch1 = new(0f, 0f, false);
    private DateTime _touch0StartTime;
    private DateTime _touch1StartTime;
    private float _initialPinchDistance;

    public event Action<TouchGestureEvent>? OnGesture;

    /// <summary>
    /// Processes a single multi-touch temporal frame and evaluates gestures.
    /// </summary>
    public TouchGestureType ProcessFrame(TouchPoint touch0, TouchPoint touch1, DateTime now)
    {
        TouchGestureType detected = TouchGestureType.None;
        float magnitude = 1.0f;

        // 1. Evaluate Pinch when both fingers are active
        if (touch0.IsDown && touch1.IsDown)
        {
            float dist = Distance(touch0.X, touch0.Y, touch1.X, touch1.Y);
            if (!_prevTouch0.IsDown || !_prevTouch1.IsDown)
            {
                _initialPinchDistance = dist;
            }
            else if (_initialPinchDistance > 0.05f)
            {
                float deltaDist = dist - _initialPinchDistance;
                if (deltaDist > 0.15f)
                {
                    detected = TouchGestureType.PinchOut;
                    magnitude = deltaDist;
                    _initialPinchDistance = dist; // reset baseline
                }
                else if (deltaDist < -0.15f)
                {
                    detected = TouchGestureType.PinchIn;
                    magnitude = Math.Abs(deltaDist);
                    _initialPinchDistance = dist;
                }
            }
        }

        // 2. Touch 0 Lifecycle (Press down)
        if (touch0.IsDown && !_prevTouch0.IsDown)
        {
            _startTouch0 = touch0;
            _touch0StartTime = now;
        }

        // 3. Touch 1 Lifecycle (Press down)
        if (touch1.IsDown && !_prevTouch1.IsDown)
        {
            _startTouch1 = touch1;
            _touch1StartTime = now;
        }

        // 4. Two-Finger Tap Evaluation (Both released together)
        if (!touch0.IsDown && _prevTouch0.IsDown && !touch1.IsDown && _prevTouch1.IsDown)
        {
            double duration0 = (now - _touch0StartTime).TotalMilliseconds;
            double duration1 = (now - _touch1StartTime).TotalMilliseconds;
            if (duration0 <= TapMaxDurationMs && duration1 <= TapMaxDurationMs)
            {
                detected = TouchGestureType.TwoFingerTap;
            }
        }
        // 5. Single Touch 0 Release Evaluation (Single Tap or Swipe)
        else if (!touch0.IsDown && _prevTouch0.IsDown && !touch1.IsDown)
        {
            double duration = (now - _touch0StartTime).TotalMilliseconds;
            float dx = _prevTouch0.X - _startTouch0.X;
            float dy = _prevTouch0.Y - _startTouch0.Y;
            float totalDist = MathF.Sqrt(dx * dx + dy * dy);

            if (duration <= TapMaxDurationMs && totalDist <= TapMaxDistance)
            {
                detected = TouchGestureType.SingleTap;
            }
            else if (duration <= SwipeMaxDurationMs && totalDist >= SwipeMinDistance)
            {
                if (Math.Abs(dx) > Math.Abs(dy) * 1.2f)
                {
                    detected = dx > 0 ? TouchGestureType.SwipeRight : TouchGestureType.SwipeLeft;
                    magnitude = Math.Abs(dx);
                }
                else if (Math.Abs(dy) > Math.Abs(dx) * 1.2f)
                {
                    detected = dy > 0 ? TouchGestureType.SwipeDown : TouchGestureType.SwipeUp;
                    magnitude = Math.Abs(dy);
                }
            }
        }

        _prevTouch0 = touch0;
        _prevTouch1 = touch1;

        if (detected != TouchGestureType.None)
        {
            OnGesture?.Invoke(new TouchGestureEvent(detected, magnitude, now));
        }

        return detected;
    }

    private static float Distance(float x1, float y1, float x2, float y2)
    {
        float dx = x1 - x2;
        float dy = y1 - y2;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
}
