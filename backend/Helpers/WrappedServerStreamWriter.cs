/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * Author: Steffen70 <steffen@seventy.mx>
 * Creation Date: 2025-02-01
 *
 * Contributors:
 * - Contributor Name <contributor@example.com>
 */

using Grpc.Core;

namespace Any2Any.Prototype.Helpers;

public class WrappedServerStreamWriter<T>(IServerStreamWriter<T> innerStreamWriter, Action onFinalMessageSent)
    : IServerStreamWriter<T>
{
    public bool IsFinalMessage { get; set; }

    public WriteOptions? WriteOptions
    {
        get => innerStreamWriter.WriteOptions;
        set => innerStreamWriter.WriteOptions = value;
    }

    public async Task WriteAsync(T message)
    {
        if (IsFinalMessage) onFinalMessageSent();

        await innerStreamWriter.WriteAsync(message);
    }
}