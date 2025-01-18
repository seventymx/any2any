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
///     Represents a link between a record and a record group.
///     Intermediary table for many-to-many relationship between records and record groups.
/// </summary>
[Table("RecordGroupLinks")]
public class RecordGroupLink
{
    /// <summary>
    ///     The unique identifier for the link.
    /// </summary>
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    ///     The record group in the link.
    /// </summary>
    public Guid RecordGroupId { get; init; }

    /// <summary>
    ///     Navigation property to the record group in the link.
    /// </summary>
    [Required]
    public virtual RecordGroup RecordGroup { get; init; } = null!;

    /// <summary>
    ///     The record in the link.
    /// </summary>
    public Guid RecordId { get; init; }

    /// <summary>
    ///     Navigation property to the record in the link.
    /// </summary>
    [Required]
    public virtual Record Record { get; init; } = null!;
}