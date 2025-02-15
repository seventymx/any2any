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

namespace Any2Any.Prototype.Helpers.Parser;

/// <summary>
///     Integer parser implementation.
/// </summary>
public class IntegerParser : IDataParser
{
    public object Deserialize(string data) => BitConverter.ToInt32(Convert.FromBase64String(data));
    public string Serialize(object value) => Convert.ToBase64String(BitConverter.GetBytes((int)value));
}