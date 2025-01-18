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
///     Represents a link between two records.
///     Intermediary table for many-to-many relationship between records.
///     All records are linked together in to groups by these links.
/// </summary>
[Table("RecordLinks")]
public class RecordLink
{
    /// <summary>
    ///     The unique identifier for the link.
    /// </summary>
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    ///     The first record in the link.
    /// </summary>
    public Guid Record1Id { get; init; }

    /// <summary>
    ///     Navigation property to the first record in the link.
    /// </summary>
    [Required]
    public virtual Record Record1 { get; init; } = null!;

    /// <summary>
    ///     The second record in the link.
    /// </summary>
    public Guid Record2Id { get; init; }

    /// <summary>
    ///     Navigation property to the second record in the link.
    /// </summary>
    [Required]
    public virtual Record Record2 { get; init; } = null!;
}