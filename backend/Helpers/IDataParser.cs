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

namespace Any2Any.Prototype.Helpers;

/// <summary>
///     Interface for data serialization and deserialization strategies.
/// </summary>
public interface IDataParser
{
    object Deserialize(string data);
    string Serialize(object value);
}