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

using Any2Any.Prototype.Model.Parser;

namespace Any2Any.Prototype.Model;

/// <summary>
///     Base class for serialization and deserialization strategies.
/// </summary>
public static class DataParserFactory
{
    private static readonly Dictionary<DataType, IDataParser> Parsers = new()
    {
        { DataType.String, new StringParser() },
        { DataType.Integer, new IntegerParser() },
        { DataType.Decimal, new DecimalParser() },
        { DataType.DateTime, new DateTimeParser() }
    };

    public static IDataParser GetParser(DataType dataType) => Parsers[dataType];
}