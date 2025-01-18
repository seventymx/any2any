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

[Table("RecordGroups")]
public class RecordGroup
{
    /// <summary>
    ///     Unique identifier for the record group.
    /// </summary>
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    ///     Many-to-many relationship between records and record groups.
    /// </summary>
    public virtual ICollection<RecordGroupLink> RecordGroupLinks { get; init; } = [];
}