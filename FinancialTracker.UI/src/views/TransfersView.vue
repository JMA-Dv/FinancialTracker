<script setup lang="ts">
import { ElButton } from 'element-plus'
import { computed, onMounted, ref } from 'vue'
import ErrorAlert from '@/components/ErrorAlert.vue'
import LoadFailure from '@/components/LoadFailure.vue'
import TransferDialog from '@/components/TransferDialog.vue'
import TransferTable from '@/components/TransferTable.vue'
import { useAllAccounts } from '@/composables/useAllAccounts'
import { useSubmit } from '@/composables/useSubmit'
import { useTransfers, type SaveTransferRequest, type Transfer } from '@/composables/useTransfers'
import { confirmAction } from '@/confirm'

const accounts = useAllAccounts()
const transfers = useTransfers()

const recording = ref(false)
const editing = ref<Transfer | null>(null)

// A Transfer needs two open Accounts.
const canRecord = computed(() => accounts.items.value.filter((account) => !account.isClosed).length >= 2)
const needMoreAccounts = computed(() => accounts.status.value === 'ready' && !canRecord.value)

// A failed refresh keeps the Transfers already shown, and says so above them.
const showTable = computed(
  () => accounts.status.value === 'ready' && (transfers.transfers.value.length > 0 || transfers.status.value === 'ready'),
)

const deleting = useSubmit((transfer: Transfer) => transfers.remove(transfer.id))

function saveEdit(request: SaveTransferRequest): Promise<void> {
  // The dialog only exists while a Transfer is being edited.
  return editing.value === null ? Promise.resolve() : transfers.edit(editing.value.id, request)
}

async function remove(transfer: Transfer): Promise<void> {
  const message = 'Both Accounts\' balances go back to what they would be without it.'
  if (await confirmAction(`Delete the Transfer of ${transfer.date}?`, message, 'Delete')) {
    await deleting.submit(transfer)
  }
}

onMounted(() => {
  void accounts.load()
  void transfers.load()
})
</script>

<template>
  <section>
    <h1>Transfers</h1>

    <ElButton type="primary" :disabled="!canRecord" :aria-describedby="needMoreAccounts ? 'need-accounts' : undefined" @click="recording = true">
      Record transfer
    </ElButton>
    <span v-if="needMoreAccounts" id="need-accounts">Open two Accounts first to record a Transfer.</span>

    <ErrorAlert v-if="deleting.error.value" :title="deleting.error.value" class="transfers__alert" />
    <LoadFailure
      v-if="accounts.status.value === 'failed'"
      title="Could not load your Accounts"
      :description="accounts.failure.value"
      @retry="accounts.load()"
    />
    <LoadFailure
      v-if="transfers.status.value === 'failed'"
      title="Could not load your Transfers"
      :description="transfers.failure.value"
      @retry="transfers.load()"
    />

    <p v-if="transfers.status.value === 'idle' || (transfers.status.value === 'loading' && !showTable)" role="status">
      Loading Transfers…
    </p>
    <template v-else-if="showTable">
      <p v-if="transfers.transfers.value.length === 0">No Transfers yet.</p>
      <TransferTable
        v-else
        :transfers="transfers.transfers.value"
        :accounts="accounts.items.value"
        @edit="editing = $event"
        @delete="remove"
      />
    </template>

    <TransferDialog
      v-if="recording"
      :accounts="accounts.items.value"
      :save="transfers.create"
      @close="recording = false"
      @saved="recording = false"
    />
    <TransferDialog
      v-if="editing"
      :existing="editing"
      :accounts="accounts.items.value"
      :save="saveEdit"
      @close="editing = null"
      @saved="editing = null"
    />
  </section>
</template>

<style scoped>
.transfers__alert {
  margin: 1rem 0;
}
</style>
