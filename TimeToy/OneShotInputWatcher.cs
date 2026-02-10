using System;
using System.Windows;
using System.Windows.Input;

namespace TimeToy
{
    /// <summary>
    /// Watches for the next mouse or key input on a UIElement and runs a caller-supplied action once.
    /// Handlers remove themselves after firing. Caller controls what happens on trigger.
    /// </summary>
    public sealed class OneShotInputWatcher : IDisposable
    {
        private readonly UIElement _owner;
        private readonly Action _onTriggered;
        private readonly Action _onCleanup; // optional extra cleanup action (null ok)

        private MouseButtonEventHandler _oneTimeMouseHandler;
        private KeyEventHandler _oneTimeKeyHandler;
        private bool _isWatching;

        public OneShotInputWatcher(UIElement owner, Action onTriggered, Action onCleanup = null)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _onTriggered = onTriggered ?? throw new ArgumentNullException(nameof(onTriggered));
            _onCleanup = onCleanup;
        }

        public void StartWatching()
        {
            if (_isWatching) return;

            // remove any stale handlers first
            if (_oneTimeMouseHandler != null)
            {
                try { _owner.RemoveHandler(Mouse.PreviewMouseDownEvent, _oneTimeMouseHandler); } catch { }
                _oneTimeMouseHandler = null;
            }
            if (_oneTimeKeyHandler != null)
            {
                try { _owner.RemoveHandler(UIElement.KeyDownEvent, _oneTimeKeyHandler); } catch { }
                _oneTimeKeyHandler = null;
            }

            _oneTimeMouseHandler = new MouseButtonEventHandler((s, e) =>
            {
                try
                {
                    _onTriggered();
                }
                catch (Exception ex)
                {
                    try { ErrorLogging.Log(ex, "Error in one-shot mouse handler."); } catch { }
                }
                finally
                {
                    TryRemoveHandlers();
                }
                // do NOT set e.Handled = true so normal click processing continues
            });

            _oneTimeKeyHandler = new KeyEventHandler((s, e) =>
            {
                try
                {
                    _onTriggered();
                }
                catch (Exception ex)
                {
                    try { ErrorLogging.Log(ex, "Error in one-shot key handler."); } catch { }
                }
                finally
                {
                    TryRemoveHandlers();
                }
                // do NOT set e.Handled = true so normal key processing continues
            });

            // register so we receive events even if child controls marked them handled
            _owner.AddHandler(Mouse.PreviewMouseDownEvent, _oneTimeMouseHandler, handledEventsToo: true);
            _owner.AddHandler(UIElement.KeyDownEvent, _oneTimeKeyHandler, handledEventsToo: true);

            _isWatching = true;
        }

        private void TryRemoveHandlers()
        {
            try { if (_oneTimeMouseHandler != null) _owner.RemoveHandler(Mouse.PreviewMouseDownEvent, _oneTimeMouseHandler); } catch { }
            try { if (_oneTimeKeyHandler != null) _owner.RemoveHandler(UIElement.KeyDownEvent, _oneTimeKeyHandler); } catch { }
            _oneTimeMouseHandler = null;
            _oneTimeKeyHandler = null;
            _isWatching = false;

            // optional caller cleanup
            try { _onCleanup?.Invoke(); } catch (Exception ex) { try { ErrorLogging.Log(ex, "Error during OneShotInputWatcher cleanup."); } catch { } }
        }

        public void StopWatching()
        {
            if (!_isWatching) return;
            TryRemoveHandlers();
        }

        public void Dispose()
        {
            StopWatching();
        }
    }
}