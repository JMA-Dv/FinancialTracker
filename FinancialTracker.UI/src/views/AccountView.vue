<script setup lang="ts">
import { ElButton } from 'element-plus'
import { computed, onMounted, ref } from 'vue'
import AccountHistoryTable from '@/components/AccountHistoryTable.vue'
import BalanceAdjustmentDialog from '@/components/BalanceAdjustmentDialog.vue'
import BalanceOnDate from '@/components/BalanceOnDate.vue'
import ErrorAlert from '@/components/ErrorAlert.vue'
import LoadFailure from '@/components/LoadFailure.vue'
import {
  adjustmentName,
  openingBalanceId,
  useAccountHistory,
  type BalanceAdjustmentRequest,
  type HistoryEntry,
} from '@/composables/useAccountHistory'
import { useSubmit } from '@/composables/useSubmit'
import { confirmAction } from '@/confirm'
import { formatMoney } from '@/money'

const props = defineProps<{ id: string }>()
const history = useAccountHistory(() => props.id)

// A failed refresh keeps the Account already shown, and says so above it.
const account = computed(() => history.account.value)
const openingId = computed(() => openingBalanceId(history.entries.value))
const adding = ref(false)
const editing = ref<HistoryEntry | null>(null)

function saveEdit(request: BalanceAdjustmentRequest): Promise<void> {
  // The dialog only exists while an entry is being edited.
  return editing.value === null ? Promise.resolve() : history.editAdjustment(editing.value.id, request)
}

const deleting = useSubmit((entry: HistoryEntry) => history.deleteAdjustment(entry.id))

async function remove(entry: HistoryEntry): Promise<void> {
  const opening = entry.id === openingId.value
  const message = opening
    ? 'This is the Opening Balance. The next earliest Balance Adjustment becomes the Opening Balance; with none left, the Account starts at zero and every Transaction and Transfer counts toward its balance.'
    : 'The balance goes back to what the Transactions, Transfers and other Balance Adjustments say.'
  const title = `Delete ${adjustmentName(opening)} of ${entry.date}?`
  if (await confirmAction(title, message, 'Delete')) {
    await deleting.submit(entry)
  }
}

onMounted(() => history.load())
</script>

<template>
  <section>
    <RouterLink to="/accounts">← Accounts</RouterLink>
    <h1>{{ account?.name ?? 'Account' }}</h1>

    <LoadFailure v-if="history.status.value === 'failed'" title="Could not load this Account" :description="history.failure.value" @retry="history.load()" />

    <p v-if="history.status.value === 'idle' || (history.status.value === 'loading' && !account)" role="status">
      Loading history…
    </p>
    <template v-else-if="account">
      <p>
        Balance:
        <strong data-testid="balance">{{ formatMoney(account.balance, account.currency) }}</strong>
      </p>
      <p v-if="account.isClosed" id="account-closed-reason">
        Reopen it first to add, edit or delete Balance Adjustments.
      </p>
      <p>The earliest Balance Adjustment is the Account's Opening Balance.</p>
      <ElButton
        type="primary"
        :disabled="account.isClosed"
        aria-describedby="account-closed-reason"
        @click="adding = true"
      >
        Add Balance Adjustment
      </ElButton>
      <ErrorAlert v-if="deleting.error.value" :title="deleting.error.value" class="account__alert" />
      <BalanceOnDate :account="account" />
      <p v-if="history.entries.value.length === 0">Nothing has happened on this Account yet.</p>
      <AccountHistoryTable
        v-else
        :entries="history.entries.value"
        :currency="account.currency"
        :opening-id="openingId"
        :locked="account.isClosed"
        @edit="editing = $event"
        @delete="remove"
      />
      <BalanceAdjustmentDialog
        v-if="adding"
        :account="account"
        :save="history.addAdjustment"
        @close="adding = false"
        @saved="adding = false"
      />
      <BalanceAdjustmentDialog
        v-if="editing"
        :account="account"
        :existing="editing"
        :opening="editing.id === openingId"
        :save="saveEdit"
        @close="editing = null"
        @saved="editing = null"
      />
    </template>
  </section>
</template>

<style scoped>
.account__alert {
  margin: 1rem 0;
}
</style>
