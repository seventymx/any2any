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

using System.Collections.Concurrent;

namespace Any2Any.Prototype.Model.Logger;

public class LogListener : IDisposable
{
    private readonly ILogger _logger;
    private readonly ConcurrentQueue<LogMessage> _logQueue;
    private readonly IDisposable _logSubscription;

    public LogListener(ILogger logger, ConcurrentQueue<LogMessage> logQueue)
    {
        _logger = logger;
        _logQueue = logQueue;

        // Hook into the logger using a custom logger provider
        _logSubscription = LoggerProvider.AddListener(OnLogReceived);
    }

    public void Dispose() => _logSubscription?.Dispose();

    private void OnLogReceived(LogLevel logLevel, string message, Exception? exception)
    {
        var logMessage = new LogMessage
        {
            Timestamp = DateTime.UtcNow.ToString("o"), // ISO 8601 format
            Level = logLevel.ToString(),
            Message = message
        };

        if (exception != null) logMessage.Message += $"\nException: {exception.Message}\n{exception.StackTrace}";

        _logQueue.Enqueue(logMessage);
    }
}