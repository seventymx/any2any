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

public class CertificateSettings
{
    // The path to the certificate file (without file extension)
    public string Path { get; init; } = null!;

    // The password for the certificate file
    public string Password { get; init; } = null!;
}