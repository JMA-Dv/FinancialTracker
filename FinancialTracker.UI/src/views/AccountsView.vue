<script setup lang="ts">
import { ElButton, ElSwitch } from 'element-plus'
import { computed, onMounted, ref } from 'vue'
import AccountSection from '@/components/AccountSection.vue'
import type { AccountAction } from '@/components/accountAction'
import ChangeAccountCurrencyDialog from '@/components/ChangeAccountCurrencyDialog.vue'
import CreateAccountDialog from '@/components/CreateAccountDialog.vue'
import RenameDialog from '@/components/RenameDialog.vue'
import ErrorAlert from '@/components/ErrorAlert.vue'
import { useSubmit } from '@/composables/useSubmit'
import { confirmAction } from '@/confirm'
import { formatMoney } from '@/money'
import { useAccountsStore, type Account } from '@/stores/accounts'

const accounts = useAccountsStore()
const creating = ref(false)
const renaming = ref<Account | null>(null)
const changingCurrency = ref<Account | null>(null)

// A failed refresh keeps the Accounts already shown, and says so above them.
const showLists = computed(() => accounts.accounts.length > 0 || accounts.status === 'ready')

// Closed Accounts come after the open ones, in the order the API gave them.
function openFirst(list: Account[]): Account[] {
  return [...list.filter((account) => !account.isClosed), ...list.filter((account) => account.isClosed)]
}

const owned = computed(() => openFirst(accounts.accounts.filter((account) => !account.isLiability)))
const owed = computed(() => openFirst(accounts.accounts.filter((account) => account.isLiability)))

const acting = useSubmit((account: Account, closing: boolean) =>
  closing ? accounts.close(account.id) : accounts.reopen(account.id),
)

function closingMessage(account: Account): string {
  const kept = 'It keeps its history, and no new money can be recorded on it.'
  return account.balance === 0
    ? kept
    : `${account.name} still holds ${formatMoney(account.balance, account.currency)}. An Account can only be closed at zero, so the API will refuse until that is moved or spent. ${kept}`
}

async function perform(action: AccountAction): Promise<void> {
  const { account } = action
  if (action.type === 'rename') {
    renaming.value = account
  } else if (action.type === 'change-currency') {
    changingCurrency.value = account
  } else if (action.type === 'close') {
    if (await confirmAction(`Close ${account.name}?`, closingMessage(account), 'Close account')) {
      await acting.submit(account, true)
    }
  } else if (await confirmAction(`Reopen ${account.name}?`, 'Money can be recorded on it again.', 'Reopen account')) {
    await acting.submit(account, false)
  }
}

function toggleClosed(value: string | number | boolean): void {
  void accounts.setIncludeClosed(value === true)
}

onMounted(() => accounts.load())
</script>

<template>
  <section>
    <h1>Accounts</h1>

    <div class="accounts__toolbar">
      <ElButton type="primary" @click="creating = true">New account</ElButton>
      <label class="accounts__closed">
        <ElSwitch :model-value="accounts.includeClosed" @change="toggleClosed" />
        Show closed
      </label>
    </div>

    <ErrorAlert v-if="acting.error.value" :title="acting.error.value" class="accounts__alert" />

    <div v-if="accounts.status === 'failed'" class="accounts__alert">
      <ErrorAlert title="Could not load your Accounts" :description="accounts.failure" />
      <ElButton class="accounts__retry" @click="accounts.load()">Try again</ElButton>
    </div>

    <p v-if="accounts.status === 'idle' || (accounts.status === 'loading' && !showLists)" role="status">
      Loading Accounts…
    </p>
    <p v-else-if="showLists && accounts.accounts.length === 0">You have no Accounts yet.</p>
    <template v-else-if="showLists">
      <AccountSection id="own-heading" title="What you own" empty-text="No Accounts here yet." :accounts="owned" @action="perform" />
      <AccountSection id="owe-heading" title="What you owe" empty-text="Nothing owed." :accounts="owed" @action="perform" />
    </template>
    <CreateAccountDialog v-if="creating" @close="creating = false" @saved="creating = false" />
    <RenameDialog v-if="renaming" :target="renaming" :rename="accounts.rename" @close="renaming = null" @saved="renaming = null" />
    <ChangeAccountCurrencyDialog
      v-if="changingCurrency"
      :account="changingCurrency"
      @close="changingCurrency = null"
      @saved="changingCurrency = null"
    />
  </section>
</template>

<style scoped>
.accounts__toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem 1.5rem;
  margin-bottom: 1rem;
}

.accounts__closed {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
}

.accounts__alert {
  margin-bottom: 1rem;
}

.accounts__retry {
  margin-top: 1rem;
}
</style>
