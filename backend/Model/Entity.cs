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

namespace Any2Any.Prototype.Model;

/// <summary>
///     Represents the entire table or entity.
/// </summary>
[Table("Entities")]
public class Entity
{
    /// <summary>
    ///     Unique identifier for the entity.
    /// </summary>
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    ///     Name of the entity for identification purposes.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Name { get; init; } = null!;

    /// <summary>
    ///     Collection of properties (columns) in the entity.
    /// </summary>
    public virtual ICollection<EntityProperty> Properties { get; init; } = [];

    /// <summary>
    ///     Collection of records (rows) in the entity.
    /// </summary>
    public virtual ICollection<Record> Records { get; init; } = [];

    /// <summary>
    ///     Link records of this entity to another entity based on a specific property.
    /// </summary>
    public IEnumerable<(Record Record1, Record Record2)> LinkRecords(Entity otherEntity, string propertyName)
    {
        var property1 = Properties.FirstOrDefault(p => p.Name == propertyName);
        var property2 = otherEntity.Properties.FirstOrDefault(p => p.Name == propertyName);

        if (property1 == null || property2 == null)
            throw new ArgumentException($"Property '{propertyName}' not found in one or both entities.");

        return from record1 in Records
            from record2 in otherEntity.Records
            where record1.Values.FirstOrDefault(v => v.PropertyId == property1.Id)?.Data ==
                  record2.Values.FirstOrDefault(v => v.PropertyId == property2.Id)?.Data
            select (record1, record2);
    }
}