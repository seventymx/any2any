/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * Author: Steffen70 <steffen@seventy.mx>
 * Creation Date: 2025-01-26
 *
 * Contributors:
 * - Contributor Name <contributor@example.com>
 */

namespace Any2Any.Prototype.Model.Logger;

public class LoggerProvider : ILoggerProvider
{
    private readonly List<Action<LogLevel, string, Exception?>> _listeners = new();

    private LoggerProvider()
    {
    }

    public static LoggerProvider Instance { get; } = new();

    public ILogger CreateLogger(string categoryName) => new CustomLogger(categoryName, _listeners);

    public void Dispose()
    {
    }

    public static IDisposable AddListener(Action<LogLevel, string, Exception?> listener)
    {
        Instance._listeners.Add(listener);
        return new ListenerDisposer(() => Instance._listeners.Remove(listener));
    }

    private class CustomLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly List<Action<LogLevel, string, Exception?>> _listeners;

        public CustomLogger(string categoryName, List<Action<LogLevel, string, Exception?>> listeners)
        {
            _categoryName = categoryName;
            _listeners = listeners;
        }

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            foreach (var listener in _listeners) listener(logLevel, message, exception);
        }
    }

    private class ListenerDisposer : IDisposable
    {
        private readonly Action _disposeAction;

        public ListenerDisposer(Action disposeAction) => _disposeAction = disposeAction;

        public void Dispose()
        {
            _disposeAction();
        }
    }

    private class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}