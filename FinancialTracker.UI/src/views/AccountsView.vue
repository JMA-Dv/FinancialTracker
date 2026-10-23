<script setup lang="ts">
import { ElButton, ElSwitch } from 'element-plus'
import { computed, onMounted, ref } from 'vue'
import AccountSection from '@/components/AccountSection.vue'
import type { AccountAction } from '@/components/accountAction'
import ChangeAccountCurrencyDialog from '@/components/ChangeAccountCurrencyDialog.vue'
import CreateAccountDialog from '@/components/CreateAccountDialog.vue'
import RenameAccountDialog from '@/components/RenameAccountDialog.vue'
import ErrorAlert from '@/components/ErrorAlert.vue'
import { useSubmit } from '@/composables/useSubmit'
import { confirmAction } from '@/confirm'
import { useAccountsStore, type Account } from '@/stores/accounts'

const accounts = useAccountsStore()
const creating = ref(false)
const renaming = ref<Account | null>(null)
const changingCurrency = ref<Account | null>(null)

// A failed refresh keeps the Accounts already shown, and says so above them.
const showLists = computed(() => accounts.accounts.length > 0 || accounts.status === 'ready')

const owned = computed(() => accounts.accounts.filter((account) => !account.isLiability))
const owed = computed(() => accounts.accounts.filter((account) => account.isLiability))

const closing = useSubmit((account: Account) => accounts.close(account.id))
const reopening = useSubmit((account: Account) => accounts.reopen(account.id))
// Whichever of the two last failed; the page shows one message at a time.
const actionError = computed(() => closing.error.value || reopening.error.value)

async function perform(action: AccountAction): Promise<void> {
  const { account } = action
  if (action.type === 'rename') {
    renaming.value = account
  } else if (action.type === 'change-currency') {
    changingCurrency.value = account
  } else if (action.type === 'close') {
    reopening.error.value = ''
    if (await confirmAction(`Close ${account.name}?`, 'It keeps its history, and no new money can be recorded on it. It must be at zero.', 'Close account')) {
      await closing.submit(account)
    }
  } else {
    closing.error.value = ''
    if (await confirmAction(`Reopen ${account.name}?`, 'Money can be recorded on it again.', 'Reopen account')) {
      await reopening.submit(account)
    }
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

    <ErrorAlert v-if="actionError" :title="actionError" class="accounts__alert" />

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
    <RenameAccountDialog v-if="renaming" :account="renaming" @close="renaming = null" @saved="renaming = null" />
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
