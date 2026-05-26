<script setup lang="ts">
import { computed } from 'vue'
import DisconnectOverlay from '@/components/DisconnectOverlay.vue'
import { useConnectionStore } from '@/stores/connection'

const connectionStore = useConnectionStore()
const isDisconnected = computed(() => !connectionStore.connected)
</script>

<template>
  <div class="h-screen flex flex-col">
    <router-view />

    <!-- 断连遮罩：界面冻结 + 断连提示 -->
    <DisconnectOverlay v-if="isDisconnected" />
  </div>
</template>
