/**
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * Author: Steffen70 <steffen@seventy.mx>
 * Creation Date: 2025-02-02
 *
 * Contributors:
 * - Contributor Name <contributor@example.com>
 */

import { DemoClient } from "../generated/DemoServiceClientPb";
import { Empty } from "google-protobuf/google/protobuf/empty_pb";

const baseAddress = `https://${window.location.host}`;

console.log("baseAddress", baseAddress);

const demoService = new DemoClient(baseAddress, null, null);

export async function sayHello(): Promise<string> {
    try {
        const response = await demoService.helloWorld(new Empty(), {});

        // Get message from response
        return response.getMessage();
    } catch (error) {
        console.error("gRPC error:", error);
        return "Error fetching HelloWorld response.";
    }
}
