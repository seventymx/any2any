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
///     Represents a row in the entity.
/// </summary>
[Table("Records")]
public class Record
{
    /// <summary>
    ///     Unique identifier for the record.
    /// </summary>
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    ///     Foreign key to the parent entity.
    /// </summary>
    [ForeignKey("Entity")]
    public Guid EntityId { get; init; }

    /// <summary>
    ///     Navigation property to the parent entity.
    /// </summary>
    public virtual Entity Entity { get; init; } = null!;

    /// <summary>
    ///     Collection of values in the record.
    /// </summary>
    public virtual ICollection<Value> Values { get; init; } = [];
}