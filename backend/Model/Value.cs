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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Any2Any.Prototype.Helpers;

namespace Any2Any.Prototype.Model;

/// <summary>
///     Represents a cell in the entity.
/// </summary>
[Table("Values")]
public class Value
{
    /// <summary>
    ///     Unique identifier for the value.
    /// </summary>
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    ///     The serialized data of the cell as a Base64 string.
    /// </summary>
    [Required]
    public string Data { get; set; } = null!;

    /// <summary>
    ///     The plain text data of the cell. (For debugging purposes)
    /// </summary>
    [Required]
    public string PlainTextData { get; set; } = null!;

    /// <summary>
    ///     The type of the data for deserialization.
    /// </summary>
    [Required]
    public DataType DataType { get; set; }

    /// <summary>
    ///     Foreign key to the parent record.
    /// </summary>
    [ForeignKey("Record")]
    public Guid RecordId { get; init; }

    /// <summary>
    ///     Navigation property to the parent record.
    /// </summary>
    public virtual Record Record { get; init; } = null!;

    /// <summary>
    ///     Foreign key to the associated property.
    /// </summary>
    [ForeignKey("EntityProperty")]
    public Guid PropertyId { get; init; }

    /// <summary>
    ///     Navigation property to the associated property.
    /// </summary>
    public virtual EntityProperty EntityProperty { get; init; } = null!;

    /// <summary>
    ///     Deserialize the data using the appropriate parser.
    /// </summary>
    public object GetDeserializedValue()
    {
        var parser = DataParserFactory.GetParser(DataType);
        return parser.Deserialize(Data);
    }

    /// <summary>
    ///     Serialize the value using the appropriate parser.
    /// </summary>
    public void SetSerializedValue(object value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value), "Value cannot be null.");

        var valueAsString = value.ToString() ?? string.Empty;
        PlainTextData = valueAsString;

        object? parsedValue = null;

        // Determine the data type using a switch statement with pattern matching
        switch (valueAsString)
        {
            case var _ when int.TryParse(valueAsString, out var intValue):
                DataType = DataType.Integer;
                parsedValue = intValue;
                break;

            case var _ when decimal.TryParse(valueAsString, out var decimalValue):
                DataType = DataType.Decimal;
                parsedValue = decimalValue;
                break;

            case var _ when DateTime.TryParse(valueAsString, out var dateTimeValue):
                DataType = DataType.DateTime;
                parsedValue = dateTimeValue;
                break;

            default:
                DataType = DataType.String;
                parsedValue = valueAsString;
                break;
        }

        var parser = DataParserFactory.GetParser(DataType);
        PlainTextData = value.ToString() ?? "";
        Data = parser.Serialize(parsedValue ?? valueAsString);
    }
}