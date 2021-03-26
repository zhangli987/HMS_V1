namespace Nxr.Internal
{
    public interface INibriuGestureListener
    {
        /// <summary>
        ///  The result of Gesture.
        /// </summary>
        /// <param name="gesture"></param>
        void OnGesture(GESTURE_ID gesture);
    }
}
