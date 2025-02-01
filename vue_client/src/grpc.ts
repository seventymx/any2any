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
