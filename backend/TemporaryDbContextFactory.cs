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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Any2Any.Prototype;

public class Any2AnyDbContextFactory : IDesignTimeDbContextFactory<Any2AnyDbContext>
{
    public Any2AnyDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<Any2AnyDbContext>()
            .UseSqlite("Data Source=any2any.db")
            .Options;

        return new(options);
    }
}