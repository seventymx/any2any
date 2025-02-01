<script setup lang="ts">
/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.

  Author: Steffen70 <steffen@seventy.mx>
  Creation Date: 2025-02-02

  Contributors:
  - Contributor Name <contributor@example.com>
*/

import { ref, onMounted } from "vue";
import { sayHello } from "./grpc";

const message = ref<string>("Loading...");

onMounted(async () => {
    message.value = await sayHello();
});
</script>

<template>
    <div class="min-h-screen flex flex-col items-center justify-center text-gray-900">
        <div class="bg-white shadow-lg rounded-lg p-6 max-w-sm w-full">
            <h1 class="text-xl font-bold text-center">gRPC Vue 3 Client</h1>
            <p class="mt-4 text-center text-blue-500">{{ message }}</p>
        </div>
    </div>
</template>

<style scoped lang="scss">
h1 {
    color: #1e40af;
}
p {
    font-size: 1.2rem;
}
</style>
