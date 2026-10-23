<script setup lang="ts">
import { ElButton } from 'element-plus'
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import ErrorAlert from '@/components/ErrorAlert.vue'
import LoadFailure from '@/components/LoadFailure.vue'
import TransactionDialog from '@/components/TransactionDialog.vue'
import TransactionFilters from '@/components/TransactionFilters.vue'
import TransactionTable from '@/components/TransactionTable.vue'
import { useLedgerLookups } from '@/composables/useLedgerLookups'
import { useSubmit } from '@/composables/useSubmit'
import { useTransactions } from '@/composables/useTransactions'
import { confirmAction } from '@/confirm'
import { usePayeesStore } from '@/stores/payees'
import {
  filterFromQuery,
  isFiltered,
  queryFromFilter,
  transactionsPath,
  type SaveTransactionRequest,
  type Transaction,
  type TransactionFilter,
} from '@/transactionRules'

const route = useRoute()
const router = useRouter()
const payees = usePayeesStore()
const lookups = useLedgerLookups()

// The URL holds the filters, so a link from another screen can open a filtered list.
const filter = computed(() => filterFromQuery(route.query))
const transactions = useTransactions(() => filter.value)

const recording = ref(false)
const editing = ref<Transaction | null>(null)

const hasOpenAccount = computed(() => lookups.accounts.value.some((account) => !account.isClosed))
const noOpenAccount = computed(() => lookups.status.value === 'ready' && !hasOpenAccount.value)
// A failed refresh keeps the Transactions already shown, and says so above them.
const showTable = computed(
  () => lookups.status.value === 'ready' && (transactions.transactions.value.length > 0 || transactions.status.value === 'ready'),
)

const deleting = useSubmit((transaction: Transaction) => transactions.remove(transaction.id))

function changeFilter(next: TransactionFilter): void {
  void router.replace({ query: queryFromFilter(next) })
}

function saveEdit(request: SaveTransactionRequest): Promise<void> {
  // The dialog only exists while a Transaction is being edited.
  return editing.value === null ? Promise.resolve() : transactions.edit(editing.value.id, request)
}

async function remove(transaction: Transaction): Promise<void> {
  const message = 'The Account\'s balance goes back to what it would be without it.'
  if (await confirmAction(`Delete the Transaction of ${transaction.date}?`, message, 'Delete')) {
    await deleting.submit(transaction)
  }
}

// Leaving the screen changes the route's query too, and that is not a new filter.
watch(
  () => transactionsPath(filter.value),
  () => {
    if (route.name === 'transactions') {
      void transactions.load()
    }
  },
)

onMounted(() => {
  void lookups.load()
  void payees.load()
  void transactions.load()
})
</script>

<template>
  <section>
    <h1>Transactions</h1>

    <ElButton type="primary" :disabled="!hasOpenAccount" :aria-describedby="noOpenAccount ? 'no-open-account' : undefined" @click="recording = true">
      Record transaction
    </ElButton>
    <span v-if="noOpenAccount" id="no-open-account">
      Open an Account first to record Transactions.
    </span>

    <ErrorAlert v-if="deleting.error.value" :title="deleting.error.value" class="transactions__alert" />
    <LoadFailure
      v-if="lookups.status.value === 'failed'"
      title="Could not load your Accounts and Categories"
      :description="lookups.failure.value"
      @retry="lookups.load()"
    />
    <LoadFailure
      v-if="payees.status === 'failed'"
      title="Could not load your Payees"
      :description="payees.failure"
      @retry="payees.load()"
    />
    <LoadFailure
      v-if="transactions.status.value === 'failed'"
      title="Could not load your Transactions"
      :description="transactions.failure.value"
      @retry="transactions.load()"
    />

    <TransactionFilters
      v-if="lookups.status.value === 'ready'"
      :filter="filter"
      :accounts="lookups.accounts.value"
      :categories="lookups.categories.value"
      :payees="payees.payees"
      @change="changeFilter"
    />

    <p v-if="transactions.status.value === 'idle' || (transactions.status.value === 'loading' && !showTable)" role="status">
      Loading Transactions…
    </p>
    <template v-else-if="showTable">
      <p v-if="transactions.transactions.value.length === 0">
        {{ isFiltered(filter) ? 'No Transactions match these filters.' : 'No Transactions yet.' }}
      </p>
      <TransactionTable
        v-else
        :transactions="transactions.transactions.value"
        :accounts="lookups.accounts.value"
        :categories="lookups.categories.value"
        @edit="editing = $event"
        @delete="remove"
      />
    </template>

    <TransactionDialog
      v-if="recording"
      :accounts="lookups.accounts.value"
      :categories="lookups.categories.value"
      :save="transactions.create"
      @close="recording = false"
      @saved="recording = false"
    />
    <TransactionDialog
      v-if="editing"
      :existing="editing"
      :accounts="lookups.accounts.value"
      :categories="lookups.categories.value"
      :save="saveEdit"
      @close="editing = null"
      @saved="editing = null"
    />
  </section>
</template>

<style scoped>
.transactions__alert {
  margin: 1rem 0;
}
</style>
